using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PosManagement.ViewModels
{
    public partial class ReturnHistoryViewModel : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ReturnHistoryViewModel(
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private DateTime? startDate;

        [ObservableProperty]
        private DateTime? endDate;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private ReturnHistoryItemViewModel? selectedReturn;

        [ObservableProperty]
        private int totalReturnCount;

        [ObservableProperty]
        private decimal totalRefundAmount;

        public ObservableCollection<ReturnHistoryItemViewModel> Returns { get; }
            = new();

        public ObservableCollection<ReturnHistoryLineViewModel> SelectedReturnItems { get; }
            = new();

        public ObservableCollection<DailyRefundSummaryViewModel> DailySummaries { get; }
            = new();

        public ObservableCollection<PaymentMethodRefundSummaryViewModel> PaymentMethodSummaries { get; }
            = new();

        partial void OnSelectedReturnChanged(
            ReturnHistoryItemViewModel? value)
        {
            SelectedReturnItems.Clear();

            if (value != null)
            {
                foreach (var item in value.Items)
                    SelectedReturnItems.Add(item);
            }
        }

        [RelayCommand]
        private async Task LoadReturnsAsync()
        {
            if (IsBusy)
                return;

            if (StartDate.HasValue &&
                EndDate.HasValue &&
                StartDate.Value.Date > EndDate.Value.Date)
            {
                StatusMessage = "Start date cannot be later than end date.";
                return;
            }

            IsBusy = true;

            try
            {
                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                // Apply filters once. All summaries and history records
                // use exactly the same filtered dataset.
                var query = db.SaleReturns
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search = SearchText.Trim();

                    query = query.Where(r =>
                        r.Sale.InvoiceNumber.Contains(search));
                }

                if (StartDate.HasValue)
                {
                    var start = StartDate.Value.Date;

                    query = query.Where(r => r.ReturnDate >= start);
                }

                if (EndDate.HasValue)
                {
                    var endExclusive = EndDate.Value.Date.AddDays(1);

                    query = query.Where(r =>
                        r.ReturnDate < endExclusive);
                }

                // Overall summary
                TotalReturnCount = await query.CountAsync();

                TotalRefundAmount = await query
                    .SumAsync(r => (decimal?)r.RefundAmount) ?? 0m;

                // Daily refund summary
                var dailyRecords = await query
                    .GroupBy(r => r.ReturnDate.Date)
                    .Select(g => new DailyRefundSummaryViewModel
                    {
                        ReturnDate = g.Key,
                        ReturnCount = g.Count(),
                        RefundAmount = g.Sum(x => x.RefundAmount)
                    })
                    .OrderByDescending(x => x.ReturnDate)
                    .ToListAsync();

                DailySummaries.Clear();

                foreach (var item in dailyRecords)
                    DailySummaries.Add(item);

                // Payment method summary
                var paymentRecords = await query
                    .GroupBy(r => r.RefundMethod)
                    .Select(g => new PaymentMethodRefundSummaryViewModel
                    {
                        RefundMethod = g.Key,
                        ReturnCount = g.Count(),
                        RefundAmount = g.Sum(x => x.RefundAmount)
                    })
                    .OrderByDescending(x => x.RefundAmount)
                    .ToListAsync();

                PaymentMethodSummaries.Clear();

                foreach (var item in paymentRecords)
                    PaymentMethodSummaries.Add(item);

                // Return details (latest 500 matching records)
                var records = await query
                    .Include(r => r.ProcessedByUser)
                    .Include(r => r.Sale)
                    .Include(r => r.Items)
                        .ThenInclude(i => i.SaleItem)
                    .OrderByDescending(r => r.ReturnDate)
                    .Take(500)
                    .ToListAsync();

                Returns.Clear();

                foreach (var record in records)
                {
                    Returns.Add(new ReturnHistoryItemViewModel
                    {
                        SaleReturnId = record.SaleReturnId,
                        SaleId = record.SaleId,
                        InvoiceNumber = record.Sale.InvoiceNumber,
                        ReturnDate = record.ReturnDate,
                        RefundAmount = record.RefundAmount,
                        RefundMethod = record.RefundMethod,
                        Reason = record.Reason,
                        ProcessedBy = record.ProcessedByUser != null
                                        ? record.ProcessedByUser.FullName
                                        : "Historical / Unknown",
                        Items = record.Items.Select(item =>
                            new ReturnHistoryLineViewModel
                            {
                                ProductName = item.SaleItem.ProductName,
                                SKU = item.SaleItem.SKU,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                RefundAmount = item.RefundAmount,
                                Restock = item.Restock
                            }).ToList()
                    });
                }

                SelectedReturn = Returns.FirstOrDefault();

                StatusMessage =
                    $"Showing {Returns.Count} of {TotalReturnCount} return record(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load return history: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            SearchText = string.Empty;
            StartDate = null;
            EndDate = null;

            await LoadReturnsAsync();
        }
    }

    public class ReturnHistoryItemViewModel
    {
        public int SaleReturnId { get; set; }
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundMethod { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string ProcessedBy { get; set; } = string.Empty;
        public List<ReturnHistoryLineViewModel> Items { get; set; } = new();
    }

    public class ReturnHistoryLineViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public bool Restock { get; set; }

        public string RestockStatus => Restock ? "Yes" : "No";
    }

    public class DailyRefundSummaryViewModel
    {
        public DateTime ReturnDate { get; set; }
        public int ReturnCount { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class PaymentMethodRefundSummaryViewModel
    {
        public string RefundMethod { get; set; } = string.Empty;
        public int ReturnCount { get; set; }
        public decimal RefundAmount { get; set; }
    }
}

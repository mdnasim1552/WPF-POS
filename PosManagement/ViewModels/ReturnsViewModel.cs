using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows;

namespace PosManagement.ViewModels
{
    public partial class ReturnsViewModel : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        private int? _currentSaleId;

        public ReturnsViewModel(
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [ObservableProperty]
        private string invoiceNumber = string.Empty;

        [ObservableProperty]
        private string refundMethod = "Cash";

        [ObservableProperty]
        private string reason = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public ObservableCollection<ReturnableSaleItemViewModel> Items { get; }
            = new();

        public ObservableCollection<string> RefundMethods { get; } =
            new()
            {
            "Cash",
            "Card",
            "Mobile Banking"
            };

        public decimal RefundTotal =>
            Items.Where(x => x.IsSelected)
                 .Sum(x => x.RefundAmount);

        public bool HasSale => _currentSaleId.HasValue;

        partial void OnInvoiceNumberChanged(string value)
        {
            _currentSaleId = null;
            Items.Clear();

            OnPropertyChanged(nameof(HasSale));
            OnPropertyChanged(nameof(RefundTotal));
        }

        [RelayCommand]
        private async Task SearchSaleAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                StatusMessage = "Enter an invoice number.";
                return;
            }

            IsBusy = true;

            try
            {
                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                var sale = await db.Sales
                    .AsNoTracking()
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s =>
                        s.InvoiceNumber == InvoiceNumber.Trim());

                Items.Clear();
                _currentSaleId = null;

                if (sale == null)
                {
                    StatusMessage = "Sale not found.";
                    OnPropertyChanged(nameof(HasSale));
                    OnPropertyChanged(nameof(RefundTotal));
                    return;
                }

                // Get quantities returned in previous return transactions.
                var returnedQuantities = await db.SaleReturnItems
                    .Where(x => x.SaleReturn.SaleId == sale.SaleId)
                    .GroupBy(x => x.SaleItemId)
                    .Select(g => new
                    {
                        SaleItemId = g.Key,
                        ReturnedQuantity = g.Sum(x => x.Quantity)
                    })
                    .ToDictionaryAsync(
                        x => x.SaleItemId,
                        x => x.ReturnedQuantity);

                foreach (var saleItem in sale.Items)
                {
                    int previouslyReturned =
                        returnedQuantities.TryGetValue(
                            saleItem.SaleItemId,
                            out var returned)
                            ? returned
                            : 0;

                    var returnItem = new ReturnableSaleItemViewModel
                    {
                        SaleItemId = saleItem.SaleItemId,
                        ProductId = saleItem.ProductId,
                        ProductName = saleItem.ProductName,
                        SKU = saleItem.SKU,
                        SoldQuantity = saleItem.Quantity,
                        PreviouslyReturned = previouslyReturned,
                        UnitPrice = saleItem.UnitPrice,
                        QuantityToReturn = 0,
                        Restock = true
                    };

                    returnItem.PropertyChanged += (_, _) =>
                    {
                        OnPropertyChanged(nameof(RefundTotal));
                    };

                    Items.Add(returnItem);
                }

                _currentSaleId = sale.SaleId;

                OnPropertyChanged(nameof(HasSale));
                OnPropertyChanged(nameof(RefundTotal));

                StatusMessage = Items.Any(x => x.RemainingQuantity > 0)
                    ? $"Sale found: {sale.InvoiceNumber}"
                    : "All items in this sale have already been returned.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ProcessReturnAsync()
        {
            if (IsBusy)
                return;

            if (!_currentSaleId.HasValue)
            {
                StatusMessage = "Search for a sale first.";
                return;
            }

            var selectedItems = Items
                .Where(x => x.IsSelected)
                .ToList();

            if (!selectedItems.Any())
            {
                StatusMessage = "Select at least one item to return.";
                return;
            }

            foreach (var item in selectedItems)
            {
                if (item.QuantityToReturn <= 0 ||
                    item.QuantityToReturn > item.RemainingQuantity)
                {
                    StatusMessage =
                        $"Invalid return quantity for {item.ProductName}. " +
                        $"Maximum allowed: {item.RemainingQuantity}.";
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(RefundMethod))
            {
                StatusMessage = "Select a refund method.";
                return;
            }

            decimal refundTotal = selectedItems.Sum(
                x => x.QuantityToReturn * x.UnitPrice);

            var confirmation = MessageBox.Show(
                $"Process this return?\n\n" +
                $"Invoice: {InvoiceNumber}\n" +
                $"Refund: {refundTotal:N2}\n" +
                $"Method: {RefundMethod}",
                "Confirm Return",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.Yes)
                return;

            IsBusy = true;

            try
            {
                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                // Serializable isolation protects the return-quantity
                // validation against concurrent return transactions.
                await using var transaction =
                    await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                var sale = await db.Sales
                    .Include(s => s.Items)
                    .FirstOrDefaultAsync(s =>
                        s.SaleId == _currentSaleId.Value);

                if (sale == null)
                    throw new InvalidOperationException(
                        "The original sale could not be found.");

                // Recalculate previously returned quantities from the DB.
                var returnedQuantities = await db.SaleReturnItems
                    .Where(x => x.SaleReturn.SaleId == sale.SaleId)
                    .GroupBy(x => x.SaleItemId)
                    .Select(g => new
                    {
                        SaleItemId = g.Key,
                        ReturnedQuantity = g.Sum(x => x.Quantity)
                    })
                    .ToDictionaryAsync(
                        x => x.SaleItemId,
                        x => x.ReturnedQuantity);

                // Validate against current database values, not UI values.
                foreach (var selected in selectedItems)
                {
                    var originalItem = sale.Items.FirstOrDefault(
                        x => x.SaleItemId == selected.SaleItemId);

                    if (originalItem == null)
                        throw new InvalidOperationException(
                            "A selected sale item no longer exists.");

                    int previouslyReturned =
                        returnedQuantities.TryGetValue(
                            originalItem.SaleItemId,
                            out var returned)
                            ? returned
                            : 0;

                    int remaining =
                        originalItem.Quantity - previouslyReturned;

                    if (selected.QuantityToReturn <= 0 ||
                        selected.QuantityToReturn > remaining)
                    {
                        throw new InvalidOperationException(
                            $"Return quantity changed for " +
                            $"{originalItem.ProductName}. " +
                            $"Only {remaining} unit(s) can be returned.");
                    }
                }

                var saleReturn = new SaleReturn
                {
                    SaleId = sale.SaleId,
                    ReturnDate = DateTime.Now,
                    RefundMethod = RefundMethod,
                    Reason = Reason.Trim(),
                    RefundAmount = 0
                };

                foreach (var selected in selectedItems)
                {
                    var originalItem = sale.Items.First(
                        x => x.SaleItemId == selected.SaleItemId);

                    decimal itemRefund =
                        originalItem.UnitPrice * selected.QuantityToReturn;

                    saleReturn.Items.Add(new SaleReturnItem
                    {
                        SaleItemId = originalItem.SaleItemId,
                        Quantity = selected.QuantityToReturn,
                        UnitPrice = originalItem.UnitPrice,
                        RefundAmount = itemRefund,
                        Restock = selected.Restock
                    });

                    saleReturn.RefundAmount += itemRefund;

                    if (selected.Restock)
                    {
                        var product = await db.Products.FirstOrDefaultAsync(
                            p => p.ProductId == originalItem.ProductId);

                        if (product == null)
                        {
                            throw new InvalidOperationException(
                                $"Product '{originalItem.ProductName}' not found.");
                        }

                        int quantityBefore = product.StockQuantity;

                        product.StockQuantity += selected.QuantityToReturn;

                        db.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.ProductId,
                            MovementType = "Return",
                            QuantityChange = selected.QuantityToReturn,
                            QuantityBefore = quantityBefore,
                            QuantityAfter = product.StockQuantity,
                            SaleId = sale.SaleId,
                            Notes = $"Return for invoice: {sale.InvoiceNumber}"
                        });
                    }
                }

                db.SaleReturns.Add(saleReturn);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                StatusMessage =
                    $"Return completed successfully. " +
                    $"Refund: {saleReturn.RefundAmount:N2}";

                MessageBox.Show(
                    $"Return completed successfully!\n\n" +
                    $"Invoice: {sale.InvoiceNumber}\n" +
                    $"Refund: {saleReturn.RefundAmount:N2}\n" +
                    $"Method: {saleReturn.RefundMethod}",
                    "Return Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                IsBusy = false;
                if (Reason.Length > 500)
                {
                    StatusMessage = "Return reason cannot exceed 500 characters.";
                    return;
                }

                if (RefundMethod.Length > 30)
                {
                    StatusMessage = "Invalid refund method.";
                    return;
                }
                // Reload the invoice so remaining returnable quantities
                // reflect the completed transaction.
                await SearchSaleAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                StatusMessage =
                    "Product stock changed during the return. Please try again.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Return failed: {ex.Message}";

                MessageBox.Show(
                    StatusMessage,
                    "Return Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(RefundTotal));
            }
        }

        partial void OnRefundMethodChanged(string value)
        {
            // No additional calculation required.
        }

        partial void OnReasonChanged(string value)
        {
            // No additional calculation required.
        }

        public void RefreshRefundTotal()
        {
            OnPropertyChanged(nameof(RefundTotal));
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using PosManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace PosManagement.ViewModels
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly CurrentUserService _currentUser;

        public ObservableCollection<Sale> Sales { get; } = new();

        [ObservableProperty]
        private Sale? selectedSale;

        [ObservableProperty]
        private bool isBusy;

        public SalesHistoryViewModel(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            CurrentUserService currentUser)
        {
            _dbFactory = dbFactory;
            _currentUser = currentUser;
        }

        [RelayCommand]
        public async Task LoadSalesAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                // Start with all sales.
                IQueryable<Sale> query = db.Sales
                    .AsNoTracking();

                // Cashiers can only see their own sales.
                if (_currentUser.IsCashier)
                {
                    if (!_currentUser.UserId.HasValue)
                        throw new UnauthorizedAccessException(
                            "No authenticated cashier is available.");

                    int currentUserId = _currentUser.UserId.Value;

                    query = query.Where(s =>
                        s.CreatedByUserId == currentUserId);
                }

                // Admin and Manager can see all sales.
                var records = await query
                    .OrderByDescending(s => s.SaleDate)
                    .Take(500)
                    .ToListAsync();

                Sales.Clear();

                foreach (var sale in records)
                    Sales.Add(sale);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load sales: {ex.Message}",
                    "Sales History",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<Sale?> GetSaleForReceiptAsync(int saleId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            IQueryable<Sale> query = db.Sales
                .AsNoTracking()
                .Include(s => s.Items);

            // Apply the same restriction when loading a receipt.
            // This prevents a cashier from requesting another user's
            // receipt by changing the selected SaleId.
            if (_currentUser.IsCashier)
            {
                if (!_currentUser.UserId.HasValue)
                    throw new UnauthorizedAccessException(
                        "No authenticated cashier is available.");

                int currentUserId = _currentUser.UserId.Value;

                query = query.Where(s =>
                    s.CreatedByUserId == currentUserId);
            }

            return await query.FirstOrDefaultAsync(
                s => s.SaleId == saleId);
        }
    }
}

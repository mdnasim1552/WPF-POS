using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PosManagement.ViewModels
{
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ObservableCollection<Product> Products { get; } = new();

        [ObservableProperty]
        private string productName = string.Empty;

        [ObservableProperty]
        private string sku = string.Empty;

        [ObservableProperty]
        private string barcode = string.Empty;

        [ObservableProperty]
        private string costPriceText = "0.00";

        [ObservableProperty]
        private string sellingPriceText = "0.00";

        [ObservableProperty]
        private string stockQuantityText = "0";

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private bool isBusy;

        public ProductsViewModel(
            IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task LoadProductsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading products...";

                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                var products = await db.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                Products.Clear();

                foreach (var product in products)
                    Products.Add(product);

                StatusMessage = $"{Products.Count} products loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not load products: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddProductAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(ProductName) ||
                string.IsNullOrWhiteSpace(Sku))
            {
                StatusMessage = "Product name and SKU are required.";
                return;
            }

            if (!decimal.TryParse(CostPriceText, out var costPrice) ||
                !decimal.TryParse(SellingPriceText, out var sellingPrice) ||
                !int.TryParse(StockQuantityText, out var stockQuantity))
            {
                StatusMessage = "Enter valid prices and stock quantity.";
                return;
            }

            if (costPrice < 0 || sellingPrice < 0 || stockQuantity < 0)
            {
                StatusMessage = "Prices and stock cannot be negative.";
                return;
            }

            if (sellingPrice < costPrice)
            {
                StatusMessage =
                    "Warning: selling price is below cost price. " +
                    "Check the values before saving.";
                return;
            }

            try
            {
                IsBusy = true;

                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                var normalizedSku = Sku.Trim();

                var skuExists = await db.Products
                    .AnyAsync(p => p.SKU == normalizedSku);

                if (skuExists)
                {
                    StatusMessage = "This SKU already exists.";
                    return;
                }

                var normalizedBarcode = string.IsNullOrWhiteSpace(Barcode)
                    ? null
                    : Barcode.Trim();

                if (normalizedBarcode is not null)
                {
                    var barcodeExists = await db.Products
                        .AnyAsync(p => p.Barcode == normalizedBarcode);

                    if (barcodeExists)
                    {
                        StatusMessage = "This barcode already exists.";
                        return;
                    }
                }

                var product = new Product
                {
                    Name = ProductName.Trim(),
                    SKU = normalizedSku,
                    Barcode = normalizedBarcode,
                    CostPrice = costPrice,
                    SellingPrice = sellingPrice,
                    StockQuantity = stockQuantity,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                db.Products.Add(product);
                await db.SaveChangesAsync();

                Products.Add(product);

                ProductName = string.Empty;
                Sku = string.Empty;
                Barcode = string.Empty;
                CostPriceText = "0.00";
                SellingPriceText = "0.00";
                StockQuantityText = "0";

                StatusMessage = "Product added successfully.";
            }
            catch (DbUpdateException)
            {
                StatusMessage =
                    "Could not save product. Check for duplicate SKU/barcode.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PosManagement.Data;
using PosManagement.Models;
using PosManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace PosManagement.ViewModels
{
    public partial class PosViewModel : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly CurrentUserService _currentUser;
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Product> FilteredProducts { get; } = new();
        public ObservableCollection<CartItem> CartItems { get; } = new();
        public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Card", "Mobile Banking" };
        [ObservableProperty]
        private Product? selectedProduct;

        [ObservableProperty]
        private CartItem? selectedCartItem;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string paymentMethod = "Cash";

        [ObservableProperty]
        private decimal discount;

        [ObservableProperty]
        private decimal amountReceived;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        public decimal Subtotal =>
            CartItems.Sum(item => item.LineTotal);
        [ObservableProperty]
        private decimal taxRate;

        public decimal TaxAmount =>
            Math.Round(
                Math.Max(0, Subtotal - Discount) * TaxRate / 100m,
                2,
                MidpointRounding.AwayFromZero);
        public decimal Total =>
            Math.Max(0, Subtotal - Discount) + TaxAmount;

        public decimal ChangeAmount =>
            PaymentMethod == "Cash"
                ? Math.Max(0, AmountReceived - Total)
                : 0;

        public int CartItemCount =>
            CartItems.Sum(item => item.Quantity);

        public PosViewModel(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            CurrentUserService currentUser)
        {
            _dbFactory = dbFactory;
            _currentUser = currentUser;

            CartItems.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (CartItem item in e.NewItems)
                        item.PropertyChanged += CartItem_PropertyChanged;
                }

                if (e.OldItems != null)
                {
                    foreach (CartItem item in e.OldItems)
                        item.PropertyChanged -= CartItem_PropertyChanged;
                }

                NotifyTotalsChanged();
            };
        }
        partial void OnTaxRateChanged(decimal value)
        {
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ChangeAmount));
        }
        private void CartItem_PropertyChanged(
            object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity))
                NotifyTotalsChanged();
        }

        private void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(CartItemCount));
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyProductFilter();
        }

        partial void OnDiscountChanged(decimal value)
        {
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ChangeAmount));
        }

        partial void OnAmountReceivedChanged(decimal value)
        {
            OnPropertyChanged(nameof(ChangeAmount));
        }

        partial void OnPaymentMethodChanged(string value)
        {
            OnPropertyChanged(nameof(ChangeAmount));
        }

        [RelayCommand]
        public async Task LoadProductsAsync()
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var products = await db.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                Products.Clear();

                foreach (var product in products)
                    Products.Add(product);

                ApplyProductFilter();
                StatusMessage = $"{Products.Count} active products loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not load products: {ex.Message}";
            }
        }

        private void ApplyProductFilter()
        {
            FilteredProducts.Clear();

            var query = SearchText.Trim();

            var matches = Products.Where(p =>
                string.IsNullOrWhiteSpace(query) ||
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.SKU.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode?.Contains(
                    query, StringComparison.OrdinalIgnoreCase) ?? false));

            foreach (var product in matches)
                FilteredProducts.Add(product);
        }

        [RelayCommand]
        private void AddToCart()
        {
            if (SelectedProduct == null)
            {
                StatusMessage = "Select a product first.";
                return;
            }

            if (SelectedProduct.StockQuantity <= 0)
            {
                StatusMessage = "This product is out of stock.";
                return;
            }

            var existing = CartItems.FirstOrDefault(
                x => x.ProductId == SelectedProduct.ProductId);

            if (existing != null)
            {
                if (existing.Quantity + 1 > SelectedProduct.StockQuantity)
                {
                    StatusMessage = "Not enough stock available.";
                    return;
                }

                existing.Quantity++;
            }
            else
            {
                CartItems.Add(new CartItem
                {
                    ProductId = SelectedProduct.ProductId,
                    ProductName = SelectedProduct.Name,
                    SKU = SelectedProduct.SKU,
                    UnitPrice = SelectedProduct.SellingPrice,
                    Quantity = 1
                });
            }

            NotifyTotalsChanged();
            StatusMessage = $"{SelectedProduct.Name} added to cart.";
        }

        [RelayCommand]
        private void RemoveCartItem()
        {
            if (SelectedCartItem == null)
            {
                StatusMessage = "Select a cart item to remove.";
                return;
            }

            CartItems.Remove(SelectedCartItem);
            SelectedCartItem = null;
            NotifyTotalsChanged();
        }

        [RelayCommand]
        private void ClearCart()
        {
            CartItems.Clear();
            Discount = 0;
            AmountReceived = 0;
            StatusMessage = "Cart cleared.";
            NotifyTotalsChanged();
        }

        [RelayCommand]
        private async Task CheckoutAsync()
        {
            if (IsBusy)
                return;

            if (CartItems.Count == 0)
            {
                StatusMessage = "Add at least one product.";
                return;
            }

            if (Discount < 0 || Discount > Subtotal)
            {
                StatusMessage = "Discount must be between zero and subtotal.";
                return;
            }

            if (PaymentMethod == "Cash" && AmountReceived < Total)
            {
                StatusMessage = "Amount received is less than the total.";
                return;
            }
            if (TaxRate < 0 || TaxRate > 100)
            {
                StatusMessage = "Tax rate must be between 0 and 100.";
                return;
            }
            IsBusy = true;

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                await using var transaction =
                    await db.Database.BeginTransactionAsync();

                // Re-read current product records from the database.
                var productIds = CartItems.Select(x => x.ProductId)
                    .Distinct()
                    .ToList();

                var databaseProducts = await db.Products
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                foreach (var item in CartItems)
                {
                    if (!databaseProducts.TryGetValue(
                        item.ProductId, out var product))
                    {
                        throw new InvalidOperationException(
                            $"Product '{item.ProductName}' no longer exists.");
                    }

                    if (!product.IsActive)
                    {
                        throw new InvalidOperationException(
                            $"Product '{product.Name}' is inactive.");
                    }

                    if (item.Quantity <= 0 ||
                        product.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for '{product.Name}'. " +
                            $"Available: {product.StockQuantity}.");
                    }
                }

                var sale = new Sale
                {
                    InvoiceNumber =
                        $"POS-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..29],
                    SaleDate = DateTime.Now,
                    CreatedByUserId = _currentUser.UserId,
                    Subtotal = Subtotal,
                    Discount = Discount,
                    Total = Total,
                    PaymentMethod = PaymentMethod,
                    AmountReceived = PaymentMethod == "Cash"
                        ? AmountReceived
                        : Total,
                    ChangeAmount = PaymentMethod == "Cash"
                        ? AmountReceived - Total
                        : 0,
                    TaxRate = TaxRate,
                    TaxAmount = TaxAmount,
                };


                foreach (var item in CartItems)
                {
                    var product = databaseProducts[item.ProductId];

                    int quantityBefore = product.StockQuantity;

                    sale.Items.Add(new SaleItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        SKU = product.SKU,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        LineTotal = item.UnitPrice * item.Quantity
                    });

                    product.StockQuantity -= item.Quantity;

                    db.StockMovements.Add(new StockMovement
                    {
                        ProductId = product.ProductId,
                        MovementType = "Sale",
                        QuantityChange = -item.Quantity,
                        QuantityBefore = quantityBefore,
                        QuantityAfter = product.StockQuantity,
                        Sale = sale,
                        Notes = $"Invoice: {sale.InvoiceNumber}"
                    });
                }


                db.Sales.Add(sale);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                var invoiceNumber = sale.InvoiceNumber;
                var completedTotal = sale.Total;
                var completedChange = sale.ChangeAmount;

                CartItems.Clear();
                Discount = 0;
                AmountReceived = 0;
                SelectedProduct = null;

                await LoadProductsAsync();

                StatusMessage =
                    $"Sale completed. Invoice: {invoiceNumber}. " +
                    $"Total: {completedTotal:N2}. " +
                    $"Change: {completedChange:N2}.";

                MessageBox.Show(
                    $"Sale completed successfully!\n\n" +
                    $"Invoice: {invoiceNumber}\n" +
                    $"Total: {completedTotal:N2}\n" +
                    $"Change: {completedChange:N2}",
                    "Checkout Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateConcurrencyException)
            {
                StatusMessage =
                    "Stock changed during checkout. Refresh products and try again.";

                await LoadProductsAsync();

                MessageBox.Show(
                    StatusMessage,
                    "Stock Changed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Checkout failed: {ex.Message}";

                MessageBox.Show(
                    StatusMessage,
                    "Checkout Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        public void HandleBarcodeScan(string code)
        {
            code = code.Trim();

            if (string.IsNullOrWhiteSpace(code))
                return;

            var product = Products.FirstOrDefault(p =>
                string.Equals(p.Barcode, code,
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.SKU, code,
                    StringComparison.OrdinalIgnoreCase));

            if (product == null)
            {
                StatusMessage = $"No product found for barcode/SKU: {code}";
                return;
            }

            SelectedProduct = product;
            AddToCartCommand.Execute(null);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PosManagement.Data;
using PosManagement.Models;
using PosManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace PosManagement.ViewModels
{
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private List<Product> _allProducts = new();

        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        [ObservableProperty]
        private Product? selectedProduct;

        [ObservableProperty]
        private Category? selectedCategory;

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
        private string reorderLevelText = "5";

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string newCategoryName = string.Empty;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private bool isBusy;

        public bool IsEditing => SelectedProduct is not null;
        private readonly ProductImageService _productImageService;

        [ObservableProperty]
        private string? selectedImagePath;

        [ObservableProperty]
        private ImageSource? productImagePreview;

        private bool hasImageChange;
        public ProductsViewModel(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            ProductImageService productImageService)
        {
            _dbFactory = dbFactory;
            _productImageService = productImageService;
        }

        partial void OnSelectedProductChanged(Product? value)
        {
            OnPropertyChanged(nameof(IsEditing));

            SelectedImagePath = null;
            hasImageChange = false;

            if (value is null)
            {
                ClearForm();
                return;
            }

            ProductName = value.Name;
            Sku = value.SKU;
            Barcode = value.Barcode ?? string.Empty;

            CostPriceText = value.CostPrice.ToString(
                "0.00", CultureInfo.CurrentCulture);

            SellingPriceText = value.SellingPrice.ToString(
                "0.00", CultureInfo.CurrentCulture);

            StockQuantityText = value.StockQuantity.ToString();
            ReorderLevelText = value.ReorderLevel.ToString();

            SelectedCategory = Categories.FirstOrDefault(
                c => c.CategoryId == value.CategoryId);

            ProductImagePreview =
                _productImageService.LoadImage(value.ImagePath);
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
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

                var categories = await db.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                Categories.Clear();

                foreach (var category in categories)
                    Categories.Add(category);

                _allProducts = await db.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                ApplyFilter();

                StatusMessage = $"{_allProducts.Count} active products loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not load data: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyFilter()
        {
            var query = SearchText.Trim();

            var filtered = _allProducts
                .Where(p =>
                    string.IsNullOrWhiteSpace(query) ||
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.SKU.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (p.Barcode?.Contains(
                        query, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            Products.Clear();

            foreach (var product in filtered)
                Products.Add(product);
        }


        [RelayCommand]
        private async Task SaveProductAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(ProductName) ||
                string.IsNullOrWhiteSpace(Sku))
            {
                StatusMessage = "Product name and SKU are required.";
                return;
            }

            if (!decimal.TryParse(CostPriceText, out var cost) ||
                !decimal.TryParse(SellingPriceText, out var selling) ||
                !int.TryParse(StockQuantityText, out var stock) ||
                !int.TryParse(ReorderLevelText, out var reorderLevel))
            {
                StatusMessage =
                    "Enter valid prices and whole-number quantities.";
                return;
            }

            if (cost < 0 || selling < 0 || stock < 0 || reorderLevel < 0)
            {
                StatusMessage = "Prices and quantities cannot be negative.";
                return;
            }

            if (selling < cost)
            {
                StatusMessage =
                    "Selling price is below cost. Review it before saving.";
                return;
            }

            var normalizedSku = Sku.Trim();

            var normalizedBarcode = string.IsNullOrWhiteSpace(Barcode)
                ? null
                : Barcode.Trim();

            string? newImagePath = null;
            string? oldImagePath = null;
            bool newImageCreated = false;
            bool productInitiallySaved = false;

            try
            {
                IsBusy = true;

                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                var currentId = SelectedProduct?.ProductId ?? 0;

                if (await db.Products.AnyAsync(
                    p => p.SKU == normalizedSku &&
                         p.ProductId != currentId))
                {
                    StatusMessage = "That SKU already exists.";
                    return;
                }

                if (normalizedBarcode is not null &&
                    await db.Products.AnyAsync(
                        p => p.Barcode == normalizedBarcode &&
                             p.ProductId != currentId))
                {
                    StatusMessage = "That barcode already exists.";
                    return;
                }

                Product product;

                if (currentId == 0)
                {
                    product = new Product
                    {
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    db.Products.Add(product);
                }
                else
                {
                    product = await db.Products.FirstAsync(
                        p => p.ProductId == currentId && p.IsActive);
                }

                oldImagePath = product.ImagePath;

                product.Name = ProductName.Trim();
                product.SKU = normalizedSku;
                product.Barcode = normalizedBarcode;
                product.CategoryId = SelectedCategory?.CategoryId;
                product.CostPrice = cost;
                product.SellingPrice = selling;
                product.StockQuantity = stock;
                product.ReorderLevel = reorderLevel;

                // Save first so a new product gets its database ID.
                if (currentId == 0)
                {
                    await db.SaveChangesAsync();
                    productInitiallySaved = true;
                }

                // Only change the image when the user selected or removed one.
                if (hasImageChange)
                {
                    if (!string.IsNullOrWhiteSpace(SelectedImagePath))
                    {
                        newImagePath = _productImageService.SaveProductImage(
                            SelectedImagePath,
                            product.ProductId);

                        newImageCreated = true;
                        product.ImagePath = newImagePath;
                    }
                    else
                    {
                        product.ImagePath = null;
                    }
                }

                // Save the image path and other product changes.
                await db.SaveChangesAsync();

                // Delete the old image only after the database save succeeds.
                if (hasImageChange &&
                    !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    _productImageService.DeleteProductImage(oldImagePath);
                }

                StatusMessage = currentId == 0
                    ? "Product added successfully."
                    : "Product updated successfully.";

                SelectedProduct = null;
                ClearForm();

                await RefreshProductsAsync();
            }
            catch (DbUpdateException)
            {
                if (newImageCreated && newImagePath != null)
                    _productImageService.DeleteProductImage(newImagePath);

                StatusMessage =
                    "Database save failed. Check duplicate SKU/barcode and database constraints.";
            }
            catch (Exception ex)
            {
                if (newImageCreated && newImagePath != null)
                    _productImageService.DeleteProductImage(newImagePath);

                StatusMessage = productInitiallySaved
                    ? $"Product record was created, but image or final save failed: {ex.Message}"
                    : $"Could not save product: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void NewProduct()
        {
            SelectedProduct = null;
            ClearForm();
            StatusMessage = "Ready to add a new product.";
        }

        [RelayCommand]
        private async Task DeactivateProductAsync()
        {
            if (IsBusy || SelectedProduct is null)
                return;

            var productId = SelectedProduct.ProductId;

            try
            {
                IsBusy = true;

                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                var product = await db.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product is null)
                {
                    StatusMessage = "Product not found.";
                    return;
                }

                product.IsActive = false;
                await db.SaveChangesAsync();

                SelectedProduct = null;
                ClearForm();

                StatusMessage = "Product deactivated.";
                await RefreshProductsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not deactivate product: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddCategoryAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(NewCategoryName))
            {
                StatusMessage = "Enter a category name.";
                return;
            }

            var name = NewCategoryName.Trim();

            try
            {
                IsBusy = true;

                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                if (await db.Categories.AnyAsync(
                    c => c.Name == name))
                {
                    StatusMessage = "That category already exists.";
                    return;
                }

                var category = new Category
                {
                    Name = name,
                    IsActive = true
                };

                db.Categories.Add(category);
                await db.SaveChangesAsync();

                Categories.Add(category);
                SelectedCategory = category;
                NewCategoryName = string.Empty;

                StatusMessage = "Category added successfully.";
            }
            catch (DbUpdateException)
            {
                StatusMessage = "Could not add category. It may already exist.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not add category: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        private void BrowseProductImage()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Product Image",
                Filter =
                    "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
                    "All Files|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var preview = _productImageService.LoadImage(
                    dialog.FileName);

                if (preview == null)
                {
                    StatusMessage = "Could not load the selected image.";
                    return;
                }

                SelectedImagePath = dialog.FileName;
                ProductImagePreview = preview;
                hasImageChange = true;

                StatusMessage = "Image selected. Save the product to apply it.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not select image: {ex.Message}";
            }
        }

        [RelayCommand]
        private void RemoveProductImage()
        {
            SelectedImagePath = null;
            ProductImagePreview = null;
            hasImageChange = true;

            StatusMessage = "Image will be removed when you save the product.";
        }
        private async Task RefreshProductsAsync()
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            _allProducts = await db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ApplyFilter();
        }

        private void ClearForm()
        {
            ProductName = string.Empty;
            Sku = string.Empty;
            Barcode = string.Empty;

            CostPriceText = "0.00";
            SellingPriceText = "0.00";
            StockQuantityText = "0";
            ReorderLevelText = "5";

            SelectedCategory = null;

            SelectedImagePath = null;
            ProductImagePreview = null;
            hasImageChange = false;
        }
    }
}

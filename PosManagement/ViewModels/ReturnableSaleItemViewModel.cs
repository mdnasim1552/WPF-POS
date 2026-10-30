using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.ViewModels
{
    public partial class ReturnableSaleItemViewModel : ObservableObject
    {
        public int SaleItemId { get; set; }
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public int SoldQuantity { get; set; }
        public int PreviouslyReturned { get; set; }

        public int RemainingQuantity =>
            SoldQuantity - PreviouslyReturned;

        public decimal UnitPrice { get; set; }

        [ObservableProperty]
        private int quantityToReturn;

        [ObservableProperty]
        private bool restock = true;

        [ObservableProperty]
        private bool isSelected;

        public decimal RefundAmount =>
            IsSelected ? QuantityToReturn * UnitPrice : 0;

        partial void OnQuantityToReturnChanged(int value)
        {
            OnPropertyChanged(nameof(RefundAmount));
        }

        partial void OnIsSelectedChanged(bool value)
        {
            OnPropertyChanged(nameof(RefundAmount));
        }
    }
}

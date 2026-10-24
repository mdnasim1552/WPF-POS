using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Models
{
    public partial class CartItem : ObservableObject
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        [ObservableProperty]
        private int quantity = 1;

        public decimal LineTotal => UnitPrice * Quantity;

        partial void OnQuantityChanged(int value)
        {
            OnPropertyChanged(nameof(LineTotal));
        }
    }
}

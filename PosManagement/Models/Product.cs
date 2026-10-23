using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PosManagement.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SellingPrice { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

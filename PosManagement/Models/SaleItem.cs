using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Models
{
    public class SaleItem
    {
        public int SaleItemId { get; set; }

        public int SaleId { get; set; }

        public Sale Sale { get; set; } = null!;

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Snapshot values preserve the original sale details.
        public string ProductName { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
    }
}

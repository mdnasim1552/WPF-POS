using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Models
{
    public class SaleReturnItem
    {
        public int SaleReturnItemId { get; set; }

        public int SaleReturnId { get; set; }

        public SaleReturn SaleReturn { get; set; } = null!;

        public int SaleItemId { get; set; }

        public SaleItem SaleItem { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal RefundAmount { get; set; }

        public bool Restock { get; set; } = true;
    }
}

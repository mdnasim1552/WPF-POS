using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Models
{
    public class StockMovement
    {
        public int StockMovementId { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Sale, Purchase, Adjustment, Return
        public string MovementType { get; set; } = string.Empty;

        // Positive = stock added; negative = stock removed.
        public int QuantityChange { get; set; }

        public int QuantityBefore { get; set; }

        public int QuantityAfter { get; set; }

        public int? SaleId { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

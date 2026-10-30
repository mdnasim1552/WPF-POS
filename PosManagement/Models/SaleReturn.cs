using System;
using System.Collections.Generic;
using System.Text;

namespace PosManagement.Models
{
    public class SaleReturn
    {
        public int SaleReturnId { get; set; }

        public int SaleId { get; set; }

        public Sale Sale { get; set; } = null!;

        public DateTime ReturnDate { get; set; } = DateTime.Now;

        public decimal RefundAmount { get; set; }

        public string RefundMethod { get; set; } = "Cash";

        public string Reason { get; set; } = string.Empty;

        public ICollection<SaleReturnItem> Items { get; set; }
            = new List<SaleReturnItem>();
        public int? ProcessedByUserId { get; set; }
        public AppUser? ProcessedByUser { get; set; }
    }
}

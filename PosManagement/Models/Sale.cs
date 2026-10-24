using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Text;

namespace PosManagement.Models
{
    public class Sale
    {
        public int SaleId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime SaleDate { get; set; } = DateTime.Now;

        public decimal Subtotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Total { get; set; }

        public string PaymentMethod { get; set; } = "Cash";

        public decimal AmountReceived { get; set; }

        public decimal ChangeAmount { get; set; }

        public ICollection<SaleItem> Items { get; set; }
            = new List<SaleItem>();
    }
}

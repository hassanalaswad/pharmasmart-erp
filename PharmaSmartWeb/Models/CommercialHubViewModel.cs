using System;
using System.Collections.Generic;

namespace PharmaSmartWeb.Models
{
    public class CommercialHubViewModel
    {
        public bool IsCashier { get; set; }
        public decimal TodaySales { get; set; }
        public decimal TodayReturns { get; set; }
        public int TodayInvoicesCount { get; set; }
        public int TodayReturnsCount { get; set; }

        public List<RecentCommercialInvoice> RecentSales { get; set; } = new List<RecentCommercialInvoice>();
        public List<RecentCommercialPurchase> RecentPurchases { get; set; } = new List<RecentCommercialPurchase>();

        public List<decimal> SalesLast7Days { get; set; } = new List<decimal>();
        public List<decimal> PurchasesLast7Days { get; set; } = new List<decimal>();
        public List<string> Last7DaysLabels { get; set; } = new List<string>();

        public int TotalSalesOperations { get; set; }
        public int TotalPurchasesOperations { get; set; }
        public int TotalReturnsOperations { get; set; }
        public int TotalOperationsToday => TotalSalesOperations + TotalPurchasesOperations + TotalReturnsOperations;
    }

    public class RecentCommercialInvoice
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber => $"INV-{InvoiceId:D4}";
        public string CustomerName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime SaleDate { get; set; }
    }

    public class RecentCommercialPurchase
    {
        public int PurchaseId { get; set; }
        public string InvoiceNumber => $"PUR-{PurchaseId:D4}";
        public string SupplierName { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
    }
}

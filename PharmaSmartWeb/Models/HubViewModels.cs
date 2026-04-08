using System;
using System.Collections.Generic;

namespace PharmaSmartWeb.Models
{
    // =========================================================
    // ≡ات ┘à┘ê╪»┘è┘╪د╪ز ╪ذ┘è╪د┘╪د╪ز ╪د┘╪ذ┘ê╪د╪ذ╪د╪ز ╪د┘┘à╪▒┘â╪▓┘è╪ر (Central Hubs)
    // =========================================================

    public class SalesHubViewModel
    {
        public bool IsCashier { get; set; }
        public decimal TotalSalesMonth { get; set; }
        public int InvoicesCountMonth { get; set; }
        public int CustomersCount { get; set; }
        public decimal TotalReturnsMonth { get; set; }

        public decimal TotalOwedDebts { get; set; }
        public int TotalLoyaltyPoints { get; set; }
        public int ActiveCustomersPercent { get; set; }
        public int TodayInvoicesCount { get; set; }

        public System.Collections.Generic.List<CustomerOverviewModel> TopCustomers { get; set; } = new System.Collections.Generic.List<CustomerOverviewModel>();
        public System.Collections.Generic.List<RecentInvoiceModel> TodayInvoices { get; set; } = new System.Collections.Generic.List<RecentInvoiceModel>();
        public System.Collections.Generic.List<RecentReturnModel> RecentReturns { get; set; } = new System.Collections.Generic.List<RecentReturnModel>();
    }

    public class CustomerOverviewModel
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public decimal CreditBalance { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }

    public class RecentInvoiceModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber => $"INV-{InvoiceId:D4}";
        public DateTime SaleDate { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class RecentReturnModel
    {
        public int ReturnId { get; set; }
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ReturnDate { get; set; }
    }

    public class PurchasesHubViewModel
    {
        public decimal TotalPurchasesMonth { get; set; }
        public int SuppliersCount { get; set; }
        public int PurchasesInvoicesCount { get; set; }
        public decimal TotalReturnsMonth { get; set; }
        public int MonthReturnsCount { get; set; }

        public decimal TotalSupplierDebts { get; set; }
        public int PendingPurchasesCount { get; set; }
        public decimal PurchasesGrowthPercent { get; set; }

        public System.Collections.Generic.List<RecentPurchaseModel> RecentPurchases { get; set; } = new System.Collections.Generic.List<RecentPurchaseModel>();
        public System.Collections.Generic.List<SupplierOverviewModel> ActiveSuppliers { get; set; } = new System.Collections.Generic.List<SupplierOverviewModel>();
    }

    public class RecentPurchaseModel
    {
        public int PurchaseId { get; set; }
        public string InvoiceNumber => $"PUR-{PurchaseId:D4}";
        public string SupplierName { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPaid { get; set; }
        public bool HasBonusItems { get; set; }
        public string SupplierInitials => string.IsNullOrWhiteSpace(SupplierName) ? "┘à" : (SupplierName.Length >= 2 ? SupplierName.Substring(0, 2) : SupplierName);
    }

    public class SupplierOverviewModel
    {
        public int SupplierId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

    public class InventoryHubViewModel
    {
        public decimal TotalInventoryValue { get; set; }
        public int TotalDrugs { get; set; }
        public int ShortagesCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int WarehousesCount { get; set; }
        public int ItemGroupsCount { get; set; }
        public int PendingTransfersCount { get; set; }

        public List<string> ValuationLabels { get; set; } = new List<string>();
        public List<decimal> ValuationData { get; set; } = new List<decimal>();

        public int StockHealthyPercentage { get; set; }
        public int StockExpiringPercentage { get; set; }
        public int StockDangerPercentage { get; set; }

        public List<RecentStockMovementModel> RecentMovements { get; set; } = new List<RecentStockMovementModel>();
        public List<InventoryAlertModel> CriticalAlerts { get; set; } = new List<InventoryAlertModel>();
    }

    public class RecentStockMovementModel
    {
        public int MovementId { get; set; }
        public string DrugName { get; set; }
        public string BatchNo { get; set; }
        public string MovementType { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public DateTime MovementDate { get; set; }
        public string UserName { get; set; }
    }

    public class InventoryAlertModel 
    {
        public string DrugName { get; set; }
        public string BatchNo { get; set; }
        public string AlertType { get; set; } 
        public string AlertSeverity { get; set; } 
        public string AlertMessage { get; set; }
        public string ActionType { get; set; } 
    }

    public class FinanceHubViewModel
    {
        public decimal TotalCash { get; set; }
        public decimal TotalBankBalance { get; set; }
        public int JournalEntriesCount { get; set; }
        public int VouchersMonthCount { get; set; }
        
        public decimal AccountsReceivable { get; set; }
        public decimal AccountsPayable { get; set; }

        public List<string> CashFlowLabels { get; set; } = new List<string>();
        public List<decimal> CashInflows { get; set; } = new List<decimal>();
        public List<decimal> CashOutflows { get; set; } = new List<decimal>();

        public List<string> LiquidityLabels { get; set; } = new List<string>();
        public List<decimal> LiquidityData { get; set; } = new List<decimal>();

        public List<RecentVoucherModel> RecentVouchers { get; set; } = new List<RecentVoucherModel>();
        public List<RecentJournalModel> RecentJournals { get; set; } = new List<RecentJournalModel>();
    }

    public class RecentVoucherModel
    {
        public int VoucherId { get; set; }
        public string VoucherNumber => $"V-{VoucherId:D4}";
        public string VoucherType { get; set; } // Receipt, Payment
        public string PayeePayerName { get; set; }
        public string Notes { get; set; }
        public decimal Amount { get; set; }
        public DateTime VoucherDate { get; set; }
    }

    public class RecentJournalModel
    {
        public int JournalId { get; set; }
        public string JournalNumber => $"JV-{JournalId:D4}";
        public string Description { get; set; }
        public string Source { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsPosted { get; set; }
        public DateTime JournalDate { get; set; }
    }

    public class ReportsHubViewModel
    {
        // Permission Flags
        public bool CanViewFinancialReports { get; set; }
        public bool CanViewInventoryReports { get; set; }

        // Filters
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? SelectedBranchId { get; set; }
        public bool IsSuperAdmin { get; set; }

        // KPIs
        public decimal TotalRevenuesYTD { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal TotalAssetsValue { get; set; }
        public int BranchesCount { get; set; }
        public decimal TotalLiquidity { get; set; }
        
        // P&L Trend Chart
        public List<string> PnlLabels { get; set; } = new List<string>();
        public List<decimal> PnlRevenues { get; set; } = new List<decimal>();
        public List<decimal> PnlExpenses { get; set; } = new List<decimal>();

        // Branch Comparison Chart
        public List<string> BranchLabels { get; set; } = new List<string>();
        public List<decimal> BranchSales { get; set; } = new List<decimal>();
        public List<decimal> BranchReturns { get; set; } = new List<decimal>();

        // ABC Analysis Chart
        public decimal AbcCategoryA { get; set; }
        public decimal AbcCategoryB { get; set; }
        public decimal AbcCategoryC { get; set; }

        // Cash Flow Trend Chart
        public List<string> CashFlowLabels { get; set; } = new List<string>();
        public List<decimal> CashInflows { get; set; } = new List<decimal>();
        public List<decimal> CashOutflows { get; set; } = new List<decimal>();
    }

    public class AdminHubViewModel
    {
        public int EmployeesCount { get; set; }
        public int UsersCount { get; set; }
        public int ActiveBranches { get; set; }
        public int RolesCount { get; set; }
        public int SystemErrorsCount { get; set; }
        public int TodayLogsCount { get; set; }
        public decimal TotalSalaries { get; set; }
    }

    public class SettingsHubViewModel
    {
        public bool IsSuperAdmin { get; set; }
        public bool IsPharmacist { get; set; }
    }
}

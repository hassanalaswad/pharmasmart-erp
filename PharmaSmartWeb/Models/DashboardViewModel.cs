using System;
using System.Collections.Generic;

namespace PharmaSmartWeb.Models
{
    // =========================================================
    // 📊 1. موديلات لوحات التحكم (Dashboards)
    // =========================================================

    public class DashboardViewModel
    {

        public decimal TotalSalesAllTime { get; set; }
        public decimal TodaySales { get; set; }
        public decimal TotalSales { get; set; }
        public int InvoicesCount { get; set; }
        public decimal NetProfit { get; set; }
        public int ShortagesCount { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalRevenues { get; set; }

        public string[] ChartLabels { get; set; }
        public decimal[] SalesChartData { get; set; }
        public decimal[] ProfitChartData { get; set; }

        public List<BranchAnalysisViewModel> BranchesPerformance { get; set; }
        public List<AuditAlertViewModel> CriticalAlerts { get; set; }
        // =========================================================
        // 🔐 الإضافة الجديدة: مفاتيح التحكم بالعرض (View Control Flags)
        // =========================================================
        public bool IsSuperAdmin { get; set; }
        public bool CanViewFinancials { get; set; }
        public bool CanViewInventory { get; set; }
        public bool ShowPosWorkspace { get; set; }

        // New properties for new Dashboard UI
        public int TotalStockQuantity { get; set; }
        public int SystemLogsCount { get; set; }
        public List<JournalDetailOverview> RecentJournals { get; set; } = new List<JournalDetailOverview>();
        public List<BranchStatusOverview> BranchStatuses { get; set; } = new List<BranchStatusOverview>();
        public List<ExpiringDrugOverview> ExpiringDrugs { get; set; } = new List<ExpiringDrugOverview>();
        public List<ShortageItemOverview> ShortageItems { get; set; } = new List<ShortageItemOverview>();
    }

    public class JournalDetailOverview {
        public string TrxNumber { get; set; }
        public string AccountName { get; set; }
        public string Type { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
    public class BranchStatusOverview {
        public string BranchName { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
    }
    public class ExpiringDrugOverview {
        public string DrugName { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
    }
    public class ShortageItemOverview {
        public string DrugName { get; set; }
        public int ShortageAmount { get; set; }
    }

    public class BranchAnalysisViewModel
    {
        public int BranchId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public decimal Sales { get; set; }
        public decimal Profit { get; set; }
        public int Shortages { get; set; }
        public string PerformanceLevel { get; set; }
        public string Status { get; set; }
        public double TargetAchievement { get; set; }
    }

    public class AuditAlertViewModel
    {
        public string BranchName { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public DateTime Time { get; set; }
    }

    // =========================================================
    // 📈 2. موديلات مركز التقارير (Reports Center)
    // =========================================================

    public class ReportCenterViewModel
    {
        public decimal MonthlyRevenue { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public int ExpiredItemsCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int TotalTransactions { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; }
    }

    public class RecentActivityViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
    }

    public class StockExpiryViewModel
    {
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public string BatchNumber { get; set; }
        public int Quantity { get; set; }
        public string UnitName { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class PharmacistSalesViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CommissionAmount { get; set; }
        public double SalesPercentage { get; set; }
    }

    public class ShortageForecastViewModel
    {
        public int DrugId { get; set; }
        public string DrugName { get; set; }
        public int CurrentStock { get; set; }
        public decimal MonthlyForecast { get; set; }
        public decimal SuggestedOrder { get; set; }
        public string RiskLevel { get; set; }
    }

    // 🚀 تم إضافة خاصية Percentage هنا لحل خطأ الكنترولر
    public class PnlAccountViewModel
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public decimal Total { get; set; }
        public double Percentage { get; set; }
    }

    // =========================================================
    // 🔄 3. موديلات دعم التوافقية للإصدارات السابقة (Legacy Support)
    // =========================================================

    public class BranchCardViewModel
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthlySales { get; set; }
        public int ShortagesCount { get; set; }
        public string Status { get; set; }
    }

    public class HQDashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalInvoices { get; set; }
        public decimal NetProfit { get; set; }
        public int GlobalShortages { get; set; }
        public List<BranchOverview> Branches { get; set; }
        public decimal[] ChartSalesData { get; set; }
        public string[] ChartLabels { get; set; }
    }

    public class BranchOverview
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public decimal TodaySales { get; set; }
        public decimal MonthlySales { get; set; }
        public int PendingShortages { get; set; }
        public string PerformanceLevel { get; set; }
    }
}

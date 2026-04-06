// ============================================================
//  🗺️ AppRoutes — الدليل المركزي لجميع مسارات وانتقالات التطبيق
//  PharmaSmart ERP — Infrastructure Layer
//
//  🎯 الهدف:
//    - منع أي Hardcoded URL في أي مكان بالتطبيق.
//    - نقطة مرجعية واحدة لأسماء الكنترولرات والدوال والشاشات.
//    - تسهيل إعادة التسمية والـ Refactoring مستقبلاً.
//
//  📖 طريقة الاستخدام:
//    - في وجهات Razor:    asp-controller="@AppRoutes.Drugs.Controller"  asp-action="@AppRoutes.Drugs.Actions.Index"
//    - في الكنترولرات:    return RedirectToAction(AppRoutes.Drugs.Actions.Index, AppRoutes.Drugs.Controller);
//    - في الفلاتر:        [HasPermission(AppRoutes.Drugs.Screen, "Edit")]
// ============================================================

namespace PharmaSmartWeb.Infrastructure
{
    /// <summary>
    /// الثوابت المركزية لجميع مسارات التطبيق وأسماء الشاشات.
    /// استخدم هذا الكلاس في كل مكان بدلاً من الـ Hardcoded Strings.
    /// </summary>
    public static class AppRoutes
    {
        // ┌──────────────────────────────────────────────────────────────┐
        // │  🏠 الرئيسية والحساب                                        │
        // └──────────────────────────────────────────────────────────────┘

        public static class Home
        {
            public const string Controller = "Home";
            public static class Actions
            {
                public const string Index        = "Index";
                public const string AccessDenied = "AccessDenied";
                public const string HandleError  = "HandleError";
                
                // 🏢 البوابات المركزية (Central Hubs)
                public const string SalesHub     = "SalesHub";
                public const string PurchasesHub = "PurchasesHub";
                public const string InventoryHub = "InventoryHub";
                public const string FinanceHub   = "FinanceHub";
                public const string ReportsHub   = "ReportsHub";
                public const string AdminHub     = "AdminHub";
            }
        }

        public static class Account
        {
            public const string Controller = "Account";
            public static class Actions
            {
                public const string Login          = "Login";
                public const string Logout         = "Logout";
                public const string ChangePassword = "ChangePassword";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  💊 الأدوية والمخزون                                         │
        // └──────────────────────────────────────────────────────────────┘

        public static class Drugs
        {
            public const string Controller = "Drugs";
            public const string Screen     = "Drugs";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
                public const string Details = "Details";
            }
        }

        public static class Inventory
        {
            public const string Controller = "Inventory";
            public const string Screen     = "Inventory";
            public static class Actions
            {
                public const string Index     = "Index";
                public const string Shortages = "Shortages";
            }
        }

        public static class StockAudit
        {
            public const string Controller = "StockAudit";
            public const string Screen     = "StockAudit";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class Warehouses
        {
            public const string Controller = "Warehouses";
            public const string Screen     = "Warehouses";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class ItemGroups
        {
            public const string Controller = "ItemGroups";
            public const string Screen     = "ItemGroups";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class Barcode
        {
            public const string Controller = "Barcode";
            public const string Screen     = "Barcode";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Print  = "Print";
            }
        }

        public static class DrugTransfers
        {
            public const string Controller = "DrugTransfers";
            public const string Screen     = "DrugTransfers";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  💰 المبيعات والعملاء                                        │
        // └──────────────────────────────────────────────────────────────┘

        public static class Sales
        {
            public const string Controller = "Sales";
            public const string Screen     = "Sales";
            public static class Actions
            {
                public const string Index   = "Index";
                public const string Create  = "Create";
                public const string Details = "Details";
                public const string Delete  = "Delete";
                public const string Pos     = "Pos";        // نقطة البيع المباشرة
                public const string Print   = "PrintInvoice";
            }
        }

        public static class SalesReturn
        {
            public const string Controller = "SalesReturn";
            public const string Screen     = "SalesReturn";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class Customers
        {
            public const string Controller = "Customers";
            public const string Screen     = "Customers";
            public static class Actions
            {
                public const string Index   = "Index";
                public const string Create  = "Create";
                public const string Edit    = "Edit";
                public const string Delete  = "Delete";
                public const string Details = "Details";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🚚 المشتريات والموردين                                      │
        // └──────────────────────────────────────────────────────────────┘

        public static class Purchases
        {
            public const string Controller = "Purchases";
            public const string Screen     = "Purchases";
            public static class Actions
            {
                public const string Index   = "Index";
                public const string Create  = "Create";
                public const string Edit    = "Edit";
                public const string Delete  = "Delete";
                public const string Details = "Details";
                public const string Print   = "PrintInvoice";
            }
        }

        public static class PurchasesReturn
        {
            public const string Controller = "PurchasesReturn";
            public const string Screen     = "PurchasesReturn";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class Suppliers
        {
            public const string Controller = "Suppliers";
            public const string Screen     = "Suppliers";
            public static class Actions
            {
                public const string Index   = "Index";
                public const string Create  = "Create";
                public const string Edit    = "Edit";
                public const string Delete  = "Delete";
                public const string Details = "Details";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🏦 المالية والمحاسبة                                        │
        // └──────────────────────────────────────────────────────────────┘

        public static class Accounting
        {
            public const string Controller = "Accounting";
            public const string Screen     = "Accounting";
            public static class Actions
            {
                public const string Index        = "Index";
                public const string Create       = "Create";
                public const string Edit         = "Edit";
                public const string Delete       = "Delete";
        }
        }

        public static class JournalEntries
        {
            public const string Controller = "JournalEntries";
            public const string Screen     = "JournalEntries";
            public static class Actions
            {
                public const string Index   = "Index";
                public const string Create  = "Create";
                public const string Edit    = "Edit";
                public const string Delete  = "Delete";
                public const string Details = "Details";
                public const string Post    = "Post";
            }
        }

        public static class Vouchers
        {
            public const string Controller = "Vouchers";
            public const string Screen     = "Vouchers";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
                public const string Print  = "Print";
            }
        }

        public static class FundTransfers
        {
            public const string Controller = "FundTransfers";
            public const string Screen     = "FundTransfers";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class Currencies
        {
            public const string Controller = "Currencies";
            public const string Screen     = "Currencies";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        public static class FinancialSettings
        {
            public const string Controller = "FinancialSettings";
            public const string Screen     = "FinancialSettings";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Edit   = "Edit";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  📊 التقارير وذكاء الأعمال                                   │
        // └──────────────────────────────────────────────────────────────┘

        public static class Report
        {
            public const string Controller = "Report";
            public static class Actions
            {
                public const string Index             = "Index";
                public const string TrialBalance      = "TrialBalance";
                public const string Ledger            = "Ledger";
            }
        }

        public static class ReportCenter
        {
            public const string Controller = "ReportCenter";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class IncomeStatementReport
        {
            public const string Controller = "IncomeStatementReport";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class CashFlowReport
        {
            public const string Controller = "CashFlowReport";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class ProfitLossReport
        {
            public const string Controller = "ProfitLossReport";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class StockExpiryReport
        {
            public const string Controller = "StockExpiryReport";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class ShortageForecastReport
        {
            public const string Controller = "ShortageForecastReport";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class PharmacistSalesReport
        {
            public const string Controller = "PharmacistSalesReport";
            public static class Actions { public const string Index = "Index"; }
        }

        public static class InventoryIntelligence
        {
            public const string Controller = "InventoryIntelligence";
            public static class Actions
            {
                public const string Index = "Index";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  ⚙️ الإدارة والرقابة                                         │
        // └──────────────────────────────────────────────────────────────┘

        public static class Employees
        {
            public const string Controller = "Employees";
            public const string Screen     = "Employees";
            public static class Actions
            {
                public const string Index   = "Index";
                public const string Create  = "Create";
                public const string Edit    = "Edit";
                public const string Delete  = "Delete";
                public const string Details = "Details";
            }
        }

        public static class Users
        {
            public const string Controller = "Users";
            public const string Screen     = "Users";
            public static class Actions
            {
                public const string Index          = "Index";
                public const string Create         = "Create";
                public const string Edit           = "Edit";
                public const string Delete         = "Delete";
                public const string ResetPassword  = "ResetPassword";
            }
        }

        public static class Roles
        {
            public const string Controller = "Roles";
            public const string Screen     = "Roles";
            public static class Actions
            {
                public const string Index             = "Index";
                public const string ManagePermissions = "ManagePermissions";
                public const string UpdatePermissions = "UpdatePermissions";
            }
        }

        public static class Admin
        {
            public const string Controller = "Admin";
            public const string Screen     = "SystemLogs";
            public static class Actions
            {
                public const string Index      = "Index";
                public const string SystemLogs = "SystemLogs";
            }
        }

        public static class Branches
        {
            public const string Controller = "Branches";
            public const string Screen     = "Branches";
            public static class Actions
            {
                public const string Index  = "Index";
                public const string Create = "Create";
                public const string Edit   = "Edit";
                public const string Delete = "Delete";
            }
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🔀 ثوابت الانتقال الموحدة (Transition Constants)            │
        // │  استخدمها في TempData["RedirectTo"] أو RedirectToAction      │
        // └──────────────────────────────────────────────────────────────┘

        /// <summary>
        /// قائمة شاملة بأسماء جميع الشاشات المسجلة في systemscreens،
        /// تُستخدم في [HasPermission] وتسجيل السجلات (RecordLog)
        /// </summary>
        public static class Screens
        {
            // ───── الأدوية والمخزون ─────
            public const string Drugs           = "Drugs";
            public const string ShortageForecast= "ShortageForecast";
            public const string Inventory       = "Inventory";
            public const string InventoryShortages = "Inventory.Shortages";
            public const string StockAudit      = "StockAudit";
            public const string Warehouses      = "Warehouses";
            public const string Batches         = "Batches";
            public const string ItemGroups      = "ItemGroups";
            public const string Barcode         = "Barcode";
            public const string DrugTransfers   = "DrugTransfers";

            // ───── المبيعات والعملاء ─────
            public const string Sales           = "Sales";
            public const string SalesReturn     = "SalesReturn";
            public const string Customers       = "Customers";

            // ───── المشتريات والموردين ─────
            public const string Purchases       = "Purchases";
            public const string PurchasesReturn = "PurchasesReturn";
            public const string Suppliers       = "Suppliers";

            // ───── المالية والحسابات ─────
            public const string Accounting      = "Accounting";
            public const string JournalEntries  = "JournalEntries";
            public const string TrialBalance    = "TrialBalance";
            public const string AccountReports  = "AccountReports";
            public const string Vouchers        = "Vouchers";
            public const string FundTransfers   = "FundTransfers";
            public const string BankAccounts    = "BankAccounts";
            public const string Currencies      = "Currencies";
            public const string FinancialSettings = "FinancialSettings";

            // ───── التقارير المالية ─────
            public const string Ledger          = "Ledger";
            public const string DailyCashFlow   = "DailyCashFlow";
            public const string IncomeStatement = "IncomeStatement";
            public const string ProfitAndLoss   = "ProfitAndLoss";

            // ───── ذكاء الأعمال ─────
            public const string PharmacistSales = "PharmacistSales";
            public const string StockExpiry     = "StockExpiry";

            // ───── الإدارة والرقابة ─────
            public const string Employees       = "Employees";
            public const string Users           = "Users";
            public const string Roles           = "Roles";
            public const string SystemLogs      = "SystemLogs";
            public const string Branches        = "Branches";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🎬 أنواع الإجراءات الموحدة (Permission Actions)             │
        // └──────────────────────────────────────────────────────────────┘

        public static class PermissionActions
        {
            public const string View   = "View";
            public const string Add    = "Add";
            public const string Edit   = "Edit";
            public const string Delete = "Delete";
            public const string Print  = "Print";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🍪 ثوابت الكوكيز                                            │
        // └──────────────────────────────────────────────────────────────┘

        public static class Cookies
        {
            public const string TokenName      = "PharmaSmart_FastToken";
            public const string ActiveBranchId = "ActiveBranchId";
            public const string IsInsideBranch = "IsInsideBranch";
        }

        // ┌──────────────────────────────────────────────────────────────┐
        // │  🔑 ثوابت Claims                                             │
        // └──────────────────────────────────────────────────────────────┘

        public static class ClaimKeys
        {
            public const string UserID            = "UserID";
            public const string RoleID            = "RoleID";
            public const string RoleName          = "RoleName";
            public const string BranchID          = "BranchID";
            public const string BranchName        = "BranchName";
            public const string Permission        = "Permission";
            public const string PermissionsLoaded = "PermissionsLoaded";
        }
    }
}

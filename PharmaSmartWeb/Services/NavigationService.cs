using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Services
{
    // ==========================================
    // 🌍 محتويات القائمة (Navigation Models)
    // ==========================================
    
    public class MenuItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string RequiredPolicy { get; set; } // سياسة الحماية المطلوبة لرؤية هذا العنصر
    }

    public class MenuGroup
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public List<MenuItem> Items { get; set; } = new List<MenuItem>();
    }

    // ==========================================
    // 🧠 واجهة المحرك الديناميكي (Engine Interface)
    // ==========================================
    public interface INavigationService
    {
        Task<List<MenuGroup>> GetAllowedMenusAsync(ClaimsPrincipal user);
    }

    // ==========================================
    // ⚙️ تطبيق المحرك (Engine Implementation)
    // ==========================================
    public class NavigationService : INavigationService
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NavigationService(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<List<MenuGroup>> GetAllowedMenusAsync(ClaimsPrincipal user)
        {
            var allowedGroups = new List<MenuGroup>();
            var httpContext = _httpContextAccessor.HttpContext;
            var currentController = httpContext?.GetRouteValue("controller")?.ToString() ?? "";
            var currentAction = httpContext?.GetRouteValue("action")?.ToString() ?? "";

            string activeUnit = DetermineActiveUnit(currentController, currentAction);

            if (activeUnit == "Commercial")
            {
                var group = new MenuGroup { Title = "العمليات التجارية", Icon = "storefront", Items = new List<MenuItem>() };
                
                group.Items.Add(new MenuItem { Title = "لوحة التحكم", Url = "/Home/CommercialHub", Icon = "dashboard" });

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "Sales.Add") || user.IsInRole("Cashier") || user.IsInRole("Pharmacist"))
                    group.Items.Add(new MenuItem { Title = "نقطة البيع السريعة", Url = "/Sales/Create", Icon = "point_of_sale" });
                
                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "Sales.View") || user.IsInRole("BranchManager"))
                    group.Items.Add(new MenuItem { Title = "المبيعات ونقاط البيع", Url = "/Sales/Index", Icon = "receipt_long" });
                
                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "SalesReturn.View") || user.IsInRole("BranchManager"))
                    group.Items.Add(new MenuItem { Title = "مرتجع المبيعات", Url = "/SalesReturn/Index", Icon = "assignment_return" });

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "Purchases.Add") || user.IsInRole("BranchManager"))
                    group.Items.Add(new MenuItem { Title = "فواتير المشتريات", Url = "/Purchases/Create", Icon = "add_shopping_cart" });

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "Purchases.View") || user.IsInRole("BranchManager") || user.IsInRole("Storekeeper"))
                    group.Items.Add(new MenuItem { Title = "سجل المشتريات", Url = "/Purchases/Index", Icon = "shopping_cart_checkout" });

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "PurchasesReturn.View") || user.IsInRole("BranchManager"))
                    group.Items.Add(new MenuItem { Title = "مرتجع المشتريات", Url = "/PurchasesReturn/Index", Icon = "remove_shopping_cart" });

                // Reporting link mapped to unit Operations

                if (group.Items.Any()) allowedGroups.Add(group);
            }
            else if (activeUnit == "Inventory")
            {
                var group = new MenuGroup { Title = "إدارة المستودعات", Icon = "inventory_2", Items = new List<MenuItem>() };
                
                group.Items.Add(new MenuItem { Title = "لوحة تحكم المخزون", Url = "/Home/InventoryHub", Icon = "dashboard" });

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "Drug.View") || user.IsInRole("Pharmacist") || user.IsInRole("Storekeeper"))
                    group.Items.Add(new MenuItem { Title = "الأدوية والمخزون", Url = "/Drugs/Index", Icon = "medication" });

                if (user.IsInRole("SuperAdmin") || user.IsInRole("Pharmacist") || user.IsInRole("Storekeeper"))
                {
                    group.Items.Add(new MenuItem { Title = "المجموعات العلاجية", Url = "/ItemGroups/Index", Icon = "category" });
                }

                if (user.IsInRole("SuperAdmin") || user.IsInRole("Storekeeper") || user.IsInRole("Pharmacist"))
                {
                    group.Items.Add(new MenuItem { Title = "المستودعات والرفوف", Url = "/Warehouses/Index", Icon = "warehouse" });
                    group.Items.Add(new MenuItem { Title = "جرد وتسوية المخزون", Url = "/StockAudit/Index", Icon = "rule" });
                    group.Items.Add(new MenuItem { Title = "التحويلات المخزنية", Url = "/DrugTransfers/Index", Icon = "local_shipping" });
                }

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "System.ChangeBranch") || user.IsInRole("BranchManager"))
                {
                    group.Items.Add(new MenuItem { Title = "نواقص الأدوية", Url = "/Inventory/Shortages", Icon = "warning_amber" });
                    group.Items.Add(new MenuItem { Title = "رقابة صلاحية الأصناف", Url = "/Report/StockExpiry", Icon = "event_busy" });
                }

                if (group.Items.Any()) allowedGroups.Add(group);
            }
            else if (activeUnit == "Finance")
            {
                var group = new MenuGroup { Title = "المالية والحسابات", Icon = "account_balance", Items = new List<MenuItem>() };

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "Accounts.View") || user.IsInRole("Accountant"))
                {
                    // الرابط الرئيسي للوحة التحكم
                    group.Items.Add(new MenuItem { Title = "لوحة تحكم المالية", Url = "/Home/FinanceHub", Icon = "account_balance" });

                    group.Items.Add(new MenuItem { Title = "الدليل المحاسبي", Url = "/Accounting/Index", Icon = "account_tree" });
                    group.Items.Add(new MenuItem { Title = "إدارة العملاء", Url = "/Customers/Index", Icon = "groups" });
                    group.Items.Add(new MenuItem { Title = "إدارة الموردين", Url = "/Suppliers/Index", Icon = "local_shipping" });
                    group.Items.Add(new MenuItem { Title = "سجل الموظفين", Url = "/Employees/Index", Icon = "badge" });
                }

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "JournalEntries.View") || user.IsInRole("Accountant"))
                {
                    group.Items.Add(new MenuItem { Title = "التحويلات المالية", Url = "/FundTransfers/Index", Icon = "currency_exchange" });
                }

                if (user.IsInRole("SuperAdmin") || user.IsInRole("Accountant"))
                {
                    group.Items.Add(new MenuItem { Title = "كشف حساب تفصيلي", Url = "/Report/Ledger", Icon = "list_alt" });
                }

                if (group.Items.Any()) allowedGroups.Add(group);
            }
            else if (activeUnit == "Reports")
            {
                var group = new MenuGroup { Title = "التقارير والتحليلات", Icon = "donut_large", Items = new List<MenuItem>() };

                if (user.IsInRole("SuperAdmin") || user.HasClaim("Permission", "System.ChangeBranch") || user.IsInRole("BranchManager"))
                {
                    group.Items.Add(new MenuItem { Title = "مركز التقارير (اللوحة الرئيسية)", Url = "/Home/ReportsHub", Icon = "dashboard" });
                    
                    // المالية
                    group.Items.Add(new MenuItem { Title = "قائمة الدخل والأرباح", Url = "/Report/IncomeStatement", Icon = "point_of_sale" });
                    group.Items.Add(new MenuItem { Title = "ميزان المراجعة المالي", Url = "/Report/TrialBalance", Icon = "balance" });
                    group.Items.Add(new MenuItem { Title = "كشف حساب تفصيلي", Url = "/Report/Ledger", Icon = "menu_book" });
                    group.Items.Add(new MenuItem { Title = "حركة الدخل والسيولة النقدية", Url = "/Report/DailyCashFlow", Icon = "payments" });

                    // المخزون
                    group.Items.Add(new MenuItem { Title = "التنبؤ بخطة المشتريات (AI)", Url = "/InventoryIntelligence/Index", Icon = "online_prediction" });
                    group.Items.Add(new MenuItem { Title = "نواقص المخزون التشغيلية", Url = "/Inventory/Shortages", Icon = "warning" });
                    group.Items.Add(new MenuItem { Title = "مراقبة الصلاحية والاستهلاك", Url = "/Report/StockExpiry", Icon = "event_busy" });

                    // الأداء والعمليات
                    group.Items.Add(new MenuItem { Title = "انتاجية الفروع", Url = "/Report/PharmacistSales", Icon = "local_pharmacy" });
                }

                if (group.Items.Any()) allowedGroups.Add(group);
            }
            else if (activeUnit == "SystemSettings")
            {
                var group = new MenuGroup { Title = "إعدادات النظام", Icon = "admin_panel_settings", Items = new List<MenuItem>() };

                if (user.IsInRole("SuperAdmin"))
                {
                    group.Items.Add(new MenuItem { Title = "إعدادات النظام العامة", Url = "/Admin/Index", Icon = "settings" });
                    group.Items.Add(new MenuItem { Title = "الإعدادات المالية", Url = "/FinancialSettings/Index", Icon = "settings_suggest" });
                    group.Items.Add(new MenuItem { Title = "إدارة الفروع", Url = "/Branches/Index", Icon = "account_tree" });
                    group.Items.Add(new MenuItem { Title = "إدارة المستخدمين", Url = "/Users/Index", Icon = "manage_accounts" });
                    group.Items.Add(new MenuItem { Title = "مصفوفة الصلاحيات", Url = "/Roles/Index", Icon = "admin_panel_settings" });
                    group.Items.Add(new MenuItem { Title = "إدارة العملات", Url = "/Currencies/Index", Icon = "paid" });
                    group.Items.Add(new MenuItem { Title = "النسخ الاحتياطي", Url = "/Admin/Backup", Icon = "backup" });
                    group.Items.Add(new MenuItem { Title = "سجلات الرقابة", Url = "/Admin/SystemLogs", Icon = "policy" });
                }

                if (group.Items.Any()) allowedGroups.Add(group);
            }
            else
            {
                // لا يعرض الوحدات في القائمة الجانبية كعناصر يتم النقر عليها أبداً (Main Fallback is Empty)
            }

            return Task.FromResult(allowedGroups);
        }

        private string DetermineActiveUnit(string controller, string action)
        {
            if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(action, "CommercialHub", StringComparison.OrdinalIgnoreCase) || string.Equals(action, "SalesHub", StringComparison.OrdinalIgnoreCase) || string.Equals(action, "PurchasesHub", StringComparison.OrdinalIgnoreCase)) return "Commercial";
                if (string.Equals(action, "InventoryHub", StringComparison.OrdinalIgnoreCase)) return "Inventory";
                if (string.Equals(action, "FinanceHub", StringComparison.OrdinalIgnoreCase)) return "Finance";
                if (string.Equals(action, "ReportsHub", StringComparison.OrdinalIgnoreCase)) return "Reports";
                if (string.Equals(action, "AdminHub", StringComparison.OrdinalIgnoreCase) || string.Equals(action, "SettingsHub", StringComparison.OrdinalIgnoreCase)) return "SystemSettings";
                return "Main";
            }

            var commercialControllers = new[] { "Sales", "SalesReturn", "Purchases", "PurchasesReturn", "POS" };
            if (commercialControllers.Contains(controller, StringComparer.OrdinalIgnoreCase)) return "Commercial";

            var inventoryControllers = new[] { "Drugs", "ItemGroups", "Warehouses", "Barcode", "Inventory", "StockAudit", "DrugTransfers" };
            if (inventoryControllers.Contains(controller, StringComparer.OrdinalIgnoreCase)) return "Inventory";

            var financeControllers = new[] { "Accounting", "Customers", "Suppliers", "Employees", "Vouchers", "FundTransfers", "JournalEntries" };
            if (financeControllers.Contains(controller, StringComparer.OrdinalIgnoreCase)) return "Finance";

            var reportControllers = new[] { "Report", "InventoryIntelligence" };
            if (reportControllers.Contains(controller, StringComparer.OrdinalIgnoreCase))
            {
                // To keep Context specific, if we are viewing Shortages, it could be either Inventory or Reports.
                // We map generically to Reports unless they pass a flag. Let's map it to Reports for now.
                return "Reports";
            }

            var settingsControllers = new[] { "Admin", "Branches", "Users", "Roles", "Currencies", "FinancialSettings" };
            if (settingsControllers.Contains(controller, StringComparer.OrdinalIgnoreCase)) return "SystemSettings";

            return "Main";
        }
    }
}

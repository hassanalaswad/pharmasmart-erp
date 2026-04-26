using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly PharmaSmartWeb.Services.IWhatsAppService _whatsappService;
        private readonly PharmaSmartWeb.Services.NotificationEngine _notificationEngine;
        private readonly Microsoft.Extensions.Logging.ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            PharmaSmartWeb.Services.IWhatsAppService whatsappService,
            PharmaSmartWeb.Services.NotificationEngine notificationEngine,
            Microsoft.Extensions.Logging.ILogger<HomeController> logger) : base(context)
        {
            _whatsappService       = whatsappService;
            _notificationEngine    = notificationEngine;
            _logger                = logger;
        }

        public async Task<IActionResult> DashboardHub()
        {
            int scopeId = ReportScopeId;
            bool isGlobal = (scopeId == 0);
            var today = DateTime.Today;

            var branch = isGlobal ? await _context.Branches.Include(b => b.DefaultCurrency).FirstOrDefaultAsync() 
                                  : await _context.Branches.Include(b => b.DefaultCurrency).FirstOrDefaultAsync(b => b.BranchId == scopeId);
            ViewBag.CurrencySymbol = branch?.DefaultCurrency?.CurrencyCode ?? branch?.DefaultCurrency?.CurrencyName ?? "R.Y";




            var model = new DashboardViewModel
            {
                IsSuperAdmin = IsSuperAdmin,
                CanViewFinancials = (bool)(ViewData["CanViewAccountReports"] ?? false) || IsSuperAdmin,
                CanViewInventory = (bool)(ViewData["CanViewInventory"] ?? false) || IsSuperAdmin,
                ShowPosWorkspace = (bool)(ViewData["CanViewSales"] ?? false) || IsSuperAdmin
            };

            ViewBag.BranchesList = await _context.Branches.Where(b => b.IsActive == true).ToListAsync();

            // Populate system-wide logs (or branch isolated if needed)
            model.SystemLogsCount = await _context.Systemlogs.CountAsync();

            // Branch Statuses
            if (IsSuperAdmin) {
                model.BranchStatuses = await _context.Branches.Select(b => new BranchStatusOverview {
                    BranchName = b.BranchName,
                    Location = b.Location ?? string.Empty,
                    IsActive = b.IsActive == true
                }).ToListAsync();

                // Chart Data for Branches (Sales & Simple Profit for the month)
                var rawSales = await _context.Sales
                    .Where(s => s.SaleDate.Month == today.Month && s.SaleDate.Year == today.Year)
                    .GroupBy(s => s.BranchId)
                    .Select(g => new { bId = g.Key, total = g.Sum(x => x.NetAmount) })
                    .ToListAsync();
                
                var rawProfitsList = await _context.Journaldetails.Include(d => d.Journal).Include(d => d.Account)
                    .Where(d => d.Journal.IsPosted == true && d.Journal.JournalDate.Month == today.Month && d.Journal.JournalDate.Year == today.Year)
                    .Where(d => d.Account.AccountType == "Revenue" || (d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense")))
                    .ToListAsync();

                var rawProfits = rawProfitsList.GroupBy(d => d.Journal.BranchId)
                    .Select(g => new {
                        bId = g.Key,
                        profit = g.Where(x => x.Account.AccountType == "Revenue").Sum(x => x.Credit - x.Debit) - g.Where(x => x.Account.AccountType != null && x.Account.AccountType.StartsWith("Expense")).Sum(x => x.Debit - x.Credit)
                    }).ToList();

                var activeBranchesForChart = ViewBag.BranchesList as List<Branches>;
                if (activeBranchesForChart != null)
                {
                    var branchStats = activeBranchesForChart.Select(b => new {
                        BranchName = b.BranchName,
                        TotalSales = rawSales.FirstOrDefault(r => r.bId == b.BranchId)?.total ?? 0m,
                        TotalProfit = rawProfits.FirstOrDefault(r => r.bId == b.BranchId)?.profit ?? 0m
                    }).ToList();
                    ViewBag.BranchStatsJson = System.Text.Json.JsonSerializer.Serialize(branchStats);
                }
            }

            if (model.CanViewFinancials || IsSuperAdmin)
            {
                var salesQ = _context.Sales.AsQueryable();
                var journalQ = _context.Journaldetails.Include(d => d.Journal).Include(d => d.Account)
                                .Where(d => d.Journal.IsPosted == true).AsQueryable();

                if (!isGlobal)
                {
                    salesQ = salesQ.Where(s => s.BranchId == scopeId);
                    journalQ = journalQ.Where(d => d.Journal.BranchId == scopeId);
                }

                model.TodaySales = await salesQ.Where(s => s.SaleDate.Date == today).SumAsync(s => (decimal?)s.NetAmount) ?? 0m;

                var pnlToday = await journalQ.Where(d => d.Journal.JournalDate.Date == today
                                && (d.Account.AccountType == "Revenue" || (d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense")))).ToListAsync();

                model.TotalRevenues = pnlToday.Where(d => d.Account.AccountType == "Revenue").Sum(d => d.Credit - d.Debit);
                model.TotalExpenses = pnlToday.Where(d => d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense")).Sum(d => d.Debit - d.Credit);
                model.NetProfit = model.TotalRevenues - model.TotalExpenses;

                model.RecentJournals = await journalQ.OrderByDescending(j => j.JournalId).Take(4).Select(d => new JournalDetailOverview {
                    TrxNumber = "#TRX-" + d.JournalId,
                    AccountName = d.Account.AccountName,
                    Type = d.Account.AccountType == "Revenue" ? "إيرادات" : ((d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense")) ? "خصوم" : "أصول"),
                    Debit = d.Debit,
                    Credit = d.Credit
                }).ToListAsync();
            }

            if (model.CanViewInventory || IsSuperAdmin)
            {
                var invQ = _context.Branchinventory.AsQueryable();
                if (!isGlobal) invQ = invQ.Where(bi => bi.BranchId == scopeId);
                
                model.TotalStockQuantity = await invQ.SumAsync(bi => (int?)bi.StockQuantity) ?? 0;
                model.ShortagesCount = await invQ.CountAsync(bi => bi.StockQuantity <= bi.MinimumStockLevel);
                
                model.ShortageItems = await invQ.Where(bi => bi.StockQuantity <= bi.MinimumStockLevel)
                                                .Include(bi => bi.Drug)
                                                .Take(3)
                                                .Select(bi => new ShortageItemOverview {
                                                    DrugName = bi.Drug.DrugName,
                                                    ShortageAmount = bi.MinimumStockLevel - bi.StockQuantity
                                                }).ToListAsync();
                
                // Expiry Alerts
                model.ExpiringDrugs = await _context.DrugBatches.Include(b => b.Drug)
                                        .Where(b => b.ExpiryDate <= DateTime.Today.AddMonths(2))
                                        .OrderBy(b => b.ExpiryDate)
                                        .Take(3)
                                        .Select(b => new ExpiringDrugOverview {
                                            DrugName = b.Drug.DrugName,
                                            ExpiryDate = b.ExpiryDate,
                                            Quantity = b.Drug.Branchinventory.FirstOrDefault() != null ? b.Drug.Branchinventory.FirstOrDefault()!.StockQuantity : 0
                                        }).ToListAsync();
            }

            if (model.ShowPosWorkspace)
            {
                int uid = int.Parse(User.FindFirst("UserID")?.Value ?? "0");
                model.InvoicesCount = await _context.Sales.CountAsync(s => s.SaleDate.Date == today && s.UserId == uid && s.BranchId == scopeId);
            }

            return View(model);
        }

        // ==========================================================
        // 🏢 البوابات المركزية (Central Hubs)
        // ==========================================================
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "لوحة التحكم الرئيسية";
            ViewData["HideLayoutBack"] = true;

            int scopeId = ReportScopeId;
            bool isGlobal = (scopeId == 0);
            var today = DateTime.Today;

            // ===== بيانات المبيعات =====
            var salesQ = _context.Sales.AsQueryable();
            if (!isGlobal) salesQ = salesQ.Where(s => s.BranchId == scopeId);

            ViewBag.TodaySales = await salesQ
                .Where(s => s.SaleDate.Date == today && !s.IsReturn)
                .SumAsync(s => (decimal?)s.NetAmount) ?? 0m;

            ViewBag.TodayInvoicesCount = await salesQ
                .CountAsync(s => s.SaleDate.Date == today && !s.IsReturn);

            // آخر 5 فواتير
            ViewBag.RecentSales = await salesQ
                .Include(s => s.Customer)
                .Include(s => s.SalePayments)
                .Where(s => !s.IsReturn)
                .OrderByDescending(s => s.SaleId)
                .Take(5)
                .Select(s => new {
                    InvoiceId = s.SaleId,
                    CustomerName = s.Customer != null ? s.Customer.FullName : "عميل نقدي",
                    TotalAmount = s.NetAmount,
                    PaymentMethod = s.SalePayments.Any() ? s.SalePayments.FirstOrDefault()!.PaymentMethod : "كاش",
                    SaleDate = s.SaleDate
                }).ToListAsync();

            // ===== بيانات المخزون والنواقص =====
            var invQ = _context.Branchinventory.AsQueryable();
            if (!isGlobal) invQ = invQ.Where(bi => bi.BranchId == scopeId);

            ViewBag.ShortagesCount = await invQ
                .CountAsync(bi => bi.StockQuantity <= bi.MinimumStockLevel);

            ViewBag.ShortageItems = await invQ
                .Where(bi => bi.StockQuantity <= bi.MinimumStockLevel)
                .Include(bi => bi.Drug)
                .OrderBy(bi => bi.StockQuantity)
                .Take(5)
                .Select(bi => new {
                    DrugName = bi.Drug.DrugName,
                    StockQuantity = bi.StockQuantity,
                    MinimumStockLevel = bi.MinimumStockLevel,
                    ShortageAmount = bi.MinimumStockLevel - bi.StockQuantity
                }).ToListAsync();

            // ===== انتهاء الصلاحية (≤30 يوم) =====
            ViewBag.ExpiringCount = await _context.DrugBatches
                .CountAsync(b => b.ExpiryDate <= DateTime.Today.AddDays(30) && b.ExpiryDate >= DateTime.Today);

            // ===== الرصيد النقدي =====
            var cashAccountQ = _context.Journaldetails
                .Include(d => d.Journal).Include(d => d.Account)
                .Where(d => d.Account.AccountType == "Asset" && d.Account.AccountName.Contains("نقد"));
            ViewBag.CashBalance = await cashAccountQ.SumAsync(d => (decimal?)(d.Credit - d.Debit)) ?? 5200m;

            // ===== آخر 7 أيام مبيعات للرسم البياني =====
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-6 + i)).ToList();
            var dailySales7 = await salesQ
                .Where(s => !s.IsReturn && s.SaleDate.Date >= last7Days.First() && s.SaleDate.Date <= today)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.NetAmount) })
                .ToListAsync();

            var arCulture = new System.Globalization.CultureInfo("ar-EG");
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(
                last7Days.Select(d => d.ToString("ddd", arCulture)).ToList());
            ViewBag.ChartActual = System.Text.Json.JsonSerializer.Serialize(
                last7Days.Select(d => dailySales7.FirstOrDefault(x => x.Date == d)?.Total ?? 0m).ToList());
            ViewBag.ChartForecast = System.Text.Json.JsonSerializer.Serialize(
                last7Days.Select(d => 0m).ToList());

            // ===== SuperAdmin: إحصائيات الفروع =====
            if (User.IsInRole("SuperAdmin"))
            {
                var branches = await _context.Branches.Where(b => b.IsActive == true).ToListAsync();
                var branchSalesMonth = await _context.Sales
                    .Where(s => !s.IsReturn && s.SaleDate.Month == today.Month && s.SaleDate.Year == today.Year)
                    .GroupBy(s => s.BranchId)
                    .Select(g => new { BId = g.Key, Total = g.Sum(x => x.NetAmount) })
                    .ToListAsync();

                ViewBag.BranchStats = branches.Select(b => new {
                    BranchName = b.BranchName,
                    TotalSales = branchSalesMonth.FirstOrDefault(r => r.BId == b.BranchId)?.Total ?? 0m
                }).ToList();

                ViewBag.TotalBranchSales = await _context.Sales
                    .Where(s => !s.IsReturn && s.SaleDate.Date == today)
                    .SumAsync(s => (decimal?)s.NetAmount) ?? 0m;

                ViewBag.TotalShortages = await _context.Branchinventory
                    .CountAsync(bi => bi.StockQuantity <= bi.MinimumStockLevel);
            }

            // ===== Cashier: مبيعات الوردية =====
            if (User.IsInRole("Cashier") || User.IsInRole("Pharmacist"))
            {
                int uid = int.TryParse(User.FindFirst("UserID")?.Value, out int parsedUid) ? parsedUid : 0;
                var shiftSales = await salesQ
                    .Where(s => !s.IsReturn && s.SaleDate.Date == today && s.UserId == uid)
                    .SumAsync(s => (decimal?)s.NetAmount) ?? 0m;
                ViewBag.ShiftSales = shiftSales;
                ViewBag.ShiftInvoices = await salesQ
                    .CountAsync(s => !s.IsReturn && s.SaleDate.Date == today && s.UserId == uid);
            }

            return View();
        }

        public async Task<IActionResult> CommercialHub()
        {
            int scopeId = ReportScopeId;
            bool isGlobal = (scopeId == 0);
            var today = DateTime.Today;
            var currentMonth = today.Month;
            var currentYear = today.Year;

            var branch = isGlobal ? await _context.Branches.Include(b => b.DefaultCurrency).FirstOrDefaultAsync() 
                                  : await _context.Branches.Include(b => b.DefaultCurrency).FirstOrDefaultAsync(b => b.BranchId == scopeId);
            ViewBag.CurrencySymbol = branch?.DefaultCurrency?.CurrencyCode ?? branch?.DefaultCurrency?.CurrencyName ?? "R.Y";

            var model = new CommercialHubViewModel();
            model.IsCashier = User.IsInRole("Cashier") || User.HasClaim("RoleId", "4");

            var salesQ = _context.Sales.AsQueryable();
            var purQ = _context.Purchases.AsQueryable();

            if (!isGlobal) {
                salesQ = salesQ.Where(s => s.BranchId == scopeId);
                purQ = purQ.Where(p => p.BranchId == scopeId);
            }

            model.TodaySales = await salesQ.Where(s => s.SaleDate.Date == today && s.IsReturn == false).SumAsync(s => (decimal?)s.NetAmount) ?? 0m;
            model.TodayReturns = await salesQ.Where(s => s.SaleDate.Date == today && s.IsReturn == true).SumAsync(s => (decimal?)s.NetAmount) ?? 0m;
            model.TodayInvoicesCount = await salesQ.CountAsync(s => s.SaleDate.Date == today && s.IsReturn == false);
            model.TodayReturnsCount = await salesQ.CountAsync(s => s.SaleDate.Date == today && s.IsReturn == true);

            model.RecentSales = await salesQ
                .Include(s => s.Customer)
                .Include(s => s.SalePayments)
                .Where(s => s.IsReturn == false)
                .OrderByDescending(s => s.SaleId)
                .Take(5)
                .Select(s => new RecentCommercialInvoice {
                    InvoiceId = s.SaleId,
                    SaleDate = s.SaleDate,
                    CustomerName = s.Customer != null ? s.Customer.FullName : "عميل نقدي",
                    PaymentMethod = s.SalePayments.Any() ? s.SalePayments.FirstOrDefault()!.PaymentMethod : "كاش",
                    TotalAmount = s.TotalAmount
                }).ToListAsync();

            model.RecentPurchases = await purQ
                .Include(p => p.Supplier)
                .Where(p => p.IsReturn == false)
                .OrderByDescending(p => p.PurchaseId)
                .Take(5)
                .Select(p => new RecentCommercialPurchase {
                    PurchaseId = p.PurchaseId,
                    SupplierName = p.Supplier.SupplierName ?? "مورد غير محدد",
                    TotalAmount = p.TotalAmount,
                    IsPaid = p.PaymentStatus == "Paid" || p.PaymentStatus == "مدفوعة" || p.RemainingAmount <= 0
                }).ToListAsync();

            // Operations breakdown today
            model.TotalSalesOperations = model.TodayInvoicesCount;
            model.TotalPurchasesOperations = await purQ.CountAsync(p => p.PurchaseDate.Date == today && p.IsReturn == false);
            model.TotalReturnsOperations = model.TodayReturnsCount;

            // Last 7 days charts data
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).Reverse().ToList();
            
            var dailySales = await salesQ
                .Where(s => s.IsReturn == false && s.SaleDate.Date >= last7Days.First() && s.SaleDate.Date <= today)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.NetAmount) })
                .ToListAsync();

            var dailyPurchases = await purQ
                .Where(p => p.IsReturn == false && p.PurchaseDate.Date >= last7Days.First() && p.PurchaseDate.Date <= today)
                .GroupBy(p => p.PurchaseDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToListAsync();

            var arCulture = new System.Globalization.CultureInfo("ar-EG");
            foreach (var date in last7Days)
            {
                model.Last7DaysLabels.Add(date.ToString("dddd", arCulture));
                model.SalesLast7Days.Add(dailySales.FirstOrDefault(d => d.Date == date)?.Total ?? 0m);
                model.PurchasesLast7Days.Add(dailyPurchases.FirstOrDefault(d => d.Date == date)?.Total ?? 0m);
            }

            ViewData["Title"] = "وحدة العمليات التجارية";
            ViewData["PageDescription"] = "المبيعات، المشتريات، المرتجعات والعمولات";

            return View(model);
        }

        public async Task<IActionResult> InventoryHub()
        {
            var model = new InventoryHubViewModel();
            var invQuery = _context.Branchinventory.Include(bi => bi.Drug).ThenInclude(d => d.ItemGroup).AsQueryable();
            var moveQuery = _context.Stockmovements.Include(m => m.Drug).Include(m => m.User).AsQueryable();
            var batchesQuery = _context.DrugBatches.Include(b => b.Drug).AsQueryable();
            var transfersQuery = _context.Drugtransfers.AsQueryable();

            int scopeId = ReportScopeId;
            if (scopeId != 0) {
                invQuery = invQuery.Where(bi => bi.BranchId == scopeId);
                moveQuery = moveQuery.Where(m => m.BranchId == scopeId);
                transfersQuery = transfersQuery.Where(t => t.ToBranchId == scopeId);
            }

            model.TotalDrugs = await _context.Drugs.CountAsync();
            model.TotalInventoryValue = await invQuery.SumAsync(bi => bi.StockQuantity * (bi.AverageCost ?? 0));
            model.ShortagesCount = await invQuery.CountAsync(bi => bi.StockQuantity <= bi.MinimumStockLevel);
            var nextMonth = DateTime.Today.AddMonths(1);
            model.ExpiringSoonCount = await batchesQuery.CountAsync(b => b.ExpiryDate <= nextMonth && b.ExpiryDate >= DateTime.Today);
            model.WarehousesCount = await _context.Warehouses.CountAsync();
            model.ItemGroupsCount = await _context.ItemGroups.CountAsync();
            
            model.PendingTransfersCount = await transfersQuery.CountAsync(t => t.Status == "Pending");

            // Charts Data: Valuation by Group
            var groupValuations = await invQuery
                .Where(bi => bi.StockQuantity > 0)
                .GroupBy(bi => bi.Drug.ItemGroup != null ? bi.Drug.ItemGroup.GroupName : "غير مصنف")
                .Select(g => new { Group = g.Key, TotalValue = g.Sum(x => x.StockQuantity * (x.AverageCost ?? 0)) })
                .OrderByDescending(g => g.TotalValue)
                .Take(6)
                .ToListAsync();

            model.ValuationLabels = groupValuations.Select(g => g.Group).ToList();
            model.ValuationData = groupValuations.Select(g => g.TotalValue).ToList();

            // Doughnut Data: Stock Health
            int totalItemsInStock = await invQuery.CountAsync();
            if (totalItemsInStock > 0)
            {
                model.StockDangerPercentage = (int)Math.Round((double)model.ShortagesCount / totalItemsInStock * 100);
                model.StockExpiringPercentage = (int)Math.Round((double)model.ExpiringSoonCount / totalItemsInStock * 100);
                model.StockHealthyPercentage = 100 - model.StockDangerPercentage - model.StockExpiringPercentage;
                if (model.StockHealthyPercentage < 0) model.StockHealthyPercentage = 0;
            }
            else
            {
                model.StockHealthyPercentage = 100;
            }

            // Critical Alerts
            var topShortages = await invQuery
                .Where(bi => bi.StockQuantity <= bi.MinimumStockLevel)
                .OrderBy(bi => bi.StockQuantity)
                .Take(5)
                .Select(bi => new InventoryAlertModel {
                    DrugName = bi.Drug.DrugName,
                    BatchNo = "-",
                    AlertType = "نقص حاد",
                    AlertSeverity = "error", // For styling
                    AlertMessage = $"الرصيد: {bi.StockQuantity} (الحد الأدنى: {bi.MinimumStockLevel})",
                    ActionType = "طلب شراء"
                }).ToListAsync();

            var topExpiring = await batchesQuery
                .Where(b => b.ExpiryDate <= nextMonth && b.ExpiryDate >= DateTime.Today)
                .OrderBy(b => b.ExpiryDate)
                .Take(5)
                .Select(b => new InventoryAlertModel {
                    DrugName = b.Drug.DrugName,
                    BatchNo = b.BatchNumber,
                    AlertType = "قارب على الانتهاء",
                    AlertSeverity = "warning",
                    AlertMessage = $"ينتهي في: {b.ExpiryDate.ToString("yyyy-MM-dd")}",
                    ActionType = "ترويج/إرجاع"
                }).ToListAsync();

            model.CriticalAlerts.AddRange(topShortages);
            model.CriticalAlerts.AddRange(topExpiring);
            model.CriticalAlerts = model.CriticalAlerts.OrderBy(a => a.AlertSeverity == "error" ? 0 : 1).Take(6).ToList();

            // Today's Movements
            model.RecentMovements = await moveQuery
                .Where(m => m.MovementDate.Date == DateTime.Today)
                .OrderByDescending(m => m.MovementId)
                .Take(5)
                .Select(m => new RecentStockMovementModel {
                    MovementId = m.MovementId,
                    DrugName = m.Drug.DrugName ?? "غير محدد",
                    BatchNo = _context.DrugBatches.Where(b => b.DrugId == m.DrugId).Select(b => b.BatchNumber).FirstOrDefault() ?? "غير محدد",
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    Notes = m.Notes ?? "لا توجد ملاحظات",
                    MovementDate = m.MovementDate,
                    UserName = m.User.Username ?? "النظام"
                }).ToListAsync();

            ViewData["Title"] = "وحدة المخزون والمستودعات";
            ViewData["PageDescription"] = "إدارة المواد، الجرد، الصلاحيات والتحويلات";

            return View(model);
        }

        public async Task<IActionResult> FinanceHub()
        {
            var today = DateTime.Today;
            var sevenDaysAgo = today.AddDays(-6);
            var model = new FinanceHubViewModel();
            
            // 1. Core Counts
            model.JournalEntriesCount = await _context.Journalentries.Where(j => j.IsPosted != true).CountAsync();
            model.VouchersMonthCount = await _context.Vouchers.Where(v => v.VoucherDate.Month == today.Month && v.VoucherDate.Year == today.Year).CountAsync();

            // 2. Safe Balance Aggregation
            var balancesDb = await _context.Journaldetails
                                  .Where(d => d.Journal.IsPosted == true)
                                  .GroupBy(d => d.AccountId)
                                  .Select(g => new { AccountId = g.Key, Balance = (decimal?)g.Sum(x => x.Debit - x.Credit) ?? 0m })
                                  .ToListAsync();
            
            var accountsList = await _context.Accounts.Select(a => new { a.AccountId, a.AccountName, a.AccountType }).ToListAsync();
            var accountBalances = accountsList.Select(a => new 
            { 
                a.AccountId, 
                a.AccountName, 
                a.AccountType, 
                Balance = balancesDb.FirstOrDefault(b => b.AccountId == a.AccountId)?.Balance ?? 0m 
            }).ToList();

            model.TotalCash = accountBalances.Where(a => a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد")).Sum(a => a.Balance);
            model.TotalBankBalance = accountBalances.Where(a => a.AccountName.Contains("بنك") || a.AccountName.Contains("مصرف")).Sum(a => a.Balance);

            var customerAccountIds = await _context.Customers.Select(c => c.AccountId).ToListAsync();
            var supplierAccountIds = await _context.Suppliers.Select(s => s.AccountId).ToListAsync();

            model.AccountsReceivable = accountBalances.Where(a => customerAccountIds.Contains(a.AccountId)).Sum(a => a.Balance);
            model.AccountsPayable = accountBalances.Where(a => supplierAccountIds.Contains(a.AccountId)).Sum(a => -a.Balance);

            // 3. Cash Flow Chart (Last 7 Days)
            var recentVouchersList = await _context.Vouchers
                                       .Where(v => v.VoucherDate >= sevenDaysAgo)
                                       .Select(v => new { v.VoucherDate, v.VoucherType, v.Amount })
                                       .ToListAsync();

            string[] arDays = { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };

            for (int i = 0; i <= 6; i++)
            {
                var curDate = sevenDaysAgo.AddDays(i);
                model.CashFlowLabels.Add(arDays[(int)curDate.DayOfWeek]);
                
                var dayReceipts = recentVouchersList.Where(v => v.VoucherDate.Date == curDate && (v.VoucherType == "قبض" || v.VoucherType == "Receipt")).Sum(v => v.Amount);
                var dayPayments = recentVouchersList.Where(v => v.VoucherDate.Date == curDate && (v.VoucherType == "صرف" || v.VoucherType == "Payment")).Sum(v => v.Amount);

                model.CashInflows.Add(dayReceipts);
                model.CashOutflows.Add(dayPayments);
            }

            // 4. Liquidity Distribution
            var cashBankAccounts = accountBalances
                                   .Where(a => (a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد") || a.AccountName.Contains("بنك") || a.AccountName.Contains("مصرف")) && a.Balance > 0)
                                   .OrderByDescending(a => a.Balance)
                                   .Take(3)
                                   .ToList();

            foreach (var acc in cashBankAccounts)
            {
                model.LiquidityLabels.Add(acc.AccountName);
                model.LiquidityData.Add(acc.Balance);
            }

            // 5. Recent Lists
            model.RecentVouchers = await _context.Vouchers
                .OrderByDescending(v => v.VoucherDate)
                .Take(5)
                .Select(v => new RecentVoucherModel
                {
                    VoucherId = v.VoucherId,
                    VoucherType = v.VoucherType,
                    PayeePayerName = v.ToAccount.AccountName,
                    Notes = v.Description ?? "لا توجد تفاصيل",
                    Amount = v.Amount,
                    VoucherDate = v.VoucherDate
                }).ToListAsync();

            model.RecentJournals = await _context.Journalentries
                .OrderByDescending(j => j.JournalDate)
                .Take(5)
                .Select(j => new RecentJournalModel
                {
                    JournalId = j.JournalId,
                    Description = j.Description ?? "بدون بيان",
                    Source = string.IsNullOrEmpty(j.ReferenceType) ? "قيد يومية يدوي" : j.ReferenceType,
                    TotalAmount = j.Journaldetails.Where(d => d.Debit > 0).Sum(d => d.Debit),
                    IsPosted = j.IsPosted,
                    JournalDate = j.JournalDate
                }).ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> ReportsHub(int? branchId, DateTime? startDate, DateTime? endDate)
        {
            var model = new ReportsHubViewModel();
            model.IsSuperAdmin = IsSuperAdmin;
            model.CanViewFinancialReports = (bool)(ViewData["CanViewAccountReports"] ?? false) || IsSuperAdmin;
            model.CanViewInventoryReports = (bool)(ViewData["CanViewInventory"] ?? false) || IsSuperAdmin;

            // 1. Time Scope
            DateTime defaultStart = new DateTime(DateTime.Today.Year, 1, 1);
            DateTime defaultEnd = DateTime.Today;
            model.StartDate = startDate ?? defaultStart;
            model.EndDate = Math.Max((endDate ?? defaultEnd).Ticks, defaultStart.Ticks) == (endDate ?? defaultEnd).Ticks ? (endDate ?? defaultEnd) : DateTime.Today;

            // 2. Branch Scope
            int scopeId = branchId.HasValue ? branchId.Value : ReportScopeId;
            model.SelectedBranchId = scopeId;
            bool isGlobal = (scopeId == 0);

            // Restrict non-admins from viewing global or other branches
            if (!IsSuperAdmin && branchId.HasValue && branchId.Value != ReportScopeId)
            {
                isGlobal = false;
                scopeId = ReportScopeId;
                model.SelectedBranchId = scopeId;
            }

            // 3. Branches list for Dropdown (if SuperAdmin)
            if (IsSuperAdmin)
            {
                ViewBag.BranchesDropdown = await _context.Branches.Where(b => b.IsActive == true).OrderBy(b => b.BranchId).ToListAsync();
            }

            var salesQ = _context.Sales.Where(s => s.SaleDate.Date <= model.EndDate.Date);
            var returnsQ = _context.Sales.Where(s => s.SaleDate.Date <= model.EndDate.Date && s.IsReturn == true);
            var journalsQ = _context.Journaldetails.Include(d => d.Journal).Include(d => d.Account).Where(d => d.Journal.IsPosted == true && d.Journal.JournalDate.Date <= model.EndDate.Date);
            var inventoryQ = _context.Branchinventory.Include(bi => bi.Drug).AsQueryable();
            var vouchersQ = _context.Vouchers.Where(v => v.VoucherDate.Date >= model.StartDate.Date && v.VoucherDate.Date <= model.EndDate.Date);

            if (!isGlobal)
            {
                salesQ = salesQ.Where(s => s.BranchId == scopeId);
                returnsQ = returnsQ.Where(s => s.BranchId == scopeId);
                journalsQ = journalsQ.Where(d => d.Journal.BranchId == scopeId);
                inventoryQ = inventoryQ.Where(bi => bi.BranchId == scopeId);
                // Vouchers tracking is global in simplified models unless we map through journal
            }

            // A) KPIs calculation
            var revenuesThisPeriodQ = journalsQ.Where(d => d.Journal.JournalDate.Date >= model.StartDate.Date && d.Account.AccountType == "Revenue");
            var expensesThisPeriodQ = journalsQ.Where(d => d.Journal.JournalDate.Date >= model.StartDate.Date && d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense"));

            var totalRevenues = await revenuesThisPeriodQ.SumAsync(d => d.Credit - d.Debit);
            var totalExpenses = await expensesThisPeriodQ.SumAsync(d => d.Debit - d.Credit);
            
            model.TotalRevenuesYTD = totalRevenues;
            model.NetProfit = totalRevenues - totalExpenses;
            model.ProfitMargin = totalRevenues > 0 ? Math.Round((model.NetProfit / totalRevenues) * 100, 2) : 0;

            var allBalances = await journalsQ.GroupBy(d => d.AccountId)
                                   .Select(g => new { AccountId = g.Key, Balance = (decimal?)g.Sum(x => x.Debit - x.Credit) ?? 0m })
                                   .ToListAsync();

            var cashBankAccounts = await _context.Accounts.Where(a => a.AccountName.Contains("نقد") || a.AccountName.Contains("صندوق") || a.AccountName.Contains("بنك") || a.AccountName.Contains("مصرف")).Select(a => a.AccountId).ToListAsync();

            model.TotalLiquidity = allBalances.Where(b => cashBankAccounts.Contains(b.AccountId)).Sum(b => b.Balance);

            model.TotalAssetsValue = await inventoryQ.SumAsync(bi => bi.StockQuantity * (bi.AverageCost ?? 0));
            model.BranchesCount = isGlobal ? await _context.Branches.CountAsync() : 1;

            // B) P&L Trend (Monthly)
            var pnlQuery = await journalsQ
                .Where(d => d.Journal.JournalDate.Date >= model.StartDate.Date && (d.Account.AccountType == "Revenue" || (d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense"))))
                .GroupBy(d => new { d.Journal.JournalDate.Year, d.Journal.JournalDate.Month, d.Account.AccountType })
                .Select(g => new { 
                    g.Key.Year, 
                    g.Key.Month, 
                    g.Key.AccountType, 
                    Credits = g.Sum(x => x.Credit),
                    Debits = g.Sum(x => x.Debit)
                })
                .ToListAsync();

            var arCulture = new System.Globalization.CultureInfo("ar-EG");
            var currentMonthPnl = model.StartDate.Date.AddDays(1 - model.StartDate.Day);
            while (currentMonthPnl <= model.EndDate)
            {
                model.PnlLabels.Add(currentMonthPnl.ToString("MMMM yyyy", arCulture));
                model.PnlRevenues.Add(pnlQuery.Where(p => p.Year == currentMonthPnl.Year && p.Month == currentMonthPnl.Month && p.AccountType == "Revenue").Sum(p => p.Credits - p.Debits));
                model.PnlExpenses.Add(pnlQuery.Where(p => p.Year == currentMonthPnl.Year && p.Month == currentMonthPnl.Month && p.AccountType != null && p.AccountType.StartsWith("Expense")).Sum(p => p.Debits - p.Credits));
                currentMonthPnl = currentMonthPnl.AddMonths(1);
            }

            // C) Branch Comparison
            if (isGlobal)
            {
                var branchSales = await salesQ.Where(s => s.SaleDate.Date >= model.StartDate.Date)
                    .GroupBy(s => new { s.BranchId, s.IsReturn })
                    .Select(g => new { 
                        g.Key.BranchId, 
                        g.Key.IsReturn, 
                        TotalNetAmount = g.Sum(x => x.NetAmount), 
                        TotalAmount = g.Sum(x => x.TotalAmount) 
                    })
                    .ToListAsync();
                var branchesList = await _context.Branches.ToListAsync();
                foreach(var b in branchesList)
                {
                    var salesData = branchSales.Where(x => x.BranchId == b.BranchId && !x.IsReturn).Sum(x => x.TotalNetAmount);
                    var returnsData = branchSales.Where(x => x.BranchId == b.BranchId && x.IsReturn).Sum(x => x.TotalAmount);
                    if (salesData > 0 || returnsData > 0)
                    {
                        model.BranchLabels.Add(b.BranchName);
                        model.BranchSales.Add(salesData);
                        model.BranchReturns.Add(returnsData);
                    }
                }
            }
            else 
            {
                var activeBranch = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == scopeId);
                model.BranchLabels.Add(activeBranch?.BranchName ?? "الفرع الحالي");
                model.BranchSales.Add(await salesQ.Where(s => !s.IsReturn && s.SaleDate.Date >= model.StartDate.Date).SumAsync(s => (decimal?)s.NetAmount) ?? 0m);
                model.BranchReturns.Add(await salesQ.Where(s => s.IsReturn && s.SaleDate.Date >= model.StartDate.Date).SumAsync(s => (decimal?)s.TotalAmount) ?? 0m);
            }

            // D) ABC Analysis Mocked Logic
            var inventoryList = await inventoryQ.Where(bi => bi.StockQuantity > 0).Select(bi => new { TotalValue = bi.StockQuantity * (bi.AverageCost ?? 0) }).ToListAsync();
            var totalVal = inventoryList.Sum(x => x.TotalValue);
            
            if (totalVal > 0)
            {
                var sorted = inventoryList.OrderByDescending(x => x.TotalValue).ToList();
                decimal aVal = 0, bVal = 0, cVal = 0;
                for(int i=0; i<sorted.Count; i++) {
                    if (aVal < totalVal * 0.70m) aVal += sorted[i].TotalValue;
                    else if (bVal < totalVal * 0.20m) bVal += sorted[i].TotalValue;
                    else cVal += sorted[i].TotalValue;
                }
                model.AbcCategoryA = Math.Round((aVal / totalVal) * 100, 1);
                model.AbcCategoryB = Math.Round((bVal / totalVal) * 100, 1);
                model.AbcCategoryC = Math.Round((cVal / totalVal) * 100, 1);
            }
            else
            {
                model.AbcCategoryA = 70; model.AbcCategoryB = 20; model.AbcCategoryC = 10;
            }

            // E) Cash Flow Trend
            var cashFlowMonths = await vouchersQ
                .GroupBy(v => new { v.VoucherDate.Year, v.VoucherDate.Month, v.VoucherType })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.VoucherType, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            var currentMonthCash = model.StartDate.Date.AddDays(1 - model.StartDate.Day);
            while (currentMonthCash <= model.EndDate)
            {
                model.CashFlowLabels.Add(currentMonthCash.ToString("MMMM yyyy", arCulture));
                model.CashInflows.Add(cashFlowMonths.Where(c => c.Year == currentMonthCash.Year && c.Month == currentMonthCash.Month && (c.VoucherType == "قبض" || c.VoucherType == "Receipt")).Sum(c => c.Total));
                model.CashOutflows.Add(cashFlowMonths.Where(c => c.Year == currentMonthCash.Year && c.Month == currentMonthCash.Month && (c.VoucherType == "صرف" || c.VoucherType == "Payment")).Sum(c => c.Total));
                currentMonthCash = currentMonthCash.AddMonths(1);
            }

            return View(model);
        }

        public async Task<IActionResult> AdminHub()
        {
            var model = new AdminHubViewModel();
            model.EmployeesCount = await _context.Employees.CountAsync();
            model.UsersCount = await _context.Users.CountAsync();
            model.ActiveBranches = await _context.Branches.CountAsync(b => b.IsActive == true);
            model.RolesCount = await _context.Userroles.CountAsync();
            model.SystemErrorsCount = await _context.Systemlogs.CountAsync(l => l.Action.Contains("Error") || (l.Details != null && l.Details.Contains("Error")) || l.Action.Contains("Exception"));
            model.TodayLogsCount = await _context.Systemlogs.CountAsync(l => l.CreatedAt.Date == DateTime.Today);
            
            if (User.IsInRole("SuperAdmin") || User.HasClaim("RoleId", "1") || User.IsInRole("Accountant") || User.HasClaim("RoleId", "3"))
            {
                model.TotalSalaries = await _context.Employees.SumAsync(e => (decimal?)e.Salary) ?? 0m;
            }

            return View(model);
        }

        // ============================================================
        // 🔔 مركز الإشعارات — يقرأ من قاعدة البيانات ثم يحذف (read = delete)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            ViewData["Title"] = "مركز الإشعارات";
            ViewData["PageDescription"] = "تنبيهات المخزون والصلاحية والتحديثات الإدارية";

            int scopeId = ReportScopeId;

            try { await _notificationEngine.GenerateAndSaveNotificationsAsync(scopeId); }
            catch (Exception ex) { _logger.LogWarning(ex, "NotificationEngine: خطأ أثناء التوليد."); }

            try {
                var saved = await _context.SystemNotifications
                    .Where(n => n.BranchId == 0 || n.BranchId == scopeId)
                    .OrderByDescending(n => n.Severity == "critical" ? 2 : n.Severity == "warning" ? 1 : 0)
                    .ThenByDescending(n => n.CreatedAt)
                    .ToListAsync();

                var viewItems = saved.Select(n => new NotificationItemVm
                {
                    Id         = n.Id,
                    Category   = n.Category,
                    Severity   = n.Severity,
                    Icon       = n.Icon,
                    IconColor  = n.IconColor,
                    BgColor    = n.BgColor,
                    BadgeColor = n.BadgeColor,
                    Title      = n.Title,
                    Body       = n.Body,
                    ActionUrl  = n.ActionUrl,
                    ActionText = n.ActionText,
                    OccurredAt = n.CreatedAt,
                    IsRead     = n.IsRead
                }).ToList();

                ViewBag.TotalCount     = viewItems.Count;
                ViewBag.UnreadCount    = viewItems.Count(i => !i.IsRead);
                ViewBag.CriticalCount  = viewItems.Count(i => i.Severity == "critical" && !i.IsRead);
                ViewBag.WarningCount   = viewItems.Count(i => i.Severity == "warning" && !i.IsRead);
                ViewBag.InventoryCount = viewItems.Count(i => (i.Category is "shortage" or "expiry" or "inventory") && !i.IsRead);
                ViewBag.AdminCount     = viewItems.Count(i => i.Category == "admin" && !i.IsRead);

                var unread = saved.Where(n => !n.IsRead).ToList();
                if (unread.Any())
                {
                    unread.ForEach(n => n.IsRead = true);
                    await _context.SaveChangesAsync();
                }

                return View(viewItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في شاشة التنبيهات");
                ViewBag.TotalCount = 0;
                ViewBag.UnreadCount = 0;
                return View(new List<NotificationItemVm>());
            }
        }


        public IActionResult SettingsHub()

        {
            var model = new SettingsHubViewModel();
            model.IsSuperAdmin = User.IsInRole("SuperAdmin") || User.HasClaim("RoleId", "1");
            model.IsPharmacist = User.IsInRole("Pharmacist") || User.HasClaim("RoleId", "2");

            if (!model.IsSuperAdmin && !model.IsPharmacist)
                return RedirectToAction("AccessDenied");

            return View(model);
        }
        
        // 🛑 دالة رفض الوصول (لحل مشكلة 404 عند منع الصلاحية)
        // ==========================================================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return Content(@"<!DOCTYPE html>
                <html dir='rtl' lang='ar'>
                <head>
                    <meta charset='utf-8'>
                    <title>عذراً - وصول مرفوض</title>
                    <link href='/css/local-fonts.css' rel='stylesheet'>
                </head>
                <body style='font-family:""Cairo"", Arial; text-align:center; padding-top:100px; background:#f8fafc; direction:rtl;'>
                    <div style='background:white; padding:50px; border-radius:30px; box-shadow:0 10px 30px rgba(0,0,0,0.05); max-width:600px; margin:auto; border:1px solid #f1f5f9;'>
                        <h1 style='color:#e11d48; font-size:80px; margin:0 0 20px 0;'>🛑</h1>
                        <h2 style='color:#0f172a; font-size:30px; margin-bottom:10px;'>عذراً، الوصول مرفوض!</h2>
                        <h3 style='color:#64748b; font-size:16px; margin-bottom:40px; font-weight:600;'>ليس لديك الصلاحية الكافية للوصول إلى هذه الشاشة أو تنفيذ هذه العملية.</h3>
                        <a href='/Home/Index' style='display:inline-block; padding:15px 40px; background:#0f172a; color:white; text-decoration:none; border-radius:15px; font-weight:bold; transition:all 0.3s;'>العودة للرئيسية</a>
                    </div>
                </body>
                </html>", "text/html");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeActiveBranch(int branchId, string returnUrl)
        {
            if (IsSuperAdmin)
            {
                Response.Cookies.Append("ActiveBranchId", branchId.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTime.Now.AddDays(7) });
            }
            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }


    }

    // ViewModel للإشعارات
    public class NotificationItemVm
    {
        public int Id { get; set; }
        public string Category   { get; set; } = ""; // inventory | admin
        public string Severity   { get; set; } = ""; // critical | warning | info
        public string Icon       { get; set; } = "notifications";
        public string IconColor  { get; set; } = "text-slate-500";
        public string BgColor    { get; set; } = "bg-slate-50 border-slate-200";
        public string BadgeColor { get; set; } = "bg-slate-400";
        public string Title      { get; set; } = "";
        public string Body       { get; set; } = "";
        public string ActionUrl  { get; set; } = "#";
        public string ActionText { get; set; } = "عرض";
        public DateTime OccurredAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}

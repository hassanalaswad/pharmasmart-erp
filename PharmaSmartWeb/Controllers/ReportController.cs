using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace PharmaSmartWeb.Controllers
{
    // تم نقل الموديلات (ViewModels) إلى ملف DashboardViewModels.cs مسبقاً
    // لذلك نتركها هنا كتعليقات أو يمكن حذفها نهائياً لتنظيف الكود

    [Authorize]
    public class ReportController : BaseController
    {
        public ReportController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📊 0. لوحة تحكم مركز التقارير (الرئيسية)
        // ==========================================
        // ==========================================
        // 📊 0. لوحة تحكم مركز التقارير (الرئيسية)
        // ==========================================
        [HttpGet]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> ReportCenter()
        {
            // 🚀 استخدام المظلة الآمنة للتقارير (تسمح بـ 0 للمدير العام لرؤية كل المؤسسة)
            int branchId = ReportScopeId;
            bool isGlobalScope = (branchId == 0); // هل نحن في وضع المؤسسة ككل؟

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var threeMonthsLater = today.AddMonths(3);

            var model = new ReportCenterViewModel();

            // 1. استعلام المبيعات (الإيرادات والحركات)
            var salesQ = _context.Sales.Where(s => s.SaleDate >= startOfMonth);
            if (!isGlobalScope) salesQ = salesQ.Where(s => s.BranchId == branchId);

            model.MonthlyRevenue = await salesQ.SumAsync(s => (decimal?)s.NetAmount) ?? 0m;
            model.TotalTransactions = await salesQ.CountAsync();

            // 2. استعلام المصروفات من القيود اليومية (Expenses)
            var expQ = _context.Journaldetails
                .Include(j => j.Journal)
                .Include(j => j.Account)
                .Where(j => j.Journal.JournalDate >= startOfMonth && j.Journal.IsPosted == true && j.Account.AccountType == "Expense");
            if (!isGlobalScope) expQ = expQ.Where(j => j.Journal.BranchId == branchId);

            model.MonthlyExpenses = await expQ.SumAsync(j => (decimal?)j.Debit - (decimal?)j.Credit) ?? 0m;

            // 3. استعلام صلاحية الأدوية (Expiry)
            var expiryQ = _context.Purchasedetails
                .Include(pd => pd.Purchase)
                .Where(pd => pd.RemainingQuantity > 0);
            if (!isGlobalScope) expiryQ = expiryQ.Where(pd => pd.Purchase.BranchId == branchId);

            model.ExpiredItemsCount = await expiryQ.CountAsync(pd => pd.ExpiryDate <= today);
            model.NearExpiryCount = await expiryQ.CountAsync(pd => pd.ExpiryDate > today && pd.ExpiryDate <= threeMonthsLater);

            // 🚀 تمرير حالة النطاق للواجهة لتغيير الألوان ديناميكياً (بدون استخدام كوكيز في الواجهة)
            ViewBag.IsGlobalScope = isGlobalScope;

            // 🚀 جلب بيانات المخطط البياني للمقارنة بين الفروع (يظهر فقط عند اختيار كافة الفروع)
            if (isGlobalScope)
            {
                // جلب أسماء الفروع أولاً
                var allBranchList = await _context.Branches
                    .Where(b => b.IsActive == true)
                    .Select(b => new { b.BranchId, b.BranchName })
                    .ToListAsync();

                // مبيعات كل فرع (من جدول المبيعات مباشرة)
                var rawSales = await _context.Sales
                    .Where(s => s.SaleDate >= startOfMonth)
                    .GroupBy(s => s.BranchId)
                    .Select(g => new { BranchId = g.Key, TotalSales = g.Sum(x => (decimal?)x.TotalAmount) ?? 0m })
                    .ToListAsync();

                // مصروفات كل فرع (من القيود)
                var rawExpenses = await _context.Journaldetails
                    .Include(j => j.Journal)
                    .Include(j => j.Account)
                    .Where(j => j.Journal.JournalDate >= startOfMonth 
                             && j.Journal.IsPosted == true 
                             && j.Account.AccountCode.StartsWith("5"))
                    .GroupBy(j => j.Journal.BranchId)
                    .Select(g => new { BranchId = g.Key, TotalExpenses = g.Sum(x => x.Debit - x.Credit) })
                    .ToListAsync();

                // إيرادات كل فرع (من القيود)
                var rawRevenues = await _context.Journaldetails
                    .Include(j => j.Journal)
                    .Include(j => j.Account)
                    .Where(j => j.Journal.JournalDate >= startOfMonth 
                             && j.Journal.IsPosted == true 
                             && j.Account.AccountCode.StartsWith("4"))
                    .GroupBy(j => j.Journal.BranchId)
                    .Select(g => new { BranchId = g.Key, TotalRevenues = g.Sum(x => x.Credit - x.Debit) })
                    .ToListAsync();

                var chartLabels   = new List<string>();
                var chartSales    = new List<decimal>();
                var chartExpenses = new List<decimal>();
                var chartRevenues = new List<decimal>();

                foreach (var branch in allBranchList)
                {
                    chartLabels.Add(branch.BranchName);
                    chartSales   .Add(rawSales   .FirstOrDefault(x => x.BranchId == branch.BranchId)?.TotalSales    ?? 0);
                    chartExpenses.Add(rawExpenses.FirstOrDefault(x => x.BranchId == branch.BranchId)?.TotalExpenses ?? 0);
                    chartRevenues.Add(rawRevenues.FirstOrDefault(x => x.BranchId == branch.BranchId)?.TotalRevenues ?? 0);
                }

                ViewBag.ComparisonLabels   = System.Text.Json.JsonSerializer.Serialize(chartLabels);
                ViewBag.ComparisonSales    = System.Text.Json.JsonSerializer.Serialize(chartSales);
                ViewBag.ComparisonExpenses = System.Text.Json.JsonSerializer.Serialize(chartExpenses);
                ViewBag.ComparisonRevenues = System.Text.Json.JsonSerializer.Serialize(chartRevenues);
            }

            return View(model);
        }

        // ==========================================
        // 💵 1. تقرير حركة الصندوق اليومية
        // ==========================================
        [HttpGet]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> DailyCashFlow(int? accountId, DateTime? date)
        {
            int branchId = ReportScopeId;
            var targetDate = date?.Date ?? DateTime.Today;
            ViewBag.TargetDate = targetDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            ViewBag.CashAccounts = await _context.Accounts
                .Where(a => a.IsActive == true && (a.AccountName.Contains("صندوق") || a.AccountName.Contains("بنك") || a.AccountName.Contains("نقد")))
                .OrderBy(a => a.AccountCode)
                .ToListAsync();

            if (accountId == null) return View(new List<Journaldetails>());

            var selectedAccount = await _context.Accounts.FindAsync(accountId);
            ViewBag.SelectedAccount = selectedAccount;

            if (selectedAccount == null) return View(new List<Journaldetails>());

            // 🚀 الرصيد الافتتاحي
            var openingQuery = _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => d.AccountId == accountId && d.Journal.JournalDate < targetDate && d.Journal.IsPosted == true)
                .AsQueryable();

            if (branchId != 0) openingQuery = openingQuery.Where(d => d.Journal.BranchId == branchId);
            var openingBalance = await openingQuery.SumAsync(d => d.Debit - d.Credit);

            // 🚀 حركات اليوم
            var movementQuery = _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => d.AccountId == accountId &&
                            d.Journal.JournalDate >= targetDate &&
                            d.Journal.JournalDate < targetDate.AddDays(1) &&
                            d.Journal.IsPosted == true)
                .AsQueryable();

            if (branchId != 0) movementQuery = movementQuery.Where(d => d.Journal.BranchId == branchId);
            var dailyMovements = await movementQuery.OrderBy(d => d.Journal.JournalDate).ToListAsync();

            ViewBag.OpeningBalance = openingBalance;
            ViewBag.TotalDebit = dailyMovements.Sum(m => m.Debit);
            ViewBag.TotalCredit = dailyMovements.Sum(m => m.Credit);
            ViewBag.ClosingBalance = openingBalance + (dailyMovements.Sum(m => m.Debit - m.Credit));

            return View(dailyMovements);
        }

        // ==========================================
        // 📉 2. تقرير الأرباح والخسائر
        // ==========================================
        [HttpGet]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> ProfitAndLoss(DateTime? fromDate, DateTime? toDate)
        {
            int branchId = ReportScopeId;
            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, 1, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59) ?? DateTime.Now;

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            var query = _context.Journaldetails
                .AsNoTracking()
                .Include(d => d.Journal)
                .Include(d => d.Account)
                .Where(d => d.Journal.JournalDate >= start &&
                            d.Journal.JournalDate <= end &&
                            d.Journal.IsPosted == true &&
                            (d.Account.AccountType == "Revenue" || d.Account.AccountType == "Expenses"))
                .AsQueryable();

            if (branchId != 0) query = query.Where(d => d.Journal.BranchId == branchId);

            var pnlData = await query.ToListAsync();

            var revenueAccounts = pnlData
                .Where(d => d.Account.AccountType == "Revenue")
                .GroupBy(d => new { d.Account.AccountCode, d.Account.AccountName })
                .Select(g => new PnlAccountViewModel
                {
                    Name = g.Key.AccountName,
                    Code = g.Key.AccountCode,
                    Total = g.Sum(x => x.Credit - x.Debit)
                }).ToList();

            var expenseAccounts = pnlData
                .Where(d => d.Account.AccountType == "Expenses")
                .GroupBy(d => new { d.Account.AccountCode, d.Account.AccountName })
                .Select(g => new PnlAccountViewModel
                {
                    Name = g.Key.AccountName,
                    Code = g.Key.AccountCode,
                    Total = g.Sum(x => x.Debit - x.Credit)
                }).ToList();

            decimal totalRevenue = revenueAccounts.Sum(r => r.Total);
            decimal totalExpenses = expenseAccounts.Sum(e => e.Total);

            ViewBag.Revenues = revenueAccounts;
            ViewBag.Expenses = expenseAccounts;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.NetProfit = totalRevenue - totalExpenses;

            ViewBag.ExpenseLabels = expenseAccounts.Select(e => e.Name).ToArray();
            ViewBag.ExpenseValues = expenseAccounts.Select(e => e.Total).ToArray();
            ViewBag.ComparisonLabels = new string[] { "إجمالي الإيرادات", "إجمالي المصروفات" };
            ViewBag.ComparisonValues = new decimal[] { totalRevenue, totalExpenses };

            return View();
        }

        // ==========================================
        // 📜 4. قائمة الدخل الاحترافية
        // ==========================================
        [HttpGet]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> IncomeStatement(DateTime? fromDate, DateTime? toDate)
        {
            int branchId = ReportScopeId;
            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59) ?? DateTime.Now;

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            var query = _context.Journaldetails
                .AsNoTracking()
                .Include(d => d.Journal)
                .Include(d => d.Account)
                .Where(d => d.Journal.JournalDate >= start && d.Journal.JournalDate <= end && d.Journal.IsPosted == true)
                .AsQueryable();

            if (branchId != 0) query = query.Where(d => d.Journal.BranchId == branchId);

            var allData = await query.ToListAsync();

            var salesTotal = allData.Where(d => d.Account.AccountCode.StartsWith("4")).Sum(d => d.Credit - d.Debit);
            var cogsTotal = allData.Where(d => d.Account.AccountCode.StartsWith("511") || d.Account.AccountCode.StartsWith("512")).Sum(d => d.Debit - d.Credit);

            var opExpenses = allData.Where(d => d.Account.AccountCode.StartsWith("5") &&
                                                !d.Account.AccountCode.StartsWith("511") &&
                                                !d.Account.AccountCode.StartsWith("512"))
                .GroupBy(d => new { d.Account.AccountCode, d.Account.AccountName })
                .Select(g => new PnlAccountViewModel
                {
                    Name = g.Key.AccountName,
                    Code = g.Key.AccountCode,
                    Total = g.Sum(x => x.Debit - x.Credit),
                    Percentage = salesTotal > 0 ? (double)(g.Sum(x => x.Debit - x.Credit) / salesTotal * 100) : 0
                }).ToList();

            ViewBag.Sales = salesTotal;
            ViewBag.COGS = cogsTotal;
            ViewBag.GrossProfit = salesTotal - cogsTotal;
            ViewBag.OperatingExpenses = opExpenses;
            ViewBag.TotalOpExpenses = opExpenses.Sum(e => e.Total);
            ViewBag.NetIncome = (decimal)ViewBag.GrossProfit - (decimal)ViewBag.TotalOpExpenses;

            ViewBag.GrossMargin = salesTotal > 0 ? (ViewBag.GrossProfit / salesTotal * 100) : 0;
            ViewBag.NetMargin = salesTotal > 0 ? (ViewBag.NetIncome / salesTotal * 100) : 0;

            return View();
        }

        // ==========================================
        // 👨‍⚕️ 5. تقرير إنتاجية الفروع
        // ==========================================
        [HttpGet]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> PharmacistSales(DateTime? fromDate, DateTime? toDate, int? branchId)
        {
            int selectedBranchId = branchId ?? ReportScopeId;
            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59) ?? DateTime.Now;

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");
            ViewBag.SelectedBranchId = selectedBranchId;
            ViewBag.BranchesList = await _context.Branches.Where(b => b.IsActive == true).ToListAsync();

            var jQuery = _context.Journaldetails
                .Include(d => d.Journal)
                .ThenInclude(j => j.Branch)
                .Include(d => d.Account)
                .Where(d => d.Journal.JournalDate >= start && d.Journal.JournalDate <= end && d.Journal.IsPosted == true)
                .AsQueryable();

            if (selectedBranchId != 0) jQuery = jQuery.Where(d => d.Journal.BranchId == selectedBranchId);

            var journalData = await jQuery.ToListAsync();

            var salesInvoices = await _context.Sales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end && (selectedBranchId == 0 || s.BranchId == selectedBranchId))
                .GroupBy(s => s.BranchId)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BranchId, x => x.Count);

            var branchData = journalData
                .GroupBy(d => new { d.Journal.BranchId, d.Journal.Branch.BranchName })
                .Select(g => {
                    var totalSales = g.Where(x => x.Account.AccountCode.StartsWith("4")).Sum(x => x.Credit - x.Debit);
                    var cogs = g.Where(x => x.Account.AccountCode.StartsWith("511") || x.Account.AccountCode.StartsWith("512")).Sum(x => x.Debit - x.Credit);
                    var expenses = g.Where(x => x.Account.AccountCode.StartsWith("5") && !x.Account.AccountCode.StartsWith("511") && !x.Account.AccountCode.StartsWith("512")).Sum(x => x.Debit - x.Credit);
                    
                    return new BranchProductivityViewModel
                    {
                        BranchId = g.Key.BranchId,
                        BranchName = g.Key.BranchName ?? "غير محدد",
                        InvoiceCount = salesInvoices.ContainsKey(g.Key.BranchId) ? salesInvoices[g.Key.BranchId] : 0,
                        TotalSales = totalSales,
                        TotalExpenses = expenses,
                        COGS = cogs,
                        NetProfit = totalSales - cogs - expenses
                    };
                })
                .OrderByDescending(x => x.TotalSales)
                .ToList();

            decimal grandTotalSales = branchData.Sum(x => x.TotalSales);
            foreach (var item in branchData)
            {
                item.ContributionPercentage = grandTotalSales > 0 ? (double)(item.TotalSales / grandTotalSales * 100) : 0;
            }

            ViewBag.GrandTotalSales = grandTotalSales;
            ViewBag.TotalProfits = branchData.Sum(x => x.NetProfit);
            ViewBag.TotalExpenses = branchData.Sum(x => x.TotalExpenses);
            ViewBag.IsGlobalScope = (selectedBranchId == 0);

            // Fetch Branch details for Report Layout Header
            if (selectedBranchId != 0)
            {
                var currentBranch = await _context.Branches.FindAsync(selectedBranchId);
                if (currentBranch != null)
                {
                    ViewBag.ReportBranchName = currentBranch.BranchName;
                    ViewBag.ReportBranchAddress = currentBranch.Location ?? "-";
                    ViewBag.ReportBranchPhone = "-";
                }
            }
            else
            {
                ViewBag.ReportBranchName = "كافة الفروع";
                ViewBag.ReportBranchAddress = "-";
                ViewBag.ReportBranchPhone = "-";
            }

            // Serialize for Chart.js
            ViewBag.ChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(branchData.Select(x => x.BranchName).ToArray());
            ViewBag.ChartSales = Newtonsoft.Json.JsonConvert.SerializeObject(branchData.Select(x => x.TotalSales).ToArray());
            ViewBag.ChartProfits = Newtonsoft.Json.JsonConvert.SerializeObject(branchData.Select(x => x.NetProfit).ToArray());
            ViewBag.ChartExpenses = Newtonsoft.Json.JsonConvert.SerializeObject(branchData.Select(x => x.TotalExpenses).ToArray());

            return View(branchData);
        }

        // ==========================================
        // 🧠 6. التنبؤ الذكي بالنواقص
        // ==========================================
        [HttpGet]
        [HasPermission("ShortageForecast", "View")]
        public async Task<IActionResult> ShortageForecast()
        {
            try
            {
                int branchId = ReportScopeId;
                var drugs = await _context.Drugs.Where(d => d.IsActive == true).ToListAsync();
                var forecastList = new List<ShortageForecastViewModel>();

                foreach (var drug in drugs)
                {
                    var historyQuery = _context.Saledetails.Include(sd => sd.Sale)
                        .Where(sd => sd.DrugId == drug.DrugId).AsQueryable();

                    if (branchId != 0) historyQuery = historyQuery.Where(sd => sd.Sale.BranchId == branchId);

                    var rawHistory = await historyQuery.Select(sd => new { sd.Sale.SaleDate, sd.Quantity }).ToListAsync();
                    var salesHistory = rawHistory
                        .Select(sd => new { date = sd.SaleDate.ToString("yyyy-MM-dd"), quantity = sd.Quantity })
                        .ToList();

                    decimal predictedDemand = 0;

                    if (salesHistory != null && salesHistory.Count > 5)
                    {
                        predictedDemand = await CallProphetModel(salesHistory);
                    }
                    else if (salesHistory != null && salesHistory.Any())
                    {
                        predictedDemand = (decimal)salesHistory.Average(x => x.quantity) * 30;
                    }

                    var stockQuery = _context.Branchinventory.Where(p => p.DrugId == drug.DrugId).AsQueryable();
                    if (branchId != 0) stockQuery = stockQuery.Where(p => p.BranchId == branchId);

                    var currentStock = await stockQuery.SumAsync(p => p.StockQuantity);

                    forecastList.Add(new ShortageForecastViewModel
                    {
                        DrugId = drug.DrugId,
                        DrugName = drug.DrugName,
                        CurrentStock = currentStock,
                        MonthlyForecast = Math.Round(predictedDemand, 0),
                        SuggestedOrder = (predictedDemand > currentStock) ? (predictedDemand - currentStock) : 0,
                        RiskLevel = (currentStock < (predictedDemand * 0.2m)) ? "High" : (currentStock < predictedDemand) ? "Medium" : "Safe"
                    });
                }

                return View(forecastList.OrderByDescending(x => x.SuggestedOrder).ToList());
            }
            catch (Exception ex)
            {
                return Content($"Internal Server Error: {ex.Message} - {ex.InnerException?.Message}");
            }
        }

        private async Task<decimal> CallProphetModel(object historyData)
        {
            try
            {
                string jsonInput = JsonSerializer.Serialize(historyData);
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "demand_forecast.py");

                if (!System.IO.File.Exists(scriptPath)) return 0;

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = "python";
                start.Arguments = $"\"{scriptPath}\"";
                start.UseShellExecute = false;
                start.RedirectStandardInput = true;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;

                using (Process process = Process.Start(start))
                {
                    if (process == null) return 0;

                    using (StreamWriter writer = process.StandardInput)
                    {
                        await writer.WriteAsync(jsonInput);
                    }

                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = await reader.ReadToEndAsync();
                        if (decimal.TryParse(result, out decimal forecast))
                        {
                            return forecast;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
            return 0;
        }

        // ==========================================
        // ⏳ 7. تقرير رقابة الصلاحية
        // ==========================================
        [HttpGet]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> StockExpiry(string filter)
        {
            int branchId = ReportScopeId;
            var today = DateTime.Today;
            var threeMonthsLater = today.AddMonths(3);
            var sixMonthsLater = today.AddMonths(6);

            var query = _context.Purchasedetails
                .Include(d => d.Drug)
                .Include(d => d.Purchase)
                .Where(d => d.RemainingQuantity > 0)
                .AsQueryable();

            if (branchId != 0) query = query.Where(d => d.Purchase.BranchId == branchId);

            if (filter == "expired")
                query = query.Where(i => i.ExpiryDate <= today);
            else if (filter == "near")
                query = query.Where(i => i.ExpiryDate > today && i.ExpiryDate <= threeMonthsLater);

            var items = await query.OrderBy(i => i.ExpiryDate)
                .Select(d => new StockExpiryViewModel
                {
                    ItemName = d.Drug.DrugName,
                    Barcode = d.Drug.Barcode,
                    BatchNumber = d.BatchNumber ?? "غير محدد",
                    Quantity = d.RemainingQuantity,
                    UnitName = d.Drug.MainUnit,
                    ExpiryDate = d.ExpiryDate
                }).ToListAsync();

            var statsQuery = _context.Purchasedetails.Include(p => p.Purchase).Where(d => d.RemainingQuantity > 0).AsQueryable();
            if (branchId != 0) statsQuery = statsQuery.Where(d => d.Purchase.BranchId == branchId);

            ViewBag.TotalExpired = await statsQuery.CountAsync(d => d.ExpiryDate <= today);
            ViewBag.TotalNear = await statsQuery.CountAsync(d => d.ExpiryDate > today && d.ExpiryDate <= threeMonthsLater);
            ViewBag.TotalSafe = await statsQuery.CountAsync(d => d.ExpiryDate > sixMonthsLater);

            ViewBag.CurrentFilter = filter;

            return View(items);
        }

        // ==========================================
        // 📖 9. تقرير كشف الحساب التفصيلي (Ledger)
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "View")]
        public async Task<IActionResult> Ledger(int? accountId, DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Accounts = await _context.Accounts
                .Where(a => a.IsActive == true)
                .OrderBy(a => a.AccountCode)
                .ToListAsync();

            if (accountId == null) return View(new List<Journaldetails>());

            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var selectedAccount = await _context.Accounts.FindAsync(accountId);
            ViewBag.SelectedAccount = selectedAccount;

            if (selectedAccount == null) return View(new List<Journaldetails>());

            ViewBag.FromDate = start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.ToDate = end.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            // جلب كافة الحسابات الفرعية التابعة لهذا الحساب لعرض كشف شامل إذا كان حساباً رئيسياً
            var targetAccountIds = await _context.Accounts
                .Where(a => a.AccountCode.StartsWith(selectedAccount.AccountCode))
                .Select(a => a.AccountId)
                .ToListAsync();

            var transactions = await _context.Journaldetails
                .AsNoTracking()
                .Include(d => d.Journal)
                .Include(d => d.Account)
                .Where(d => targetAccountIds.Contains(d.AccountId) &&
                            d.Journal.JournalDate >= start &&
                            d.Journal.JournalDate <= end &&
                            d.Journal.IsPosted == true &&
                            d.Journal.BranchId == ActiveBranchId)
                .OrderBy(d => d.Journal.JournalDate)
                .ToListAsync();

            decimal openingDebit = await _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => targetAccountIds.Contains(d.AccountId) &&
                            d.Journal.JournalDate < start &&
                            d.Journal.IsPosted == true &&
                            d.Journal.BranchId == ActiveBranchId)
                .SumAsync(d => (decimal?)d.Debit) ?? 0m;

            decimal openingCredit = await _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => targetAccountIds.Contains(d.AccountId) &&
                            d.Journal.JournalDate < start &&
                            d.Journal.IsPosted == true &&
                            d.Journal.BranchId == ActiveBranchId)
                .SumAsync(d => (decimal?)d.Credit) ?? 0m;

            if (selectedAccount.AccountNature)
            {
                ViewBag.OpeningBalance = openingDebit - openingCredit;
            }
            else
            {
                ViewBag.OpeningBalance = openingCredit - openingDebit;
            }

            return View(transactions);
        }

        // ==========================================
        // 📊 10. ميزان المراجعة الهرمي
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "View")]
        public async Task<IActionResult> TrialBalance()
        {
            // جلب كافة الحسابات النشطة (AsNoTracking لمنع تتبع الكائنات)
            var allAccounts = await _context.Accounts
                .AsNoTracking()
                .Where(a => a.IsActive == true)
                .ToListAsync();

            // 🚀 هندسة الأداء (Performance): حساب الأرصدة بتجميع SQL ذكي للحد من سحب ملايين السجلات لـ RAM
            var accountBalances = await _context.Journaldetails
                .AsNoTracking()
                .Where(jd => jd.Journal.IsPosted == true && jd.Journal.BranchId == ActiveBranchId)
                .GroupBy(jd => jd.AccountId)
                .Select(g => new {
                    AccountId = g.Key,
                    TotalDebit = g.Sum(x => x.Debit),
                    TotalCredit = g.Sum(x => x.Credit)
                })
                .ToListAsync();

            // 🚀 الخطوة (1): حساب الأرصدة المباشرة لكل حساب من واقع القيود المجمعة
            foreach (var acc in allAccounts)
            {
                var bal = accountBalances.FirstOrDefault(ab => ab.AccountId == acc.AccountId);
                
                decimal totalD = bal?.TotalDebit ?? 0m;
                decimal totalC = bal?.TotalCredit ?? 0m;

                // الرصيد الأولي (الحركات المباشرة فقط على هذا الحساب)
                acc.Balance = acc.AccountNature ? (totalD - totalC) : (totalC - totalD);
            }

            // 🚀 الخطوة (2): محرك التجميع التصاعدي (Bottom-Up Roll-up)
            // نبدأ من الحسابات الأكثر عمقاً (أطول كود) ونصعد للأباء
            var orderedByDepth = allAccounts.OrderByDescending(a => a.AccountCode.Length).ToList();
            foreach (var acc in orderedByDepth)
            {
                if (acc.ParentAccountId.HasValue)
                {
                    var parent = allAccounts.FirstOrDefault(p => p.AccountId == acc.ParentAccountId.Value);
                    if (parent != null)
                    {
                        // ترحيل رصيد الابن ليُجمع في رصيد الأب
                        parent.Balance += acc.Balance;
                    }
                }
            }

            // 🚀 الخطوة (3): حساب إجماليات الميزان النهائية (من الحسابات الجذرية فقط لمنع التكرار)
            decimal exactTotalDebit = 0m;
            decimal exactTotalCredit = 0m;
            var rootAccounts = allAccounts.Where(a => a.ParentAccountId == null).ToList();

            foreach (var root in rootAccounts)
            {
                // الحساب الجذري يحتوي الآن على مجموع توازنات شجرته بالكامل
                if (root.AccountNature) // أصول أو مصروفات
                {
                    if (root.Balance > 0) exactTotalDebit += root.Balance;
                    else if (root.Balance < 0) exactTotalCredit += Math.Abs(root.Balance);
                }
                else // التزامات، ملكية، إيرادات
                {
                    if (root.Balance > 0) exactTotalCredit += root.Balance;
                    else if (root.Balance < 0) exactTotalDebit += Math.Abs(root.Balance);
                }
            }

            ViewBag.ExactTotalDebit = exactTotalDebit;
            ViewBag.ExactTotalCredit = exactTotalCredit;

            // 🚀 الخطوة (4): إخفاء الحسابات الصفرية وإعادة الترتيب
            var trialBalanceList = allAccounts
                .Where(a => Math.Round(a.Balance, 2) != 0) 
                .OrderBy(a => a.AccountCode)
                .ToList();

            return View(trialBalanceList);
        }
    }
}

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (ReportController) المحدث
=============================================================================================
الوظيفة العامة: 
هذا الكنترولر هو "محرك ذكاء الأعمال والتقارير" للنظام (BI & Reporting Engine).
يقوم بجمع، تصنيف، وتحليل البيانات التشغيلية (المالية والمخزنية) لعرضها في تقارير تفصيلية.

ملاحظة معمارية جوهرية (تحديث النطاق ReportScopeId):
- 🚀 تم استبدال `ActiveBranchId` بـ `ReportScopeId` لكي نسمح للمدير العام برؤية البيانات 
  "المجمعة" لكافة الفروع عند اختياره "كافة الفروع" (حيث يمرر قيمة 0).
- تم استخدام أسلوب البناء الديناميكي الصارم للاستعلامات `if (branchId != 0)` بدلاً من 
  شروط `OR (||)` داخل الـ `Where`، وذلك لضمان التجميع المثالي وعدم حدوث ارتباك لمحرك 
  قواعد البيانات (Entity Framework Core) أثناء الترجمة إلى SQL.
=============================================================================================
*/

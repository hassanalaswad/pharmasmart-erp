using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using ExcelDataReader;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class InventoryIntelligenceController : BaseController
    {
        private readonly PharmaSmartWeb.Services.IForecastApiService _forecastApiService;

        public InventoryIntelligenceController(
            ApplicationDbContext context,
            PharmaSmartWeb.Services.IForecastApiService forecastApiService)
            : base(context)
        {
            _forecastApiService = forecastApiService;
        }

        [HttpGet]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> Index()
        {
            int currentBranchId = ActiveBranchId;
            var invQuery = _context.Branchinventory.AsQueryable();
            if (currentBranchId > 0) invQuery = invQuery.Where(b => b.BranchId == currentBranchId);

            var abcCounts = await invQuery
                .GroupBy(b => b.Abccategory)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.CountA = abcCounts.FirstOrDefault(x => x.Category == "A")?.Count ?? 0;
            ViewBag.CountB = abcCounts.FirstOrDefault(x => x.Category == "B")?.Count ?? 0;
            ViewBag.CountC = abcCounts.FirstOrDefault(x => x.Category == "C")?.Count ?? 0;

            return View("EOQABC");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Inventory", "Edit")]
        public async Task<IActionResult> CalculateABC()
        {
            if (!IsSuperAdmin) return Unauthorized("هذه العملية تتطلب صلاحيات مدير النظام.");

            int currentBranchId = ActiveBranchId;

            var salesQuery = _context.Saledetails.Include(sd => sd.Sale).AsQueryable();
            if (currentBranchId > 0) salesQuery = salesQuery.Where(sd => sd.Sale.BranchId == currentBranchId);

            var drugSales = await salesQuery
                .GroupBy(sd => sd.DrugId)
                .Select(g => new { DrugId = g.Key, TotalRevenue = g.Sum(sd => sd.Quantity * sd.UnitPrice) })
                .OrderByDescending(d => d.TotalRevenue)
                .ToListAsync();

            decimal grandTotalRevenue = drugSales.Sum(d => d.TotalRevenue);
            if (grandTotalRevenue == 0) return BadRequest("لا توجد مبيعات كافية للتحليل.");

            var drugIds = drugSales.Select(d => d.DrugId).Distinct().ToList();
            var allInventoryRows = await _context.Branchinventory
                .Where(b => drugIds.Contains(b.DrugId) && (currentBranchId == 0 || b.BranchId == currentBranchId))
                .ToListAsync();
            var inventoryByDrug = allInventoryRows.GroupBy(b => b.DrugId).ToDictionary(g => g.Key, g => g.ToList());

            decimal runningTotal = 0;

            foreach (var item in drugSales)
            {
                runningTotal += item.TotalRevenue;
                decimal cumulativePercentage = (runningTotal / grandTotalRevenue) * 100;

                char category = 'C';
                if (cumulativePercentage <= 80) category = 'A';
                else if (cumulativePercentage <= 95) category = 'B';

                if (!inventoryByDrug.TryGetValue(item.DrugId, out var inventoryItems))
                    continue;

                foreach (var inv in inventoryItems)
                    inv.Abccategory = category.ToString();
            }

            await _context.SaveChangesAsync();
            return Ok("تم تحديث تصنيف ABC بنجاح وتم دمجها مع معادلات التنبؤ.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> GenerateSmartPlan([FromQuery] string model = "prophet")
        {
            int currentBranchId = ActiveBranchId == 0 ? 1 : ActiveBranchId;
            
            // 1. استخراج النواقص
            var inventoryQuery = _context.Branchinventory.Include(b => b.Drug).Where(b => b.BranchId == currentBranchId).AsQueryable();
            var rawInventory = await inventoryQuery.ToListAsync();
            var shortages = rawInventory.Where(b => b.StockQuantity <= (b.MinimumStockLevel == 0 ? 10 : b.MinimumStockLevel)).ToList();

            if (!shortages.Any())
                return BadRequest("لا توجد نواقص في المخزون الحالي لتوليد الخطة.");

            // 2. تجنب توليد أكثر من خطة مسودة لنفس الفرع في نفس اليوم (اختياري، سنسمح بذلك ونحفظها)
            var plan = new PurchasePlan
            {
                BranchId = currentBranchId,
                CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "1"),
                Status = "Draft",
                Notes = $"خطة مشتريات ذكية مبنية على التحليل المتقدم - {DateTime.Now:yyyy-MM-dd}"
            };
            _context.PurchasePlans.Add(plan);
            await _context.SaveChangesAsync(); 

            decimal accumulatedCost = 0;
            decimal totalLiquidity = await _context.Journaldetails
                .Where(jd => jd.Journal.IsPosted == true &&
                             jd.Account.IsActive == true && jd.Account.IsParent == false &&
                             (jd.Account.AccountName.Contains("صندوق") || jd.Account.AccountName.Contains("بنك")) &&
                             (currentBranchId == 0 || jd.Journal.BranchId == currentBranchId))
                .SumAsync(jd => jd.Debit - jd.Credit);

            foreach (var item in shortages)
            {
                // أ. تجهيز المبيعات للبروفيت (أخر 6 أشهر كافية للسرعة)
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var salesData = await _context.Saledetails.Include(sd => sd.Sale)
                    .Where(sd => sd.DrugId == item.DrugId && sd.Sale.BranchId == currentBranchId && sd.Sale.SaleDate >= sixMonthsAgo)
                    .GroupBy(sd => sd.Sale.SaleDate.Date)
                    .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), quantity = g.Sum(x => x.Quantity) })
                    .ToListAsync();
                
                string jsonPayload = System.Text.Json.JsonSerializer.Serialize(salesData);
                
                // ب. استدعاء الذكاء الاصطناعي للتنبؤ
                decimal forecastedDemand = 0;
                decimal forecastAccuracy = 0;

                if (model == "vertex" && salesData.Any())
                {
                    var salesPoints = salesData
                        .Select(s => new PharmaSmartWeb.Services.SalesDataPoint(s.date, s.quantity))
                        .ToList();

                    var forecastResult = await _forecastApiService.GetForecastAsync(
                        drugName: item.Drug?.DrugName ?? "Unknown Drug",
                        salesHistory: salesPoints);

                    forecastedDemand = forecastResult.ForecastedDemand;
                    forecastAccuracy = forecastResult.Accuracy;
                }
                else if (salesData.Any())
                {
                    try
                    {
                        string pyOut = await CallProphetScriptAsync(jsonPayload);
                        var pyResult = System.Text.Json.JsonSerializer.Deserialize<ProphetResult>(pyOut, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (pyResult != null)
                        {
                            forecastedDemand = pyResult.Forecast;
                            forecastAccuracy = pyResult.Accuracy;
                        }
                    }
                    catch { } // Fallback
                }

                if (forecastedDemand <= 0)
                {
                    // Fallback to average sales
                    forecastedDemand = salesData.Sum(s => s.quantity); 
                    if (forecastedDemand == 0) forecastedDemand = 20; // Default minimum guess
                }

                // ج. معالجة EOQ 
                decimal currentWAC = (item.AverageCost != null && item.AverageCost > 0) ? item.AverageCost.Value : 1m;
                decimal annualDemand_D = forecastedDemand * 12; 
                decimal orderingCost_S = 5000;
                decimal holdingCostPercentage_H = 0.15m;

                string abc = item.Abccategory ?? "C";
                if (abc == "A") { orderingCost_S = 2000; holdingCostPercentage_H = 0.30m; }
                else if (abc == "C") { orderingCost_S = 10000; holdingCostPercentage_H = 0.05m; }

                decimal holdingCost_H = currentWAC * holdingCostPercentage_H <= 0 ? 1 : currentWAC * holdingCostPercentage_H;
                int recommendedQty = (int)Math.Round(Math.Sqrt((double)((2 * annualDemand_D * orderingCost_S) / holdingCost_H)));

                // د. استثناءات المنقذة للحياة
                bool isLifeSaving = false;
                if (item.Drug.DrugName != null && (item.Drug.DrugName.Contains("Insulin") || item.Drug.IsLifeSaving == true))
                {
                    isLifeSaving = true;
                    int safeBuffer = (item.MinimumStockLevel == 0 ? 10 : item.MinimumStockLevel) * 4;
                    if (recommendedQty < safeBuffer) recommendedQty = safeBuffer;
                }

                // هـ. الميزانية
                decimal itemTotalCost = recommendedQty * currentWAC;
                accumulatedCost += itemTotalCost;
                string status = accumulatedCost <= totalLiquidity ? "في حدود الميزانية" : "عجز مالي - مؤجل";

                // و. حفظ
                _context.PurchasePlanDetails.Add(new PurchasePlanDetail
                {
                    PlanId = plan.PlanId,
                    DrugId = item.DrugId,
                    CurrentStock = item.StockQuantity,
                    ABCCategory = isLifeSaving ? "🚑 منقذ" : abc,
                    ForecastedDemand = Math.Round(forecastedDemand, 0),
                    ForecastAccuracy = Math.Round(forecastAccuracy, 2),
                    ProposedQuantity = recommendedQty,
                    ApprovedQuantity = recommendedQty,
                    UnitCostEstimate = currentWAC,
                    TotalCost = itemTotalCost,
                    IsLifeSaving = isLifeSaving,
                    Status = status
                });
            }

            plan.EstimatedTotalCost = accumulatedCost;
            plan.Notes += $" | مزود التنبؤ: {(model == "vertex" ? "Google Vertex AI 🧠" : "Prophet Model 🐍")}";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, planId = plan.PlanId, message = "تم التوليد بنجاح" });
        }

        private class ExcelSalesRecord {
            public string DrugName { get; set; } = string.Empty;
            public DateTime SaleDate { get; set; }
            public int Quantity { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Inventory", "View")]
        public IActionResult ExtractExcelHeaders(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0) return BadRequest("ملف غير صالح.");
            
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var headers = new List<string>();
            
            try {
                using (var stream = excelFile.OpenReadStream())
                using (var reader = excelFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? ExcelReaderFactory.CreateCsvReader(stream) : ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration() { ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true } });
                    var table = result.Tables[0];
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var headerName = table.Columns[i].ColumnName;
                        headers.Add(string.IsNullOrWhiteSpace(headerName) ? $"Column {i}" : headerName);
                    }
                }
                return Ok(new { success = true, headers = headers });
            } catch (Exception ex) { return BadRequest($"فشل قراءة الملف الأجنبي ({ex.Message})، تأكد أنه بصيغة مساعدة (xlsx, xls, csv)."); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> GenerateSmartPlanFromExcel([FromForm] IFormFile excelFile, [FromForm] int drugCol, [FromForm] int dateCol, [FromForm] int qtyCol)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest("يرجى اختيار ملف الإكسل.");

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var externalSales = new List<ExcelSalesRecord>();
            
            using (var stream = excelFile.OpenReadStream())
            {
                using (var reader = excelFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? ExcelReaderFactory.CreateCsvReader(stream) : ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });
                    
                    var table = result.Tables[0];
                    int maxIndex = Math.Max(drugCol, Math.Max(dateCol, qtyCol));

                    foreach (System.Data.DataRow row in table.Rows)
                    {
                        if (row.ItemArray.Length > maxIndex)
                        {
                            if (row[drugCol] != null && row[dateCol] != null && row[qtyCol] != null)
                            {
                                string drugName = row[drugCol].ToString().Trim();
                                if (!string.IsNullOrEmpty(drugName) && 
                                    DateTime.TryParse(row[dateCol].ToString(), out DateTime sDate))
                                {
                                    string qtyStr = row[qtyCol].ToString();
                                    int finalQty = 0;
                                    if (int.TryParse(qtyStr, out int qty)) { finalQty = qty; }
                                    else if (double.TryParse(qtyStr, out double dQty)) { finalQty = (int)dQty; }
                                    
                                    if (finalQty > 0)
                                        externalSales.Add(new ExcelSalesRecord { DrugName = drugName, SaleDate = sDate, Quantity = finalQty });
                                }
                            }
                        }
                    }
                }
            }

            if (!externalSales.Any())
                return BadRequest("ملف الإكسل فارغ أو لم يطابق البيانات المختارة (تحقق من صحة اختيار الأعمدة الخاصة بالدواء، التاريخ، والكمية).");

            int currentBranchId = ReportScopeId > 0 ? ReportScopeId : 1;

            var shortages = await _context.Branchinventory
                .Include(i => i.Drug)
                .Where(i => i.BranchId == currentBranchId && i.StockQuantity <= i.MinimumStockLevel)
                .ToListAsync();

            if (!shortages.Any())
                return BadRequest("لا توجد نواقص في المخزون الحالي لتوليد الخطة.");

            var plan = new PurchasePlan
            {
                BranchId = currentBranchId,
                CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "1"),
                Status = "Draft",
                Notes = $"خطة مشتريات مبنية على مبيعات خارجية (Excel) - {DateTime.Now:yyyy-MM-dd}"
            };
            _context.PurchasePlans.Add(plan);
            await _context.SaveChangesAsync(); 

            decimal accumulatedCost = 0;
            decimal totalLiquidity = await _context.Journaldetails
                .Where(jd => jd.Journal.IsPosted == true &&
                             jd.Account.IsActive == true && jd.Account.IsParent == false &&
                             (jd.Account.AccountName.Contains("صندوق") || jd.Account.AccountName.Contains("بنك")) &&
                             (currentBranchId == 0 || jd.Journal.BranchId == currentBranchId))
                .SumAsync(jd => jd.Debit - jd.Credit);

            var groupedExcel = externalSales
                .GroupBy(s => new { DrugName = s.DrugName.ToLower(), Date = s.SaleDate.Date })
                .Select(g => new { DrugName = g.Key.DrugName, Date = g.Key.Date.ToString("yyyy-MM-dd"), Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            foreach (var item in shortages)
            {
                string targetNameLower = item.Drug?.DrugName?.Trim().ToLower() ?? "";
                
                var salesData = groupedExcel
                    .Where(e => e.DrugName == targetNameLower)
                    .Select(e => new { date = e.Date, quantity = e.Quantity })
                    .ToList();
                
                // ── Google Vertex AI Forecasting (للبيانات الخارجية - Excel) ──────
                decimal forecastedDemand = 0;
                decimal forecastAccuracy = 0;
                string forecastSource = "Average";

                if (salesData.Any())
                {
                    var salesPoints = salesData
                        .Select(s => new PharmaSmartWeb.Services.SalesDataPoint(s.date, s.quantity))
                        .ToList();

                    var forecastResult = await _forecastApiService.GetForecastAsync(
                        drugName: item.Drug?.DrugName ?? targetNameLower,
                        salesHistory: salesPoints);

                    forecastedDemand = forecastResult.ForecastedDemand;
                    forecastAccuracy = forecastResult.Accuracy;
                    forecastSource   = forecastResult.Source;
                }

                if (forecastedDemand <= 0)
                {
                    forecastedDemand = salesData.Sum(s => s.quantity);
                    if (forecastedDemand == 0) continue;
                    forecastSource = "Sum (Fallback)";
                }

                decimal currentWAC = (item.AverageCost != null && item.AverageCost > 0) ? item.AverageCost.Value : 1m;
                decimal annualDemand_D = forecastedDemand * 12; 
                decimal orderingCost_S = 5000;
                decimal holdingCostPercentage_H = 0.15m;

                string abc = item.Abccategory ?? "C";
                if (abc == "A") { orderingCost_S = 2000; holdingCostPercentage_H = 0.30m; }
                else if (abc == "C") { orderingCost_S = 10000; holdingCostPercentage_H = 0.05m; }

                decimal holdingCost_H = currentWAC * holdingCostPercentage_H <= 0 ? 1 : currentWAC * holdingCostPercentage_H;
                int recommendedQty = (int)Math.Round(Math.Sqrt((double)((2 * annualDemand_D * orderingCost_S) / holdingCost_H)));

                bool isLifeSaving = false;
                if (item.Drug.DrugName != null && (item.Drug.DrugName.Contains("Insulin") || item.Drug.IsLifeSaving == true))
                {
                    isLifeSaving = true;
                    int safeBuffer = (item.MinimumStockLevel == 0 ? 10 : item.MinimumStockLevel) * 4;
                    if (recommendedQty < safeBuffer) recommendedQty = safeBuffer;
                }

                decimal itemTotalCost = recommendedQty * currentWAC;
                accumulatedCost += itemTotalCost;
                string status = accumulatedCost <= totalLiquidity ? "في حدود الميزانية" : "عجز مالي - مؤجل";

                _context.PurchasePlanDetails.Add(new PurchasePlanDetail
                {
                    PlanId = plan.PlanId,
                    DrugId = item.DrugId,
                    CurrentStock = item.StockQuantity,
                    ABCCategory = isLifeSaving ? "🚑 منقذ" : abc,
                    ForecastedDemand = Math.Round(forecastedDemand, 0),
                    ForecastAccuracy = Math.Round(forecastAccuracy, 2),
                    ProposedQuantity = recommendedQty,
                    ApprovedQuantity = recommendedQty,
                    UnitCostEstimate = currentWAC,
                    TotalCost = itemTotalCost,
                    IsLifeSaving = isLifeSaving,
                    Status = status
                });
            }

            plan.EstimatedTotalCost = accumulatedCost;
            plan.Notes += $" | مزود التنبؤ: {(shortages.Any() ? "Google Vertex AI" : "N/A")}";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, planId = plan.PlanId, message = "تم التوليد بنجاح من بيانات الإكسل (Google Vertex AI)" });
        }

        private const string ProphetFallbackJson = "{\"forecast\": 0, \"accuracy\": 0}";

        private static async Task<string> CallProphetScriptAsync(string jsonPayload)
        {
            try
            {
                var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "demand_forecast.py");
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return ProphetFallbackJson;

                using (var sw = process.StandardInput)
                {
                    if (sw.BaseStream.CanWrite)
                        sw.Write(jsonPayload);
                }

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                var exitTask = process.WaitForExitAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var finished = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);
                if (finished != exitTask)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    return ProphetFallbackJson;
                }

                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
                return await stdoutTask.ConfigureAwait(false);
            }
            catch
            {
                return ProphetFallbackJson;
            }
        }
        
        private class ProphetResult
        {
            public decimal Forecast { get; set; }
            public decimal Accuracy { get; set; }
        }
        // ==========================================
        // 🖨️ 5. طباعة الخطة الشرائية الذكية كتقرير رسمي
        // ==========================================
        [HttpGet]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> SmartPlanPrint(int id)
        {
            var plan = await _context.PurchasePlans
                .Include(p => p.PlanDetails)
                .ThenInclude(d => d.Drug)
                .FirstOrDefaultAsync(p => p.PlanId == id);

            if (plan == null) return NotFound("خطة الشراء غير موجودة!");

            if (!IsSuperAdmin && plan.BranchId != ActiveBranchId)
                return RedirectToAction("AccessDenied", "Home");

            return View(plan);
        }
    }
}
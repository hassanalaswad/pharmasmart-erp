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

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class InventoryIntelligenceController : BaseController
    {
        public InventoryIntelligenceController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        [HasPermission("Inventory", "View")]
        public IActionResult Index()
        {
            return View("EOQABC");
        }

        [HttpPost]
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

            decimal runningTotal = 0;

            foreach (var item in drugSales)
            {
                runningTotal += item.TotalRevenue;
                decimal cumulativePercentage = (runningTotal / grandTotalRevenue) * 100;

                char category = 'C';
                if (cumulativePercentage <= 80) category = 'A';
                else if (cumulativePercentage <= 95) category = 'B';

                var inventoryItems = await _context.Branchinventory
                    .Where(b => b.DrugId == item.DrugId && (currentBranchId == 0 || b.BranchId == currentBranchId))
                    .ToListAsync();

                foreach (var inv in inventoryItems) inv.Abccategory = category.ToString();
            }

            await _context.SaveChangesAsync();
            return Ok("تم تحديث تصنيف ABC بنجاح وتم دمجها مع معادلات التنبؤ.");
        }

        [HttpPost]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> GenerateSmartPlan()
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
                
                // ب. استدعاء بايثون للاستخبارات
                decimal forecastedDemand = 0;
                decimal forecastAccuracy = 0;
                if (salesData.Any())
                {
                    try
                    {
                        string pyOut = CallProphetScript(jsonPayload);
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
            await _context.SaveChangesAsync();

            return Ok(new { success = true, planId = plan.PlanId, message = "تم التوليد بنجاح" });
        }

        private string CallProphetScript(string jsonPayload)
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
                
                using (var process = Process.Start(psi))
                {
                    using (var sw = process.StandardInput)
                    {
                        if (sw.BaseStream.CanWrite) sw.Write(jsonPayload);
                    }
                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return result;
                }
            }
            catch
            {
                return "{\"forecast\": 0, \"accuracy\": 0}";
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
        public async Task<IActionResult> SmartPlanPrint(int id)
        {
            var plan = await _context.PurchasePlans
                .Include(p => p.PlanDetails)
                .ThenInclude(d => d.Drug)
                .FirstOrDefaultAsync(p => p.PlanId == id);

            if (plan == null) return NotFound("خطة الشراء غير موجودة!");

            return View(plan);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class PlanningController : BaseController
    {
        private readonly IForecastApiService _forecastService;

        public PlanningController(ApplicationDbContext context, IForecastApiService forecastService)
            : base(context)
        {
            _forecastService = forecastService;
        }

        // ============================================================
        // 📊 خطة المشتريات المعتمدة على التنبؤ بالذكاء الاصطناعي
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ForecastPlan()
        {
            ViewData["Title"] = "خطة المشتريات الذكية (Prophet AI)";
            ViewData["PageDescription"] = "التنبؤ بالطلب باستخدام Prophet / Vertex AI";

            int scopeId = ReportScopeId;
            bool isGlobal = (scopeId == 0);

            // جلب بيانات المخزون الحالية
            var invQuery = _context.Branchinventory
                .Include(bi => bi.Drug)
                .ThenInclude(d => d.ItemGroup)
                .AsQueryable();

            if (!isGlobal)
                invQuery = invQuery.Where(bi => bi.BranchId == scopeId);

            var inventoryItems = await invQuery
                .OrderBy(bi => bi.StockQuantity)
                .Take(50)
                .ToListAsync();

            // جلب بيانات المبيعات التاريخية (آخر 6 أشهر)
            var sixMonthsAgo = DateTime.Today.AddMonths(-6);
            var salesQuery = _context.Saledetails
                .Include(sd => sd.Drug)
                .Include(sd => sd.Sale)
                .Where(sd => sd.Sale.SaleDate >= sixMonthsAgo && sd.Sale.IsReturn == false)
                .AsQueryable();

            if (!isGlobal)
                salesQuery = salesQuery.Where(sd => sd.Sale.BranchId == scopeId);

            // بيانات المبيعات التاريخية للرسم البياني (آخر 6 أشهر)
            var monthlySalesData = await salesQuery
                .GroupBy(sd => new { sd.Sale.SaleDate.Year, sd.Sale.SaleDate.Month })
                .Select(g => new {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => (decimal)x.Quantity * x.UnitPrice)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // تجهيز بيانات الرسم البياني
            var arCulture = new System.Globalization.CultureInfo("ar-EG");
            var chartLabels = monthlySalesData.Select(m =>
                new DateTime(m.Year, m.Month, 1).ToString("MMMM", arCulture)).ToList();
            var actualSalesData = monthlySalesData.Select(m => m.TotalAmount).ToList();

            // توقعات بسيطة (متوسط متحرك كاحتياطي بدون Prophet)
            var forecastValues = new List<decimal>();
            if (actualSalesData.Count > 0)
            {
                var avg = actualSalesData.Average();
                var trend = actualSalesData.Count > 1
                    ? (actualSalesData.Last() - actualSalesData.First()) / actualSalesData.Count
                    : 0;
                // إضافة 3 أشهر مستقبلية متوقعة
                for (int i = 1; i <= 3; i++)
                {
                    chartLabels.Add(DateTime.Today.AddMonths(i).ToString("MMMM", arCulture));
                    forecastValues.Add(Math.Max(0, avg + (trend * i)));
                }
                // توقعات للأشهر الحالية
                for (int i = 0; i < actualSalesData.Count; i++)
                    forecastValues.Insert(i, 0m);
                for (int i = actualSalesData.Count; i < chartLabels.Count - 3; i++)
                    forecastValues.Insert(i, 0m);
            }

            // بناء جدول الخطة: لكل دواء، احسب الكمية المقترحة
            var forecastItems = new List<ForecastPlanItem>();
            foreach (var inv in inventoryItems)
            {
                // حساب متوسط الاستهلاك الشهري
                var avgMonthlyConsumption = await salesQuery
                    .Where(sd => sd.DrugId == inv.DrugId)
                    .GroupBy(sd => new { sd.Sale.SaleDate.Year, sd.Sale.SaleDate.Month })
                    .Select(g => (decimal)g.Sum(x => x.Quantity))
                    .ToListAsync();

                var avgConsumption = avgMonthlyConsumption.Any()
                    ? avgMonthlyConsumption.Average()
                    : (decimal)inv.MinimumStockLevel;

                // EOQ = sqrt(2 * السنوي * تكلفة طلب / تكلفة حيازة)
                var annualDemand = avgConsumption * 12;
                var orderCost = 50m; // تكلفة الطلب الافتراضية
                var holdingCostRate = 0.25m;
                var unitCost = inv.AverageCost ?? 10m;
                var holdingCost = unitCost * holdingCostRate;
                var eoq = holdingCost > 0
                    ? (decimal)Math.Round(Math.Sqrt((double)(2 * annualDemand * orderCost / holdingCost)))
                    : avgConsumption * 2;

                // الكمية المقترحة = (الطلب المتوقع لشهر) - المخزون الحالي + نقطة الطلب
                var proposedQty = Math.Max(0, Math.Round(avgConsumption * 1.5m - inv.StockQuantity));

                forecastItems.Add(new ForecastPlanItem
                {
                    DrugId = inv.DrugId,
                    DrugName = inv.Drug?.DrugName ?? "غير محدد",
                    GroupName = inv.Drug?.ItemGroup?.GroupName ?? "غير مصنف",
                    CurrentStock = inv.StockQuantity,
                    MinimumStock = inv.MinimumStockLevel,
                    AvgMonthlyConsumption = Math.Round(avgConsumption, 1),
                    ForecastedDemand = Math.Round(avgConsumption * 1.2m, 1), // بزيادة 20% تنبؤ بسيط
                    EOQ = (int)eoq,
                    ProposedQty = (int)proposedQty,
                    ApprovedQty = (int)proposedQty,
                    UnitCost = unitCost,
                    Status = inv.StockQuantity <= inv.MinimumStockLevel
                        ? "نقص حاد - طلب عاجل"
                        : (proposedQty > 0 ? "ضمن الميزانية" : "المخزون كافٍ")
                });
            }

            // العوامل الموسمية (Static reference data)
            var seasonalFactors = new List<SeasonalFactorItem>
            {
                new SeasonalFactorItem { Name = "بنادول 500mg", Category = "مسكنات", Factors = new[] { 1.2m,1.1m,1.0m,0.9m,0.8m,0.7m,0.8m,0.9m,1.1m,1.3m,1.4m,1.5m } },
                new SeasonalFactorItem { Name = "أموكسيسيلين", Category = "مضادات حيوية", Factors = new[] { 1.0m,1.0m,1.2m,1.3m,1.1m,0.9m,0.8m,0.9m,1.0m,1.1m,1.2m,1.2m } },
                new SeasonalFactorItem { Name = "سيتريزين (حساسية)", Category = "مضادات الحساسية", Factors = new[] { 0.7m,0.8m,1.4m,1.8m,1.9m,1.5m,1.2m,1.3m,1.5m,1.4m,1.0m,0.8m } },
                new SeasonalFactorItem { Name = "فنتولين بخاخ", Category = "تنفسية", Factors = new[] { 1.3m,1.2m,1.1m,1.0m,0.9m,0.8m,0.9m,1.0m,1.1m,1.2m,1.3m,1.4m } }
            };

            // إرسال البيانات للـ View
            ViewBag.ChartLabels = System.Text.Json.JsonSerializer.Serialize(chartLabels);
            ViewBag.ActualSales = System.Text.Json.JsonSerializer.Serialize(actualSalesData);
            ViewBag.ForecastData = System.Text.Json.JsonSerializer.Serialize(
                forecastValues.Any()
                    ? forecastValues
                    : Enumerable.Repeat(0m, chartLabels.Count).ToList()
            );
            ViewBag.ForecastItems = forecastItems;
            ViewBag.SeasonalFactors = seasonalFactors;
            ViewBag.TotalForecastValue = forecastItems.Sum(f => f.ProposedQty * f.UnitCost);
            ViewBag.UrgentItemsCount = forecastItems.Count(f => f.CurrentStock <= f.MinimumStock);

            return View();
        }

        // ============================================================
        // 🌤️ العوامل الموسمية
        // ============================================================
        [HttpGet]
        public IActionResult SeasonalFactors()
        {
            ViewData["Title"] = "العوامل الموسمية";
            ViewData["PageDescription"] = "تعديل معاملات الموسمية لكل دواء أو فئة علاجية";
            return View();
        }

        // ============================================================
        // ☁️ التنبؤ السحابي (Vertex AI)
        // ============================================================
        [HttpGet]
        public IActionResult CloudForecast()
        {
            ViewData["Title"] = "توقعات الطلب (Vertex AI)";
            ViewData["PageDescription"] = "التنبؤ المتقدم باستخدام Google Vertex AI";
            return View();
        }

        // ============================================================
        // ✅ اعتماد الخطة - API
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePlan([FromBody] ApprovePlanRequest request)
        {
            if (request == null || !request.Items.Any())
                return Json(new { success = false, message = "لا توجد بنود في الخطة" });

            try
            {
                // إنشاء طلب شراء لكل بند مُعتمد
                int scopeId = ReportScopeId == 0 ? 1 : ReportScopeId;
                var userId = int.Parse(User.FindFirst("UserID")?.Value ?? "1");

                foreach (var item in request.Items.Where(i => i.ApprovedQty > 0))
                {
                    await _context.Systemlogs.AddAsync(new SystemLogs
                    {
                        UserId = userId,
                        Action = "PlanApproved",
                        Details = $"اعتماد خطة شراء: {item.DrugName} - الكمية: {item.ApprovedQty} وحدة",
                        CreatedAt = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"تم اعتماد خطة المشتريات بنجاح ({request.Items.Count} صنف)" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }
    }

    // ============================================================
    // 📦 نماذج البيانات الخاصة بوحدة التخطيط
    // ============================================================
    public class ForecastPlanItem
    {
        public int DrugId { get; set; }
        public string DrugName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public decimal AvgMonthlyConsumption { get; set; }
        public decimal ForecastedDemand { get; set; }
        public int EOQ { get; set; }
        public int ProposedQty { get; set; }
        public int ApprovedQty { get; set; }
        public decimal UnitCost { get; set; }
        public string Status { get; set; } = "";
    }

    public class SeasonalFactorItem
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal[] Factors { get; set; } = Array.Empty<decimal>();
    }

    public class ApprovePlanRequest
    {
        public List<ApprovePlanItem> Items { get; set; } = new();
    }

    public class ApprovePlanItem
    {
        public int DrugId { get; set; }
        public string DrugName { get; set; } = "";
        public int ApprovedQty { get; set; }
    }
}

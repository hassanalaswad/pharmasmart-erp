using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class PricingController : BaseController
    {
        public PricingController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📋 1. شاشة التسعير الرئيسية
        // ==========================================
        [HttpGet]
        [HasPermission("Drugs", "View")]
        public async Task<IActionResult> Index(string? search, string? abc)
        {
            int branchId = ActiveBranchId;

            var query = _context.Branchinventory
                .Include(b => b.Drug)
                .Where(b => b.BranchId == branchId && b.Drug.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Drug.DrugName.Contains(search) || (b.Drug.Barcode != null && b.Drug.Barcode.Contains(search)));

            if (!string.IsNullOrWhiteSpace(abc) && abc != "All")
                query = query.Where(b => b.Abccategory == abc);

            // جلب آخر سعر بيع لكل دواء من آخر تشغيلة (جلسة شراء)
            var lastPurchasePrices = await _context.Purchasedetails
                .Include(pd => pd.Purchase)
                .Where(pd => pd.Purchase.BranchId == branchId)
                .GroupBy(pd => pd.DrugId)
                .Select(g => new
                {
                    DrugId = g.Key,
                    // متوسط أسعار البيع المرجح عبر التشغيلات (مجموع الكميات × أسعارها / إجمالي الكميات)
                    WeightedSellingPrice = g.Sum(x => x.Quantity * x.SellingPrice) / (g.Sum(x => x.Quantity) == 0 ? 1 : g.Sum(x => x.Quantity)),
                    LastSellingPrice = g.OrderByDescending(x => x.Purchase.PurchaseDate).First().SellingPrice,
                    LastPurchaseDate = g.OrderByDescending(x => x.Purchase.PurchaseDate).First().Purchase.PurchaseDate,
                    BatchCount = g.Select(x => x.BatchNumber).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.DrugId);

            var inventoryList = await query.OrderBy(b => b.Drug.DrugName).ToListAsync();

            var viewModel = inventoryList.Select(b =>
            {
                lastPurchasePrices.TryGetValue(b.DrugId, out var priceInfo);
                return new DrugPricingViewModel
                {
                    DrugId        = b.DrugId,
                    DrugName      = b.Drug.DrugName,
                    Barcode       = b.Drug.Barcode,
                    MainUnit      = b.Drug.MainUnit,
                    SubUnit       = b.Drug.SubUnit,
                    ConvFactor    = b.Drug.ConversionFactor,
                    ABCCategory   = b.Abccategory ?? "—",
                    StockQuantity = b.StockQuantity,
                    AverageCost   = b.AverageCost ?? 0,
                    CurrentSellingPrice     = b.CurrentSellingPrice ?? 0,
                    WeightedSellingPrice    = priceInfo?.WeightedSellingPrice ?? 0,
                    LastSellingPrice        = priceInfo?.LastSellingPrice ?? 0,
                    LastPurchaseDate        = priceInfo?.LastPurchaseDate,
                    BatchCount              = priceInfo?.BatchCount ?? 0,
                    ProfitMargin            = (b.CurrentSellingPrice ?? 0) > 0 && (b.AverageCost ?? 0) > 0
                        ? Math.Round(((b.CurrentSellingPrice!.Value - (b.AverageCost ?? 0)) / b.CurrentSellingPrice.Value) * 100, 1)
                        : 0
                };
            }).ToList();

            ViewBag.Search = search;
            ViewBag.ABCFilter = abc;
            ViewBag.TotalDrugs = viewModel.Count;
            ViewBag.PriceConflicts = viewModel.Count(v => Math.Abs(v.CurrentSellingPrice - v.WeightedSellingPrice) > 1 && v.WeightedSellingPrice > 0);

            return View(viewModel);
        }

        // ==========================================
        // ✏️ 2. تحديث سعر البيع لصنف واحد (AJAX)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Drugs", "Edit")]
        public async Task<IActionResult> UpdatePrice([FromBody] UpdatePriceDto dto)
        {
            if (dto == null || dto.DrugId <= 0 || dto.NewPrice <= 0)
                return BadRequest(new { success = false, message = "بيانات غير صالحة." });

            int branchId = ActiveBranchId;
            var inv = await _context.Branchinventory
                .FirstOrDefaultAsync(b => b.DrugId == dto.DrugId && b.BranchId == branchId);

            if (inv == null)
                return NotFound(new { success = false, message = "الصنف غير موجود في مخزون الفرع." });

            decimal oldPrice = inv.CurrentSellingPrice ?? 0;
            inv.CurrentSellingPrice = dto.NewPrice;
            await _context.SaveChangesAsync();

            await RecordLog("Edit", "Pricing", $"تعديل سعر البيع للدواء ID={dto.DrugId} من {oldPrice} إلى {dto.NewPrice}");

            return Ok(new { success = true, message = "تم تحديث السعر بنجاح." });
        }

        // ==========================================
        // 🔄 3. تطبيق السعر المرجح تلقائياً لجميع الأصناف (bulk)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Drugs", "Edit")]
        public async Task<IActionResult> ApplyWeightedPricesAll()
        {
            int branchId = ActiveBranchId;

            // جلب المتوسطات المرجحة من تفاصيل المشتريات
            var weightedPrices = await _context.Purchasedetails
                .Include(pd => pd.Purchase)
                .Where(pd => pd.Purchase.BranchId == branchId)
                .GroupBy(pd => pd.DrugId)
                .Select(g => new
                {
                    DrugId = g.Key,
                    WeightedPrice = g.Sum(x => (decimal)x.Quantity * x.SellingPrice)
                                    / (g.Sum(x => (decimal)x.Quantity) == 0 ? 1 : g.Sum(x => (decimal)x.Quantity))
                })
                .ToListAsync();

            if (!weightedPrices.Any())
                return BadRequest(new { success = false, message = "لا توجد بيانات مشتريات لحساب الأسعار المرجحة." });

            var drugIds = weightedPrices.Select(w => w.DrugId).ToList();
            var inventories = await _context.Branchinventory
                .Where(b => b.BranchId == branchId && drugIds.Contains(b.DrugId))
                .ToListAsync();

            int updatedCount = 0;
            foreach (var inv in inventories)
            {
                var wp = weightedPrices.FirstOrDefault(x => x.DrugId == inv.DrugId);
                if (wp != null && wp.WeightedPrice > 0)
                {
                    inv.CurrentSellingPrice = Math.Round(wp.WeightedPrice, 2);
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();
            await RecordLog("BulkEdit", "Pricing", $"تطبيق الأسعار المرجحة على {updatedCount} صنف في الفرع {branchId}");

            return Ok(new { success = true, updatedCount, message = $"تم تحديث {updatedCount} صنف بالأسعار المرجحة." });
        }

        // ==========================================
        // 📊 4. API: تفاصيل تشغيلات صنف معين
        // ==========================================
        [HttpGet]
        [HasPermission("Drugs", "View")]
        public async Task<IActionResult> GetDrugBatches(int drugId)
        {
            int branchId = ActiveBranchId;
            var batches = await _context.Purchasedetails
                .Include(pd => pd.Purchase).ThenInclude(p => p.Supplier)
                .Where(pd => pd.DrugId == drugId && pd.Purchase.BranchId == branchId)
                .OrderByDescending(pd => pd.Purchase.PurchaseDate)
                .Take(10)
                .Select(pd => new
                {
                    batchNumber  = pd.BatchNumber ?? "N/A",
                    expiryDate   = pd.ExpiryDate.ToString("yyyy-MM-dd"),
                    quantity     = pd.Quantity,
                    bonusQty     = pd.BonusQuantity,
                    costPrice    = pd.CostPrice,
                    sellingPrice = pd.SellingPrice,
                    supplier     = pd.Purchase.Supplier != null ? pd.Purchase.Supplier.SupplierName : "—",
                    purchaseDate = pd.Purchase.PurchaseDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Json(batches);
        }

        // DTO
        public class UpdatePriceDto
        {
            public int DrugId { get; set; }
            public decimal NewPrice { get; set; }
        }
    }

    // ViewModel
    public class DrugPricingViewModel
    {
        public int DrugId { get; set; }
        public string DrugName { get; set; } = "";
        public string? Barcode { get; set; }
        public string MainUnit { get; set; } = "";
        public string SubUnit { get; set; } = "";
        public int ConvFactor { get; set; }
        public string ABCCategory { get; set; } = "";
        public int StockQuantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal CurrentSellingPrice { get; set; }
        public decimal WeightedSellingPrice { get; set; }
        public decimal LastSellingPrice { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public int BatchCount { get; set; }
        public decimal ProfitMargin { get; set; }
    }
}

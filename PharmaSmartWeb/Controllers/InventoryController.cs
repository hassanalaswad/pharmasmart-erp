//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using PharmaSmartWeb.Filters;
//using PharmaSmartWeb.Models;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PharmaSmartWeb.Controllers
//{
//    [Authorize]
//    // 🚀 الوراثة من BaseController للوصول لمحرك الفروع (ActiveBranchId)
//    public class InventoryController : BaseController
//    {
//        public InventoryController(ApplicationDbContext context) : base(context) { }

//        // ==========================================
//        // 📦 1. عرض المخزون والجرد (معزول بالفرع النشط)
//        // ==========================================
//        [HttpGet]
//        [HasPermission("Inventory", "View")]
//        public async Task<IActionResult> Index()
//        {
//            // 🚀 العزل التام: يطبق على الجميع (مدير أو موظف) بناءً على الفرع النشط
//            // لا نستخدم if (!IsSuperAdmin) هنا لمنع اختلاط المخزون على المدير
//            var query = _context.Branchinventory
//                .Include(bi => bi.Drug)
//                .Include(bi => bi.Branch)
//                .Where(bi => bi.BranchId == ActiveBranchId) // <-- سطر العزل السحري
//                .AsQueryable();

//            // جلب اسم الفرع النشط لعرضه في الواجهة بشكل ديناميكي
//            var activeBranch = await _context.Branches.FindAsync(ActiveBranchId);
//            ViewBag.BranchName = activeBranch?.BranchName ?? "الفرع غير محدد";

//            var stock = await query.ToListAsync();
//            return View(stock);
//        }

//        // ==========================================
//        // ⚠️ 2. تقرير النواقص (معزول بالفرع النشط)
//        // ==========================================
//        [HttpGet]
//        [HasPermission("Inventory", "View")] // 🛡️ حارس الصلاحيات
//        public async Task<IActionResult> Shortages()
//        {
//            // 🚀 العزل التام: تحديد النواقص في سياق الفرع النشط فقط
//            var query = _context.Branchinventory
//                .Include(bi => bi.Drug)
//                .Include(bi => bi.Branch)
//                .Where(bi => bi.StockQuantity <= bi.MinimumStockLevel && bi.BranchId == ActiveBranchId) // <-- سطر العزل
//                .AsQueryable();

//            // جلب اسم الفرع النشط لعرضه في التقرير
//            var activeBranch = await _context.Branches.FindAsync(ActiveBranchId);
//            ViewBag.BranchName = activeBranch?.BranchName ?? "الفرع غير محدد";

//            var shortages = await query.ToListAsync();
//            return View(shortages);
//        }
//    }
//}

///* =============================================================================================
//📑 الكتالوج والدليل الفني للكنترولر (InventoryController)
//=============================================================================================
//الوظيفة العامة: 
//هذا الكنترولر مسؤول عن عرض "المخزون الفعلي" (Physical Stock) المتوفر في الصيدلية،
//ومراقبة الأدوية التي وصلت إلى "حد الطلب" (Reorder Limit) لتنبيه الإدارة بضرورة عمل طلبية شراء.

//ملاحظة معمارية بخصوص العزل (Branch Isolation):
//- تم تطبيق العزل التشغيلي (Operational Isolation) بشكل صارم وإجباري باستخدام `ActiveBranchId`.
//- الموظف العادي: يرى بضاعة ونواقص فرعه فقط.
//- المدير العام (SuperAdmin): يخضع للعزل هنا أيضاً. لا يرى مخزون كل الفروع مختلطاً (لكي لا يبيع
//  أو يطلب بضاعة بناءً على جرد فرع آخر بالخطأ)، بل يرى مخزون ونواقص "الفرع النشط" الذي اختاره 
//  من القائمة العلوية. إذا أراد جرد فرع آخر، يقوم ببساطة بتبديل الفرع من الهيدر.

//محتويات الكنترولر والدوال (Methods):

//1. [HttpGet] Index()
//   - الوظيفة: جرد شامل لكل الأصناف المتوفرة داخل مخزن الفرع النشط.
//   - العزل: `Where(bi => bi.BranchId == ActiveBranchId)`

//2. [HttpGet] Shortages()
//   - الوظيفة: فلترة المخزون لعرض الأصناف التي كميتها أقل من أو تساوي `MinimumStockLevel`.
//   - العزل: `Where(bi => bi.BranchId == ActiveBranchId)`
//=============================================================================================
//*/


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class InventoryController : BaseController
    {
        public InventoryController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📦 1. عرض المخزون والجرد (يدعم التجميع)
        // ==========================================
        [HttpGet]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> Index()
        {
            int currentBranchId = ActiveBranchId;
            ViewBag.BranchName = currentBranchId == 0 ? "كل الفروع (تجميع مركزي)" : (await _context.Branches.FindAsync(currentBranchId))?.BranchName ?? "غير محدد";

            List<Branchinventory> stock = new List<Branchinventory>();

            if (currentBranchId == 0 && IsSuperAdmin)
            {
                // 🚀 تجميع الأرصدة لكل الفروع
                stock = await _context.Branchinventory
                    .Include(bi => bi.Drug)
                    .GroupBy(bi => bi.DrugId)
                    .Select(g => new Branchinventory
                    {
                        DrugId = g.Key,
                        Drug = g.First().Drug,
                        StockQuantity = g.Sum(x => x.StockQuantity),
                        MinimumStockLevel = g.Max(x => x.MinimumStockLevel), // أخذ أعلى حد طلب أمان
                        AverageCost = g.Average(x => x.AverageCost)
                    }).ToListAsync();
            }
            else
            {
                // 🚀 عرض فرع محدد
                stock = await _context.Branchinventory
                    .Include(bi => bi.Drug)
                    .Include(bi => bi.Branch)
                    .Where(bi => bi.BranchId == currentBranchId)
                    .ToListAsync();
            }

            return View(stock);
        }

        // ==========================================
        // ⚠️ 2. تقرير النواقص (يدعم التجميع)
        // ==========================================
        [HttpGet]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> Shortages()
        {
            int currentBranchId = ActiveBranchId;
            ViewBag.BranchName = currentBranchId == 0 ? "كل الفروع (تجميع مركزي)" : (await _context.Branches.FindAsync(currentBranchId))?.BranchName ?? "غير محدد";

            List<Branchinventory> shortages = new List<Branchinventory>();

            if (currentBranchId == 0 && IsSuperAdmin)
            {
                // 🚀 تجميع الأرصدة وفلترة النواقص على مستوى الشركة
                var aggregatedStock = await _context.Branchinventory
                    .Include(bi => bi.Drug)
                    .GroupBy(bi => bi.DrugId)
                    .Select(g => new Branchinventory
                    {
                        DrugId = g.Key,
                        Drug = g.First().Drug,
                        StockQuantity = g.Sum(x => x.StockQuantity),
                        MinimumStockLevel = g.Sum(x => x.MinimumStockLevel) // جمع حدود الطلب كأنها مخزن واحد كبير
                    }).ToListAsync();

                shortages = aggregatedStock.Where(s => s.StockQuantity <= s.MinimumStockLevel).ToList();
            }
            else
            {
                shortages = await _context.Branchinventory
                    .Include(bi => bi.Drug)
                    .Include(bi => bi.Branch)
                    .Where(bi => bi.StockQuantity <= bi.MinimumStockLevel && bi.BranchId == currentBranchId)
                    .ToListAsync();
            }

            return View(shortages);
        }
    }
}
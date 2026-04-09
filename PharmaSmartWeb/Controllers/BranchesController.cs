using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;

// 🚀 استدعاء مجلد الفلاتر الذي يحتوي على الحارس البرمجي
using PharmaSmartWeb.Filters;

namespace PharmaSmartWeb.Controllers
{
    [Authorize] // ضمان أن المستخدم مسجل دخول أولاً
    // 🚀 الوراثة من BaseController للوصول لمحرك العزل (ActiveBranchId) ودالة التسجيل
    public class BranchesController : BaseController
    {
        public BranchesController(ApplicationDbContext context) : base(context)
        {
        }

        // ==========================================
        // 🏢 1. عرض قائمة الفروع (معزولة للموظفين، شاملة للمدير)
        // ==========================================
        [HttpGet]
        [HasPermission("Branches", "View")]
        public async Task<IActionResult> Index()
        {
            var query = _context.Branches.AsQueryable();

            // 🚀 العزل الذكي: الموظف العادي يرى بيانات فرعه فقط، المدير يرى كافة الفروع لإدارتها
            if (!IsSuperAdmin)
            {
                query = query.Where(b => b.BranchId == ActiveBranchId);
            }

            var branches = await query.ToListAsync();
            return View(branches);
        }

        // ==========================================
        // ➕ 2. شاشة إضافة فرع جديد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Branches", "Add")]
        public IActionResult Create()
        {
            // 🚀 حماية سيادية: يُمنع أي شخص غير المدير العام من إضافة فروع جديدة
            if (!IsSuperAdmin) return RedirectToAction("AccessDenied", "Home");

            return View();
        }

        // ==========================================
        // 💾 3. حفظ بيانات فرع جديد (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Branches", "Add")]
        public async Task<IActionResult> Create(Branches branch)
        {
            // 🚀 حماية سيادية للـ POST
            if (!IsSuperAdmin) return RedirectToAction("AccessDenied", "Home");

            // استثناء القوائم المرتبطة (Relations) من الفحص الإجباري لكي لا يفشل الحفظ
            ModelState.Remove("Branchinventory");
            ModelState.Remove("DrugtransfersFromBranch");
            ModelState.Remove("DrugtransfersToBranch");
            ModelState.Remove("Employees");
            ModelState.Remove("Forecasts");
            ModelState.Remove("Journalentries");
            ModelState.Remove("Purchases");
            ModelState.Remove("Sales");
            ModelState.Remove("Seasonaldata");
            ModelState.Remove("Stockmovements");
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();

                // 🚀 تسجيل العملية في السجلات (Logs)
                await RecordLog("Add", "Branches", $"تم افتتاح فرع جديد بنجاح: {branch.BranchName} (كود: {branch.BranchCode}) في منطقة {branch.Location}");

                TempData["Success"] = "تم إضافة الفرع الجديد بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Error = "يرجى التأكد من تعبئة كود واسم الفرع بشكل صحيح.";
            return View(branch);
        }

        // ==========================================
        // ✏️ 4. شاشة تعديل بيانات الفرع (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Branches", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // البحث عن الفرع المطلوب
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            // 🚀 حماية العزل: يمنع موظف فرع من تعديل بيانات فرع آخر
            if (!IsSuperAdmin && branch.BranchId != ActiveBranchId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View(branch);
        }

        // ==========================================
        // 💾 5. حفظ تعديلات الفرع (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Branches", "Edit")]
        public async Task<IActionResult> Edit(int id, Branches branch)
        {
            if (id != branch.BranchId) return NotFound();

            // 🚀 حماية العزل للـ POST لمنع اختراق الـ API
            if (!IsSuperAdmin && branch.BranchId != ActiveBranchId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // استثناء القوائم المرتبطة (Relations) لكي لا يفشل التحقق
            ModelState.Remove("Branchinventory");
            ModelState.Remove("DrugtransfersFromBranch");
            ModelState.Remove("DrugtransfersToBranch");
            ModelState.Remove("Employees");
            ModelState.Remove("Forecasts");
            ModelState.Remove("Journalentries");
            ModelState.Remove("Purchases");
            ModelState.Remove("Sales");
            ModelState.Remove("Seasonaldata");
            ModelState.Remove("Stockmovements");
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(branch);
                    await _context.SaveChangesAsync();

                    // 🚀 تسجيل عملية التعديل في السجلات (Logs)
                    await RecordLog("Edit", "Branches", $"تعديل بيانات فرع: {branch.BranchName} - العنوان المحدث: {branch.Location}");

                    TempData["Success"] = "تم تحديث بيانات الفرع بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Branches.Any(e => e.BranchId == branch.BranchId))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewBag.Error = "توجد مشكلة في البيانات المدخلة، يرجى مراجعتها.";
            return View(branch);
        }
    }
}

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (BranchesController)
=============================================================================================
الوظيفة العامة: 
هذا الكنترولر مسؤول عن إدارة "البيانات الأساسية السيادية" الخاصة بفروع الصيدليات 
(Master Data) مثل إضافة فروع جديدة، أو تعديل بيانات فروع قائمة (العنوان، الكود، الحالة).

ملاحظة معمارية بخصوص العزل (Branch Isolation):
الفروع بحد ذاتها هي كيان سيادي. ومع ذلك، تم تطبيق عزل هجين (Hybrid Isolation):
- المدير العام (SuperAdmin): لا يخضع للعزل هنا. يرى كافة الفروع ويستطيع إدارتها 
  وإضافة فروع جديدة (يتم تجاهل ActiveBranchId في دالة Index لصالحه).
- المستخدم العادي: إذا تم منحه صلاحية "عرض" أو "تعديل" الفروع (مثلاً لمدير فرع لكي يحدّث 
  رقم هاتف فرعه)، فإنه يرى ويعدّل **فرعه الحالي فقط** (ActiveBranchId). 
  ويُمنع برمجياً من فتح شاشة (Create) لإضافة فرع جديد حتى لو امتلك الصلاحية، لضمان المركزية.

محتويات الكنترولر والدوال (Methods):

1. [HttpGet] Index()
   - الوظيفة: عرض قائمة الفروع.
   - العزل: يعرض كل شيء للمدير، ويعرض الفرع النشط (فرع الموظف) لغير المدير.

2. [HttpGet/HttpPost] Create()
   - الوظيفة: إضافة كيان فرع جديد لقاعدة البيانات.
   - العزل الأمني: محصورة كلياً وإجبارياً على `IsSuperAdmin`.

3. [HttpGet/HttpPost] Edit()
   - الوظيفة: تعديل تفاصيل الفرع المحددة سلفاً.
   - العزل الأمني: تمنع أي مستخدم غير الإدارة العليا من تعديل بيانات فرع يختلف عن `ActiveBranchId`.
=============================================================================================
*/

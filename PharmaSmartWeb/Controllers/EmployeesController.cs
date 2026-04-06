using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System.Linq;
using System.Threading.Tasks;
using PharmaSmartWeb.Filters;

namespace PharmaSmartWeb.Controllers
{
    [Authorize] // ضمان أن المستخدم مسجل دخول أولاً
    public class EmployeesController : BaseController
    {
        public EmployeesController(ApplicationDbContext context) : base(context)
        {
        }

        // ==========================================
        // 👥 1. عرض قائمة الموظفين (مفلترة بالفرع النشط)
        // ==========================================
        [HttpGet]
        [HasPermission("Employees", "View")]
        public async Task<IActionResult> Index()
        {
            // 🚀 العزل الذكي: استخدام ActiveBranchId يلغي الحاجة لـ if (!IsSuperAdmin)
            // المدير سيرى موظفي الفرع الذي اختاره من القائمة العلوية، والموظف سيرى زملاءه فقط.
            var employees = await _context.Employees
                .Include(e => e.Branch)
                .Where(e => e.BranchId == ActiveBranchId)
                .ToListAsync();

            return View(employees);
        }

        // ==========================================
        // ➕ 2. شاشة إضافة موظف جديد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Employees", "Add")]
        public IActionResult Create()
        {
            // 💡 ملاحظة: نعرض قائمة الفروع للمدير فقط لكي يتمكن من اختيار فرع آخر غير النشط إذا أراد
            if (IsSuperAdmin)
            {
                ViewBag.BranchList = new SelectList(_context.Branches.Where(b => b.IsActive == true), "BranchId", "BranchName", ActiveBranchId);
            }

            return View();
        }

        // ==========================================
        // 💾 3. حفظ بيانات موظف جديد (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Employees", "Add")]
        public async Task<IActionResult> Create(Employees employee)
        {
            ModelState.Remove("Branch");
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                // 🚀 الختم الآلي والإنقاذ من الأخطاء:
                // إذا كان المستخدم موظفاً (يُمنع من اختيار الفرع)، أو إذا كان مديراً ولكنه لم يختر فرعاً صحيحاً
                if (!IsSuperAdmin || employee.BranchId <= 0)
                {
                    employee.BranchId = ActiveBranchId;
                }

                employee.IsActive = true;
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                await RecordLog("Add", "Employees", $"تم إضافة الموظف {employee.FullName} لفرع {employee.BranchId}");

                TempData["Success"] = "تم تسجيل بيانات الموظف بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            var exactErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            ViewBag.Error = "مشكلة في الحفظ: " + string.Join(" | ", exactErrors);

            if (IsSuperAdmin)
            {
                ViewBag.BranchList = new SelectList(_context.Branches.Where(b => b.IsActive == true), "BranchId", "BranchName", employee.BranchId);
            }

            return View(employee);
        }

        // ==========================================
        // ✏️ 4. شاشة تعديل الموظف (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Employees", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // 🚀 حماية العزل: منع أي مستخدم من تعديل موظف يعمل في فرع آخر
            if (!IsSuperAdmin && employee.BranchId != ActiveBranchId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (IsSuperAdmin)
            {
                ViewBag.BranchList = new SelectList(_context.Branches.Where(b => b.IsActive == true), "BranchId", "BranchName", employee.BranchId);
            }

            return View(employee);
        }

        // ==========================================
        // 💾 5. حفظ تعديلات الموظف (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Employees", "Edit")]
        public async Task<IActionResult> Edit(int id, Employees employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            ModelState.Remove("Branch");
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                // 🚀 حماية أمنية عميقة لمنع التلاعب عبر (Inspect Element)
                var existingEmployee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (!IsSuperAdmin && existingEmployee?.BranchId != ActiveBranchId)
                {
                    return RedirectToAction("AccessDenied", "Home");
                }

                // 🚀 منع نقل الموظف لفرع آخر لغير المدير العام
                if (!IsSuperAdmin)
                {
                    employee.BranchId = ActiveBranchId;
                }
                else if (employee.BranchId <= 0) // إنقاذ إذا نسي المدير اختيار الفرع
                {
                    employee.BranchId = existingEmployee.BranchId;
                }

                _context.Update(employee);
                await _context.SaveChangesAsync();

                await RecordLog("Edit", "Employees", $"تم تحديث بيانات الموظف: {employee.FullName} في فرع رقم {employee.BranchId}");

                TempData["Success"] = "تم تحديث بيانات الموظف بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            var exactErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            ViewBag.Error = "توجد مشكلة في التحديث: " + string.Join(" | ", exactErrors);

            if (IsSuperAdmin)
            {
                ViewBag.BranchList = new SelectList(_context.Branches.Where(b => b.IsActive == true), "BranchId", "BranchName", employee.BranchId);
            }

            return View(employee);
        }
    }
}
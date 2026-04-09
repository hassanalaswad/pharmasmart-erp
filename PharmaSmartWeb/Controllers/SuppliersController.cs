using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class SuppliersController : BaseController
    {
        public SuppliersController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📄 1. عرض قائمة الموردين (بيانات مركزية للجميع)
        // ==========================================
        [HttpGet]
        [HasPermission("Suppliers", "View")]
        public async Task<IActionResult> Index()
        {
            // 🚀 تصحيح معماري: الموردون بيانات مركزية (Master Data). 
            // تم إزالة العزل لكي ترى جميع الفروع نفس قائمة الموردين لتجنب التكرار.
            var suppliers = await _context.Suppliers
                .Include(s => s.Account)
                .OrderByDescending(s => s.SupplierId)
                .ToListAsync();

            return View(suppliers);
        }

        // ==========================================
        // 📊 API: جلب قائمة الموردين (للتحديث الفوري)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetSuppliersList()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SupplierName)
                .Select(s => new { supplierId = s.SupplierId, supplierName = s.SupplierName })
                .ToListAsync();
            return Json(suppliers);
        }

        // ==========================================
        // ➕ 2. شاشة إضافة مورد جديد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Suppliers", "Add")]
        public IActionResult Create(bool isPopup = false)
        {
            ViewBag.IsPopup = isPopup;
            ViewBag.ParentAccounts = new SelectList(_context.Accounts
                .Where(a => a.IsActive == true && (a.AccountType == "Liabilities" || a.AccountName.Contains("مورد")))
                .OrderBy(a => a.AccountName), "AccountId", "AccountName");

            return View();
        }

        // ==========================================
        // 💾 3. حفظ المورد وإنشاء حسابه المالي (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Suppliers", "Add")]
        public async Task<IActionResult> Create(Suppliers supplier, int ParentAccountId, bool isPopup = false)
        {
            ModelState.Remove("Account");
            ModelState.Remove("Branch");

            if (ParentAccountId == 0)
            {
                ModelState.AddModelError("ParentAccountId", "يجب اختيار الحساب الأب للموردين من الدليل المحاسبي.");
            }

            if (ModelState.IsValid)
            {
                // التحقق من عدم تكرار اسم المورد برمجياً
                bool exists = await _context.Suppliers.AnyAsync(s => s.SupplierName == supplier.SupplierName);
                if (exists)
                {
                    ViewBag.Error = "عذراً، هذا المورد مسجل مسبقاً في النظام المركزي.";
                    ViewBag.IsPopup = isPopup;
                    ViewBag.ParentAccounts = new SelectList(_context.Accounts.Where(a => a.IsActive == true && (a.AccountType == "Liabilities" || a.AccountName.Contains("مورد"))).OrderBy(a => a.AccountName), "AccountId", "AccountName", ParentAccountId);
                    return View(supplier);
                }

                var strategy = _context.Database.CreateExecutionStrategy();

                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                // الخطوة 1: إنشاء الحساب المالي الجديد للمورد (حساب موحد)
                                var parentAccount = await _context.Accounts.FindAsync(ParentAccountId);
                                var lastChild = await _context.Accounts
                                    .Where(a => a.ParentAccountId == ParentAccountId)
                                    .OrderByDescending(a => a.AccountCode)
                                    .FirstOrDefaultAsync();

                                string newAccountCode = lastChild != null
                                    ? (long.Parse(lastChild.AccountCode) + 1).ToString()
                                    : parentAccount.AccountCode + "001";

                                var newAccount = new Accounts
                                {
                                    AccountCode = newAccountCode,
                                    AccountName = supplier.SupplierName,
                                    ParentAccountId = ParentAccountId,
                                    AccountType = parentAccount.AccountType,
                                    Balance = 0,
                                    IsActive = true
                                };

                                _context.Accounts.Add(newAccount);
                                await _context.SaveChangesAsync();

                                // الخطوة 2: ربط المورد بالحساب
                                supplier.AccountId = newAccount.AccountId;
                                supplier.IsActive = true;
                                supplier.CreatedAt = DateTime.Now;

                                // نحتفظ بفرع الإنشاء لأغراض الرقابة فقط، ولكنه لا يمنع الفروع الأخرى من رؤيته
                                supplier.BranchId = ActiveBranchId;

                                _context.Suppliers.Add(supplier);
                                await _context.SaveChangesAsync();

                                await transaction.CommitAsync();
                            }
                            catch (Exception)
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    });

                    await RecordLog("Add", "Suppliers", $"تم إضافة المورد المركزي: {supplier.SupplierName} وإنشاء حسابه المالي");
                    TempData["Success"] = "تم تسجيل المورد وإنشاء حسابه المالي بنجاح.";

                    // إذا كنا في وضع Popup، نوجه لصفحة خاصة ترسل postMessage
                    if (isPopup)
                    {
                        return Content(@"<script>window.parent.postMessage('supplier_added_success','*');</script>", "text/html");
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "حدث خطأ أثناء الإنشاء المزدوج: " + ex.Message;
                }
            }

            ViewBag.IsPopup = isPopup;
            ViewBag.ParentAccounts = new SelectList(_context.Accounts
                .Where(a => a.IsActive == true && (a.AccountType == "Liabilities" || a.AccountName.Contains("مورد")))
                .OrderBy(a => a.AccountName), "AccountId", "AccountName", ParentAccountId);

            return View(supplier);
        }

        // ==========================================
        // ✏️ 4. شاشة تعديل مورد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Suppliers", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers
                .Include(s => s.Account)
                .FirstOrDefaultAsync(m => m.SupplierId == id);

            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // ==========================================
        // 💾 5. حفظ التعديلات وتحديث الدليل المالي (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Suppliers", "Edit")]
        public async Task<IActionResult> Edit(int id, Suppliers supplier)
        {
            if (id != supplier.SupplierId) return NotFound();

            ModelState.Remove("Account");
            ModelState.Remove("Branch");

            if (ModelState.IsValid)
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                var existingSupplier = await _context.Suppliers
                                    .Include(s => s.Account)
                                    .FirstOrDefaultAsync(s => s.SupplierId == id);

                                if (existingSupplier == null) throw new Exception("المورد غير موجود.");

                                existingSupplier.SupplierName = supplier.SupplierName;
                                existingSupplier.ContactPerson = supplier.ContactPerson;
                                existingSupplier.Phone = supplier.Phone;
                                existingSupplier.Address = supplier.Address;
                                existingSupplier.IsActive = supplier.IsActive;

                                if (existingSupplier.Account != null)
                                {
                                    existingSupplier.Account.AccountName = supplier.SupplierName;
                                    existingSupplier.Account.IsActive = supplier.IsActive??false;
                                }

                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();
                            }
                            catch (Exception)
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    });

                    await RecordLog("Edit", "Suppliers", $"تم تعديل بيانات المورد: {supplier.SupplierName} وتحديث حسابه المالي");
                    TempData["Success"] = "تم تحديث بيانات المورد والدليل المالي بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "حدث خطأ أثناء التحديث: " + ex.Message;
                }
            }
            return View(supplier);
        }

        // ==========================================
        // 🗑️ 6. حذف المورد (إيقاف النشاط)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Suppliers", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                supplier.IsActive = false; // Soft Delete
                await _context.SaveChangesAsync();
                await RecordLog("Delete", "Suppliers", $"تم إيقاف نشاط المورد المركزي: {supplier.SupplierName}");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (SuppliersController)
=============================================================================================
الوظيفة العامة: 
إدارة "الموردين والشركات" في النظام المركزي. 

ملاحظة معمارية هامة (Global Master Data):
- تم إلغاء عزل الفروع (`ActiveBranchId`) من شاشات الموردين بقرار هندسي مقصود.
- السبب: المورد (مثل شركة الأدوية) هو كيان موحد للشركة بأكملها. السماح لكل فرع بإنشاء مورد 
  خاص به يؤدي إلى تكرار فوضوي في الدليل المحاسبي وبيانات قاعدة البيانات.
- العزل الحقيقي: يكمن في "العمليات" (المشتريات، القيود اليومية، كشوفات الحساب) حيث يتم 
  توجيه الذمم الدائنة لكل فرع على حدة باستخدام `Purchases.BranchId` و `JournalEntries.BranchId`.
=============================================================================================
*/

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (SuppliersController)
=============================================================================================
الوظيفة العامة: 
هذا الكنترولر مسؤول عن إدارة "الموردين والشركات" في النظام. يشمل ذلك تسجيل بيانات 
الاتصال (المندوبين)، وربطهم آلياً بالدليل المحاسبي المركزي لضمان تسجيل الذمم الدائنة (مديونية 
الفروع للشركات الموردة) بشكل تلقائي عند توريد الأدوية.

ملاحظة معمارية بخصوص العزل (Branch Isolation):
تم تطبيق العزل التشغيلي الكامل باستخدام محرك `ActiveBranchId`:
- العرض (Index): يتم عرض موردي "الفرع النشط" فقط. المورد المسجل لفرع "صنعاء" لا يظهر 
  لموظف المشتريات في فرع "عدن". والمدير العام يرى موردي الفرع الذي اختاره من لوحة التحكم.
- الإضافة (Create): يتم ختم المورد الجديد وحسابه المالي آلياً برقم `ActiveBranchId`. 
  تم إزالة حقل "الفرع" من واجهة الإدخال لمنع التوجيه الخاطئ للذمم.
- التعديل والحذف (Edit/Delete): محمية برمجياً لمنع التلاعب عبر الروابط (API). لا يمكن لأي 
  مستخدم (حتى المدير) تعديل بيانات أو إيقاف نشاط مورد يتبع لفرع يختلف عن سياق الفرع 
  النشط الذي يقف عليه حالياً.

التوجيه المحاسبي (Financial Integration):
عند إنشاء مورد جديد، يقوم النظام تلقائياً بإنشاء "حساب فرعي" (Sub-Account) جديد له تحت 
الحساب الأب المحدد (مثل: الخصوم > الموردين)، ويقوم بتوليد رمز الحساب (AccountCode) بشكل 
متسلسل وربطه بالمورد في قاعدة البيانات باستخدام الـ (Transactions) لضمان عدم حدوث خطأ 
في الإنشاء المزدوج.
=============================================================================================
*/

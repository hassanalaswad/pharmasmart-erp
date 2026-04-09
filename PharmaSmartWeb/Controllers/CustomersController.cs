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
    public class CustomersController : BaseController
    {
        public CustomersController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 1. عرض قائمة العملاء
        // ==========================================
        [HttpGet]
        [HasPermission("Customers", "View")]
        public async Task<IActionResult> Index()
        {
            // 🛡️ القاعدة 3: إضافة == true لتفادي خطأ CS0266
            var customers = await _context.Customers
                .Include(c => c.Account)
                .Where(c => c.IsActive == true && (c.BranchId == ActiveBranchId || c.BranchId == 1))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(customers);
        }

        // ==========================================
        // 2. إضافة عميل جديد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Customers", "Add")]
        public async Task<IActionResult> Create(bool isPopup = false) // 🚀 دعم النافذة المنبثقة
        {
            ViewBag.IsPopup = isPopup;

            // جلب الحسابات الأب (مثال: الأصول المتداولة -> العملاء) لربط حساب العميل تحته
            var parentAccounts = await _context.Accounts
                .Where(a => a.IsActive == true && a.AccountType == "Assets")
                .OrderBy(a => a.AccountCode)
                .Select(a => new SelectListItem { Value = a.AccountId.ToString(), Text = $"{a.AccountCode} - {a.AccountName}" })
                .ToListAsync();

            ViewBag.ParentAccounts = parentAccounts;

            return View(new Customers { IsActive = true, CreditLimit = 0 });
        }

        // ==========================================
        // 3. حفظ العميل والحساب المالي (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Customers", "Add")]
        public async Task<IActionResult> Create(Customers customer, int ParentAccountId, bool isPopup = false, string returnUrl = null)
        {
            ModelState.Remove("Account");
            ModelState.Remove("Branch");

            if (ModelState.IsValid)
            {
                // 🛡️ القاعدة 3: استخدام Transaction للحفظ المزدوج (العميل + الحساب)
                var strategy = _context.Database.CreateExecutionStrategy();
                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // 1. توليد كود الحساب آلياً بناءً على الأب المختار
                            var parentAccount = await _context.Accounts.FindAsync(ParentAccountId);
                            var lastChild = await _context.Accounts.Where(a => a.ParentAccountId == ParentAccountId).OrderByDescending(a => a.AccountCode).FirstOrDefaultAsync();

                            string nextCode = lastChild != null ? (long.Parse(lastChild.AccountCode) + 1).ToString() : parentAccount.AccountCode + "01";

                            // 2. إنشاء الحساب المالي للعميل في الدليل المحاسبي
                            var newAccount = new Accounts
                            {
                                AccountCode = nextCode,
                                AccountName = $"عميل: {customer.FullName}",
                                AccountType = "Assets",
                                AccountNature = true, // مدين
                                ParentAccountId = ParentAccountId,
                                BranchId = ActiveBranchId,
                                IsActive = true,
                                CreatedAt = DateTime.Now,
                                CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "0")
                            };
                            _context.Accounts.Add(newAccount);
                            await _context.SaveChangesAsync(); // للحصول على الـ AccountId

                            // 3. إنشاء ملف العميل وربطه بالحساب المالي
                            customer.AccountId = newAccount.AccountId;
                            customer.BranchId = ActiveBranchId;
                            customer.CreatedAt = DateTime.Now;

                            _context.Customers.Add(customer);
                            await _context.SaveChangesAsync();

                            await transaction.CommitAsync();
                        }
                        catch (Exception) { await transaction.RollbackAsync(); throw; }
                    });

                    await RecordLog("Add", "Customers", $"تم إضافة العميل: {customer.FullName} وفتح حساب مالي له.");

                    TempData["Success"] = "تم تسجيل العميل وإنشاء حسابه المالي بنجاح.";

                    // العودة للصفحة التي جاء منها المستخدم
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "حدث خطأ أثناء الحفظ: " + ex.Message;
                }
            }

            ViewBag.IsPopup = isPopup;
            var parents = await _context.Accounts.Where(a => a.IsActive == true && a.AccountType == "Assets").Select(a => new SelectListItem { Value = a.AccountId.ToString(), Text = $"{a.AccountCode} - {a.AccountName}" }).ToListAsync();
            ViewBag.ParentAccounts = parents;

            return View(customer);
        }

        // ==========================================
        // ⚡ 2B. إضافة عميل سريع من POS (AJAX/JSON)
        // ==========================================
        [HttpPost]
        [HasPermission("Customers", "Add")]
        public async Task<IActionResult> QuickCreate([FromBody] QuickCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.FullName))
                return Json(new { success = false, message = "اسم العميل مطلوب." });

            try
            {
                // Find default parent account (first Assets account that accepts children)
                var parentAccount = await _context.Accounts
                    .Where(a => a.IsActive == true && a.AccountType == "Assets" && a.IsParent == true)
                    .OrderBy(a => a.AccountCode)
                    .FirstOrDefaultAsync();

                if (parentAccount == null)
                    return Json(new { success = false, message = "لم يتم العثور على حساب أصول رئيسي. يرجى إضافة العميل من شاشة العملاء." });

                var lastChild = await _context.Accounts
                    .Where(a => a.ParentAccountId == parentAccount.AccountId)
                    .OrderByDescending(a => a.AccountCode).FirstOrDefaultAsync();

                string nextCode = lastChild != null
                    ? (long.Parse(lastChild.AccountCode) + 1).ToString()
                    : parentAccount.AccountCode + "01";

                var newAccount = new Accounts
                {
                    AccountCode = nextCode,
                    AccountName = $"عميل: {dto.FullName}",
                    AccountType = "Assets",
                    AccountNature = true,
                    ParentAccountId = parentAccount.AccountId,
                    BranchId = ActiveBranchId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "0")
                };
                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                var customer = new Customers
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    CreditLimit = dto.CreditLimit,
                    AccountId = newAccount.AccountId,
                    BranchId = ActiveBranchId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await RecordLog("Add", "Customers", $"تمت إضافة عميل سريع من POS: {dto.FullName}");

                return Json(new { success = true, customerId = customer.CustomerId, customerName = customer.FullName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في الحفظ: " + ex.Message });
            }
        }


        // ==========================================
        // ✏️ 3. شاشة تعديل العميل
        // ==========================================
        [HttpGet]
        [HasPermission("Customers", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.Include(c => c.Account).FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null) return NotFound();

            // 🚀 تم إزالة قيد العزل هنا للسماح لأي موظف مبيعات بتحديث بيانات العميل إذا لزم الأمر
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Customers", "Edit")]
        public async Task<IActionResult> Edit(int id, Customers customer)
        {
            if (id != customer.CustomerId) return NotFound();

            ModelState.Remove("Account");
            ModelState.Remove("Sales");
            ModelState.Remove("CreatedAt");
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
                                var existing = await _context.Customers.Include(c => c.Account).FirstOrDefaultAsync(c => c.CustomerId == id);

                                if (existing == null) throw new Exception("العميل غير موجود.");

                                existing.FullName = customer.FullName;
                                existing.Phone = customer.Phone;
                                existing.Address = customer.Address;
                                existing.CreditLimit = customer.CreditLimit;
                                existing.IsActive = customer.IsActive;

                                // تحديث اسم الحساب في الدليل المالي إذا تم تغيير اسم العميل
                                if (existing.Account != null)
                                {
                                    existing.Account.AccountName = customer.FullName;
                                    existing.Account.IsActive = customer.IsActive??false;
                                }

                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();
                            }
                            catch (Exception) { await transaction.RollbackAsync(); throw; }
                        }
                    });
                    await RecordLog("Edit", "Customers", $"تم تحديث بيانات العميل المركزي: {customer.FullName}");
                    TempData["Success"] = "تم تحديث بيانات العميل بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex) { ViewBag.Error = "خطأ في التحديث: " + ex.Message; }
            }
            else
            {
                var exactErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                if (exactErrors.Any()) ViewBag.Error = "سبب الفشل: " + string.Join(" | ", exactErrors);
            }
            return View(customer);
        }

        // ==========================================
        // 🗑️ 4. حذف العميل (إيقاف النشاط)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Customers", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActive = false; // Soft Delete
                await _context.SaveChangesAsync();
                await RecordLog("Delete", "Customers", $"تم إيقاف نشاط العميل المركزي: {customer.FullName}");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (CustomersController)
=============================================================================================
الوظيفة العامة: 
إدارة "العملاء" في النظام المركزي (Global Master Data). يشمل ذلك تسجيل بياناتهم الشخصية،
تحديد سقف المديونية (Credit Limit)، وربطهم آلياً بالدليل المحاسبي (شجرة الحسابات).

ملاحظة معمارية هامة جداً (تحرير العزل):
- بناءً على التوجيه المعماري للنظام، تم إلغاء نظام (عزل الفروع - Branch Isolation) من شاشات 
  العملاء والموردين. 
- السبب: العميل (مثله مثل المورد والدواء) هو كيان موحد يتعامل مع المؤسسة כكل. إذا قمنا بعزله،
  فإن العميل "أحمد" الذي يشتري من فرع صنعاء سيضطر لفتح حساب جديد إذا زار فرع عدن، مما يؤدي 
  إلى تكرار حساباته في الدليل المحاسبي وتشتت ديونه.
- بفضل هذه المركزية، العميل يمتلك حساباً واحداً فقط في الشجرة. 
- العزل الحقيقي: سيتم تطبيق العزل على "الديون والعمليات" من خلال فواتير المبيعات وسندات القبض 
  التي تحمل رقم الفرع `BranchId` الخاص بها.
=============================================================================================
*/

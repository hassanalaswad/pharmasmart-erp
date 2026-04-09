//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using PharmaSmartWeb.Models;
//using PharmaSmartWeb.Filters;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Security.Claims;

//namespace PharmaSmartWeb.Controllers
//{
//    [Authorize]
//    public class JournalEntriesController : BaseController
//    {
//        public JournalEntriesController(ApplicationDbContext context) : base(context) { }

//        // ==========================================
//        // 📄 1. عرض سجل القيود اليومية (مفلتر بالفرع النشط للجميع)
//        // ==========================================
//        [HttpGet]
//        [HasPermission("JournalEntries", "View")]
//        public async Task<IActionResult> Index()
//        {
//            var query = _context.Journalentries
//                .Include(j => j.Branch)
//                .Include(j => j.Journaldetails)
//                .Where(j => j.BranchId == ActiveBranchId)
//                .AsQueryable();

//            var entries = await query.OrderByDescending(j => j.JournalDate).ToListAsync();
//            return View(entries);
//        }

//        // ==========================================
//        // ➕ 2. شاشة إضافة قيد جديد (GET)
//        // ==========================================
//        [HttpGet]
//        [HasPermission("JournalEntries", "Add")]
//        public IActionResult Create()
//        {
//            ReloadViewData();
//            return View();
//        }

//        // ==========================================
//        // 💾 3. حفظ القيد المحدث (مع الترحيل والختم الآلي والحماية)
//        // ==========================================
//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        //[HasPermission("JournalEntries", "Add")]
//        //public async Task<IActionResult> Create(Journalentries entry)
//        //{
//        //    ModelState.Remove("Branch");
//        //    ModelState.Remove("CreatedByNavigation");
//        //    if (entry.Journaldetails != null)
//        //    {
//        //        var detailsList = entry.Journaldetails.ToList();
//        //        for (int i = 0; i < detailsList.Count; i++)
//        //        {
//        //            ModelState.Remove($"Journaldetails[{i}].Account");
//        //            ModelState.Remove($"Journaldetails[{i}].Journal");
//        //        }
//        //    }

//        //    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserID")?.Value;
//        //    if (!string.IsNullOrEmpty(userIdStr)) entry.CreatedBy = int.Parse(userIdStr);

//        //    entry.BranchId = ActiveBranchId;

//        //    decimal totalDebit = entry.Journaldetails?.Sum(d => d.Debit) ?? 0;
//        //    decimal totalCredit = entry.Journaldetails?.Sum(d => d.Credit) ?? 0;

//        //    // 1. التحقق من التوازن
//        //    if (Math.Abs(totalDebit - totalCredit) > 0.01m || totalDebit <= 0)
//        //    {
//        //        ViewBag.Error = "عذراً، يجب أن يكون مجموع الطرف المدين مساوياً للطرف الدائن (القيد غير متزن).";
//        //        ReloadViewData();
//        //        return View(entry);
//        //    }

//        //    // 🚀 2. الحاجز الأمني الخلفي (Backend Validation): منع استخدام الحسابات الرئيسية
//        //    if (entry.Journaldetails != null)
//        //    {
//        //        foreach (var detail in entry.Journaldetails)
//        //        {
//        //            bool isParentAccount = _context.Accounts.Any(a => a.ParentAccountId == detail.AccountId);
//        //            if (isParentAccount)
//        //            {
//        //                var badAccount = _context.Accounts.Find(detail.AccountId);
//        //                ViewBag.Error = $"عذراً، الحساب ({badAccount?.AccountName}) هو حساب رئيسي. لا يمكن تسجيل قيود مالية عليه مباشرة، يرجى اختيار حساب فرعي.";
//        //                ReloadViewData();
//        //                return View(entry);
//        //            }
//        //        }
//        //    }

//        //    if (ModelState.IsValid)
//        //    {
//        //        var strategy = _context.Database.CreateExecutionStrategy();
//        //        try
//        //        {
//        //            await strategy.ExecuteAsync(async () =>
//        //            {
//        //                using (var transaction = await _context.Database.BeginTransactionAsync())
//        //                {
//        //                    try
//        //                    {
//        //                        entry.ReferenceType = "Manual";
//        //                        entry.IsPosted = true;

//        //                        _context.Journalentries.Add(entry);
//        //                        await _context.SaveChangesAsync();

//        //                        foreach (var item in entry.Journaldetails)
//        //                        {
//        //                            var account = await _context.Accounts.FindAsync(item.AccountId);
//        //                            if (account != null)
//        //                            {
//        //                                account.Balance += (item.Debit - item.Credit);
//        //                            }
//        //                        }

//        //                        await _context.SaveChangesAsync();
//        //                        await transaction.CommitAsync();
//        //                    }
//        //                    catch (Exception)
//        //                    {
//        //                        await transaction.RollbackAsync();
//        //                        throw;
//        //                    }
//        //                }
//        //            });

//        //            await RecordLog("Add", "JournalEntries", $"تم تسجيل قيد محاسبي مُرحل رقم {entry.JournalId} بقيمة {totalDebit} لفرع {entry.BranchId}");
//        //            TempData["Success"] = "تم ترحيل القيد وتحديث أرصدة الحسابات بنجاح.";
//        //            return RedirectToAction(nameof(Index));
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            ViewBag.Error = "فشل الحفظ في قاعدة البيانات: " + ex.Message;
//        //        }
//        //    }

//        //    ReloadViewData();
//        //    return View(entry);
//        //}
//        // ==========================================
//        // 💾 3. حفظ القيد المحدث (بدون تحديث حقل Balance)
//        // ==========================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [HasPermission("JournalEntries", "Add")]
//        public async Task<IActionResult> Create(Journalentries entry)
//        {
//            // 🛡️ تنظيف الـ ModelState من المتعلقات الملاحية لمنع فشل التحقق
//            ModelState.Remove("Branch");
//            ModelState.Remove("CreatedByNavigation");
//            if (entry.Journaldetails != null)
//            {
//                var detailsList = entry.Journaldetails.ToList();
//                for (int i = 0; i < detailsList.Count; i++)
//                {
//                    ModelState.Remove($"Journaldetails[{i}].Account");
//                    ModelState.Remove($"Journaldetails[{i}].Journal");
//                }
//            }

//            // 👤 تحديد المستخدم الحالي
//            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserID")?.Value;
//            if (!string.IsNullOrEmpty(userIdStr)) entry.CreatedBy = int.Parse(userIdStr);

//            // 🏢 ختم القيد بالفرع النشط
//            entry.BranchId = ActiveBranchId;

//            decimal totalDebit = entry.Journaldetails?.Sum(d => d.Debit) ?? 0;
//            decimal totalCredit = entry.Journaldetails?.Sum(d => d.Credit) ?? 0;

//            // 1. التحقق من التوازن المحاسبي
//            if (Math.Abs(totalDebit - totalCredit) > 0.01m || totalDebit <= 0)
//            {
//                ViewBag.Error = "عذراً، يجب أن يكون مجموع الطرف المدين مساوياً للطرف الدائن (القيد غير متزن).";
//                ReloadViewData();
//                return View(entry);
//            }

//            // 🚀 2. الحاجز الأمني الخلفي (Backend Validation): منع استخدام الحسابات الرئيسية
//            if (entry.Journaldetails != null)
//            {
//                foreach (var detail in entry.Journaldetails)
//                {
//                    bool isParentAccount = _context.Accounts.Any(a => a.ParentAccountId == detail.AccountId);
//                    if (isParentAccount)
//                    {
//                        var badAccount = _context.Accounts.Find(detail.AccountId);
//                        ViewBag.Error = $"عذراً، الحساب ({badAccount?.AccountName}) هو حساب رئيسي. لا يمكن تسجيل قيود مالية عليه مباشرة، يرجى اختيار حساب فرعي.";
//                        ReloadViewData();
//                        return View(entry);
//                    }
//                }
//            }

//            if (ModelState.IsValid)
//            {
//                var strategy = _context.Database.CreateExecutionStrategy();
//                try
//                {
//                    await strategy.ExecuteAsync(async () =>
//                    {
//                        using (var transaction = await _context.Database.BeginTransactionAsync())
//                        {
//                            try
//                            {
//                                entry.ReferenceType = "Manual";
//                                entry.IsPosted = true;
//                                entry.JournalDate = DateTime.Now;

//                                _context.Journalentries.Add(entry);
//                                await _context.SaveChangesAsync();

//                                // 💡 لاحظ هنا: تم حذف كود (account.Balance += ...) 
//                                // التزاماً بالمرحلة الثانية من هندسة الـ ERP، حيث يُحسب الرصيد ديناميكياً من الحركات.

//                                await transaction.CommitAsync();
//                            }
//                            catch (Exception)
//                            {
//                                await transaction.RollbackAsync();
//                                throw;
//                            }
//                        }
//                    });

//                    await RecordLog("Add", "JournalEntries", $"تم تسجيل قيد محاسبي مُرحل رقم {entry.JournalId} بقيمة {totalDebit} لفرع {entry.BranchId}");
//                    TempData["Success"] = "تم ترحيل القيد بنجاح.";
//                    return RedirectToAction(nameof(Index));
//                }
//                catch (Exception ex)
//                {
//                    ViewBag.Error = "فشل الحفظ في قاعدة البيانات: " + ex.Message;
//                }
//            }

//            ReloadViewData();
//            return View(entry);
//        }
//        private void ReloadViewData()
//        {
//            // 🚀 الحاجز الأمني الأمامي (Frontend UI): تصفية القائمة المنسدلة
//            // نجلب الحسابات النشطة "التي ليس لها أبناء" (الحسابات الفرعية النهائية فقط)
//            ViewBag.Accounts = _context.Accounts
//                .Where(a => a.IsActive == true && !_context.Accounts.Any(child => child.ParentAccountId == a.AccountId))
//                .OrderBy(a => a.AccountCode)
//                .Select(a => new {
//                    accountId = a.AccountId,
//                    accountCode = a.AccountCode,
//                    accountName = a.AccountName
//                }).ToList();
//        }

//        // ==========================================
//        // 👁️ 4. تفاصيل القيد (محمية بالعزل الشامل)
//        // ==========================================
//        [HttpGet]
//        [HasPermission("JournalEntries", "View")]
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null) return NotFound();

//            var entry = await _context.Journalentries
//                .Include(j => j.Branch)
//                .Include(j => j.Journaldetails).ThenInclude(d => d.Account)
//                .FirstOrDefaultAsync(m => m.JournalId == id);

//            if (entry == null) return NotFound();

//            if (entry.BranchId != ActiveBranchId)
//            {
//                return RedirectToAction("AccessDenied", "Home");
//            }

//            return View(entry);
//        }

//        // ==========================================
//        // 🔄 5. عكس القيد المحاسبي المطور (محمي بالعزل)
//        // ==========================================
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [HasPermission("JournalEntries", "Add")]
//        public async Task<IActionResult> Reverse(int id, string reason)
//        {
//            if (string.IsNullOrWhiteSpace(reason))
//            {
//                TempData["Error"] = "يجب كتابة سبب عكس القيد لإتمام العملية.";
//                return RedirectToAction(nameof(Details), new { id = id });
//            }

//            var strategy = _context.Database.CreateExecutionStrategy();

//            var originalEntryCheck = await _context.Journalentries.AsNoTracking().FirstOrDefaultAsync(j => j.JournalId == id);
//            if (originalEntryCheck == null) return NotFound();

//            if (originalEntryCheck.BranchId != ActiveBranchId)
//            {
//                return RedirectToAction("AccessDenied", "Home");
//            }

//            if (originalEntryCheck.ReferenceType == "Reversal")
//            {
//                TempData["Error"] = "لا يمكن عكس قيد هو بالفعل قيد عكسي!";
//                return RedirectToAction(nameof(Details), new { id = id });
//            }

//            try
//            {
//                await strategy.ExecuteAsync(async () =>
//                {
//                    using (var transaction = await _context.Database.BeginTransactionAsync())
//                    {
//                        try
//                        {
//                            var originalEntry = await _context.Journalentries
//                                .Include(j => j.Journaldetails)
//                                .FirstOrDefaultAsync(j => j.JournalId == id);

//                            var reversalEntry = new Journalentries
//                            {
//                                JournalDate = DateTime.Now,
//                                Description = $"إلغاء وعكس القيد رقم (#{originalEntry.JournalId}) - السبب: {reason} - البيان الأصلي: {originalEntry.Description}",
//                                BranchId = originalEntry.BranchId,
//                                CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "1"),
//                                IsPosted = true,
//                                ReferenceType = "Reversal"
//                            };

//                            _context.Journalentries.Add(reversalEntry);
//                            await _context.SaveChangesAsync();

//                            foreach (var oldDetail in originalEntry.Journaldetails)
//                            {
//                                var newDetail = new Journaldetails
//                                {
//                                    JournalId = reversalEntry.JournalId,
//                                    AccountId = oldDetail.AccountId,
//                                    Debit = oldDetail.Credit,
//                                    Credit = oldDetail.Debit
//                                };
//                                _context.Journaldetails.Add(newDetail);

//                                var account = await _context.Accounts.FindAsync(oldDetail.AccountId);
//                                if (account != null)
//                                {
//                                    account.Balance += (newDetail.Debit - newDetail.Credit);
//                                }
//                            }

//                            originalEntry.Description += $" [تم عكس هذا القيد بالقيد رقم #{reversalEntry.JournalId}]";
//                            _context.Update(originalEntry);

//                            await _context.SaveChangesAsync();
//                            await transaction.CommitAsync();
//                        }
//                        catch (Exception)
//                        {
//                            await transaction.RollbackAsync();
//                            throw;
//                        }
//                    }
//                });

//                await RecordLog("Reverse", "JournalEntries", $"تم عكس القيد رقم {id} في الفرع {originalEntryCheck.BranchId} للسبب: {reason}");
//                TempData["Success"] = "تم عكس القيد وتصفير أثره بنجاح مع توثيق السبب.";
//                return RedirectToAction(nameof(Index));
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = "خطأ فني أثناء عملية العكس: " + ex.Message;
//                return RedirectToAction(nameof(Details), new { id = id });
//            }
//        }
//    }
//}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class JournalEntriesController : BaseController
    {
        public JournalEntriesController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📄 1. عرض سجل القيود اليومية (مفلتر بالفرع النشط للجميع)
        // ==========================================
        [HttpGet]
        [HasPermission("JournalEntries", "View")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 50;

            // 🚀 هندسة الأداء (Performance): استعلام الإحصائيات الفردية لتجنب تجميع الملايين في الـ RAM
            var debitVolume = await _context.Journaldetails
                .Where(jd => jd.Journal.BranchId == ActiveBranchId)
                .SumAsync(jd => (decimal?)jd.Debit) ?? 0m;
            
            var totalEntriesCount = await _context.Journalentries
                .CountAsync(j => j.BranchId == ActiveBranchId);
                
            var draftedEntriesCount = await _context.Journalentries
                .CountAsync(j => j.BranchId == ActiveBranchId && !j.IsPosted);

            ViewBag.TotalVolume = debitVolume;
            ViewBag.TotalEntriesCount = totalEntriesCount;
            ViewBag.DraftedEntriesCount = draftedEntriesCount;

            // 🚀 نظام التقسيم الذكي (Server-Side Pagination) بـ AsNoTracking
            var baseQuery = _context.Journalentries
                .AsNoTracking()
                .Include(j => j.Branch)
                .Include(j => j.Journaldetails)
                .Where(j => j.BranchId == ActiveBranchId);

            int totalRecords = totalEntriesCount; // Reuse count

            var entries = await baseQuery
                .OrderByDescending(j => j.JournalDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(entries);
        }

        // ==========================================
        // ➕ 2. شاشة إضافة قيد جديد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("JournalEntries", "Add")]
        public IActionResult Create()
        {
            ReloadViewData();
            return View();
        }

        // ==========================================
        // 💾 3. حفظ القيد المحدث (بدون تحديث حقل Balance)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("JournalEntries", "Add")]
        public async Task<IActionResult> Create(Journalentries entry)
        {
            // 🛡️ تنظيف الـ ModelState من المتعلقات الملاحية لمنع فشل التحقق
            ModelState.Remove("Branch");
            ModelState.Remove("CreatedByNavigation");
            if (entry.Journaldetails != null)
            {
                var detailsList = entry.Journaldetails.ToList();
                for (int i = 0; i < detailsList.Count; i++)
                {
                    ModelState.Remove($"Journaldetails[{i}].Account");
                    ModelState.Remove($"Journaldetails[{i}].Journal");
                }
            }

            // 👤 تحديد المستخدم الحالي
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserID")?.Value;
            if (!string.IsNullOrEmpty(userIdStr)) entry.CreatedBy = int.Parse(userIdStr);

            // 🏢 ختم القيد بالفرع النشط
            entry.BranchId = ActiveBranchId;

            decimal totalDebit = entry.Journaldetails?.Sum(d => d.Debit) ?? 0;
            decimal totalCredit = entry.Journaldetails?.Sum(d => d.Credit) ?? 0;

            // 1. التحقق من التوازن المحاسبي
            if (Math.Abs(totalDebit - totalCredit) > 0.01m || totalDebit <= 0)
            {
                ViewBag.Error = "عذراً، يجب أن يكون مجموع الطرف المدين مساوياً للطرف الدائن (القيد غير متزن).";
                ReloadViewData();
                return View(entry);
            }

            // 🚀 2. الحاجز الأمني الخلفي (Backend Validation): منع استخدام الحسابات الرئيسية
            if (entry.Journaldetails != null)
            {
                foreach (var detail in entry.Journaldetails)
                {
                    bool isParentAccount = _context.Accounts.Any(a => a.AccountId == detail.AccountId && a.IsParent == true);
                    if (isParentAccount)
                    {
                        var badAccount = _context.Accounts.Find(detail.AccountId);
                        ViewBag.Error = $"عذراً، الحساب ({badAccount?.AccountName}) هو حساب رئيسي. لا يمكن تسجيل قيود مالية عليه مباشرة، يرجى اختيار حساب فرعي.";
                        ReloadViewData();
                        return View(entry);
                    }
                }
            }

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
                                entry.ReferenceType = "Manual";
                                entry.IsPosted = true;
                                entry.JournalDate = DateTime.Now;

                                _context.Journalentries.Add(entry);
                                await _context.SaveChangesAsync();

                                // 💡 التزاماً بالمرحلة الثانية من هندسة الـ ERP، تم إزالة أي كود يقوم بتحديث جدول الحسابات هنا.

                                await transaction.CommitAsync();
                            }
                            catch (Exception)
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    });

                    await RecordLog("Add", "JournalEntries", $"تم تسجيل قيد محاسبي مُرحل رقم {entry.JournalId} بقيمة {totalDebit} لفرع {entry.BranchId}");
                    TempData["Success"] = "تم ترحيل القيد بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "فشل الحفظ في قاعدة البيانات: " + ex.Message;
                }
            }

            ReloadViewData();
            return View(entry);
        }

        private void ReloadViewData()
        {
            // 🚀 الحاجز الأمني الأمامي (Frontend UI): تصفية القائمة المنسدلة
            // نجلب الحسابات النشطة "التي ليس لها أبناء" (الحسابات الفرعية النهائية فقط)
            ViewBag.Accounts = _context.Accounts
                .Where(a => a.IsActive == true && a.IsParent == false)
                .OrderBy(a => a.AccountCode)
                .Select(a => new {
                    accountId = a.AccountId,
                    accountCode = a.AccountCode,
                    accountName = a.AccountName
                }).ToList();
        }

        // ==========================================
        // 👁️ 4. تفاصيل القيد (محمية بالعزل الشامل)
        // ==========================================
        [HttpGet]
        [HasPermission("JournalEntries", "View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var entry = await _context.Journalentries
                .Include(j => j.Branch)
                .Include(j => j.Journaldetails).ThenInclude(d => d.Account)
                .FirstOrDefaultAsync(m => m.JournalId == id);

            if (entry == null) return NotFound();

            if (entry.BranchId != ActiveBranchId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View(entry);
        }

        // ==========================================
        // 🔄 5. عكس القيد المحاسبي المطور (بدون Balance)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("JournalEntries", "Add")]
        public async Task<IActionResult> Reverse(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "يجب كتابة سبب عكس القيد لإتمام العملية.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            var originalEntryCheck = await _context.Journalentries.AsNoTracking().FirstOrDefaultAsync(j => j.JournalId == id);
            if (originalEntryCheck == null) return NotFound();

            if (originalEntryCheck.BranchId != ActiveBranchId)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (originalEntryCheck.ReferenceType == "Reversal")
            {
                TempData["Error"] = "لا يمكن عكس قيد هو بالفعل قيد عكسي!";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            var originalEntry = await _context.Journalentries
                                .Include(j => j.Journaldetails)
                                .FirstOrDefaultAsync(j => j.JournalId == id);

                            var reversalEntry = new Journalentries
                            {
                                JournalDate = DateTime.Now,
                                Description = $"إلغاء وعكس القيد رقم (#{originalEntry.JournalId}) - السبب: {reason} - البيان الأصلي: {originalEntry.Description}",
                                BranchId = originalEntry.BranchId,
                                CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "1"),
                                IsPosted = true,
                                ReferenceType = "Reversal"
                            };

                            _context.Journalentries.Add(reversalEntry);
                            await _context.SaveChangesAsync();

                            foreach (var oldDetail in originalEntry.Journaldetails)
                            {
                                var newDetail = new Journaldetails
                                {
                                    JournalId = reversalEntry.JournalId,
                                    AccountId = oldDetail.AccountId,
                                    Debit = oldDetail.Credit,   // الدائن يصبح مديناً
                                    Credit = oldDetail.Debit    // المدين يصبح دائناً
                                };
                                _context.Journaldetails.Add(newDetail);

                                // 💡 التزاماً بالمرحلة الثانية: تم حذف تحديث الرصيد من هنا أيضاً!
                            }

                            originalEntry.Description += $" [تم عكس هذا القيد بالقيد رقم #{reversalEntry.JournalId}]";
                            _context.Update(originalEntry);

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

                await RecordLog("Reverse", "JournalEntries", $"تم عكس القيد رقم {id} في الفرع {originalEntryCheck.BranchId} للسبب: {reason}");
                TempData["Success"] = "تم عكس القيد وتصفير أثره بنجاح مع توثيق السبب.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطأ فني أثناء عملية العكس: " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
    }
}
///* =============================================================================================
//📑 الكتالوج والدليل الفني للكنترولر (JournalEntriesController)
//=============================================================================================
//الوظيفة العامة: 
//هذا الكنترولر هو "المحرك المالي المباشر" للنظام (Financial Journal Engine).
//يختص بإنشاء، ترحيل، وعكس القيود اليومية (المدينة والدائنة) بشكل يدوي. 
//يضمن هذا الكنترولر توازن القيود (المدين = الدائن) ويطبق الأثر المالي فوراً 
//على شجرة الحسابات المركزية باستخدام الـ Transactions لضمان سلامة قاعدة البيانات.

//ملاحظة معمارية بخصوص العزل الشامل (Context-Aware Isolation):
//- تم تطبيق العزل التام على **كافة المستخدمين بما فيهم المدير العام (SuperAdmin)**.
//- لماذا نعزل المدير هنا؟ لأن "القيد المالي" حركة تشغيلية دقيقة. إذا سمحنا للمدير بإضافة
//  قيد دون تحديد فرع دقيق، فقد تختلط ميزانيات الفروع. بدلاً من ذلك، المدير يعمل الآن
//  داخل "سياق الفرع النشط" (ActiveBranchId). 
//- العرض (Index): لا يعرض سوى قيود الفرع النشط.
//- الإضافة (Create): يتم الختم آلياً. وتم إزالة حقل "الفرع" من الواجهة لمنع التلاعب.
//- العكس والتفاصيل (Reverse/Details): يُمنع المدير من عكس قيد لا يخص الفرع النشط الذي 
//  يقف عليه حالياً، وهذا يمنع الأخطاء الكارثية (مثلاً: أن يعكس قيداً لفرع عدن بينما هو 
//  يستعرض تقارير فرع صنعاء).

//محتويات الكنترولر والدوال (Methods):

//1. [HttpGet] Index()
//   - الوظيفة: جلب القيود المحاسبية للفرع النشط فقط.

//2. [HttpGet/HttpPost] Create(entry)
//   - الوظيفة: إدخال قيد جديد. 
//   - الأمان المالي: يتحقق من (توازن القيد)، ويطبق عملية الترحيل التلقائي `IsPosted = true`،
//     ويختم القيد بالفرع `ActiveBranchId` للمدير والموظف على حد سواء.

//3. [HttpGet] Details(id)
//   - الوظيفة: استعراض تفاصيل القيد وطرفيه. محمية ضد استعراض قيود فروع أخرى.

//4. [HttpPost] Reverse(id, reason)
//   - الوظيفة: تطبيق "مبدأ المحاسبة السليم" بمنع حذف القيود، واستبدالها بالقيد العكسي (Reversal).
//   - المنطق: ينشئ قيداً جديداً يعكس الأطراف (المدين دائن، والدائن مدين)، ويُثبت سبب الإلغاء،
//     ويربط القيد العكسي بنفس الفرع `ActiveBranchId` الذي نشأ فيه القيد الأصلي.
//=============================================================================================
//*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System.Linq;
using System.Threading.Tasks;
using PharmaSmartWeb.Filters;
using System;
using System.Collections.Generic;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class AccountingController : BaseController
    {
        public AccountingController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 🌳 1. عرض الدليل المحاسبي (شجرة عامة لا تعزل)
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "View")]
        public async Task<IActionResult> Index()
        {
            // الدليل المحاسبي يظل Master Data موحد لكل الفروع
            var accounts = await _context.Accounts
                .Include(a => a.ParentAccount)
                .Include(a => a.SubAccounts)
                .OrderBy(a => a.AccountCode)
                .ToListAsync();

            return View(accounts);
        }

        // ==========================================
        // 📊 2. ميزان المراجعة الهرمي المصحح
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "View")]
        public async Task<IActionResult> TrialBalance()
        {
            // جلب كافة الحسابات النشطة
            var allAccounts = await _context.Accounts
                .Where(a => a.IsActive == true)
                .ToListAsync();

            // جلب كافة الحركات المرحلة للفرع الحالي
            var branchTransactions = await _context.Journaldetails
                .Include(jd => jd.Journal)
                .Where(jd => jd.Journal.IsPosted == true && jd.Journal.BranchId == ActiveBranchId)
                .ToListAsync();

            // 🚀 الخطوة (1): حساب الأرصدة المباشرة لكل حساب من واقع القيود فقط
            foreach (var acc in allAccounts)
            {
                decimal totalD = branchTransactions.Where(jd => jd.AccountId == acc.AccountId).Sum(jd => jd.Debit);
                decimal totalC = branchTransactions.Where(jd => jd.AccountId == acc.AccountId).Sum(jd => jd.Credit);

                // الرصيد الأولي (الحركات المباشرة فقط على هذا الحساب)
                acc.Balance = acc.AccountNature ? (totalD - totalC) : (totalC - totalD);
            }

            // 🚀 الخطوة (2): محرك التجميع التصاعدي (Bottom-Up Roll-up)
            // نبدأ من الحسابات الأكثر عمقاً (أطول كود) ونصعد للأباء
            var orderedByDepth = allAccounts.OrderByDescending(a => a.AccountCode.Length).ToList();
            foreach (var acc in orderedByDepth)
            {
                if (acc.ParentAccountId.HasValue)
                {
                    var parent = allAccounts.FirstOrDefault(p => p.AccountId == acc.ParentAccountId.Value);
                    if (parent != null)
                    {
                        // ترحيل رصيد الابن ليُجمع في رصيد الأب
                        parent.Balance += acc.Balance;
                    }
                }
            }

            // 🚀 الخطوة (3): حساب إجماليات الميزان النهائية (من الحسابات الجذرية فقط لمنع التكرار)
            decimal exactTotalDebit = 0m;
            decimal exactTotalCredit = 0m;
            var rootAccounts = allAccounts.Where(a => a.ParentAccountId == null).ToList();

            foreach (var root in rootAccounts)
            {
                // الحساب الجذري يحتوي الآن على مجموع توازنات شجرته بالكامل
                if (root.AccountNature) // أصول أو مصروفات
                {
                    if (root.Balance > 0) exactTotalDebit += root.Balance;
                    else if (root.Balance < 0) exactTotalCredit += Math.Abs(root.Balance);
                }
                else // التزامات، ملكية، إيرادات
                {
                    if (root.Balance > 0) exactTotalCredit += root.Balance;
                    else if (root.Balance < 0) exactTotalDebit += Math.Abs(root.Balance);
                }
            }

            ViewBag.ExactTotalDebit = exactTotalDebit;
            ViewBag.ExactTotalCredit = exactTotalCredit;

            // 🚀 الخطوة (4): إخفاء الحسابات الصفرية وإعادة الترتيب (تنفيذاً لطلبك)
            var trialBalanceList = allAccounts
                .Where(a => Math.Round(a.Balance, 2) != 0) // نستخدم التقريب لمنع ظهور أصفار بكسور متناهية الصغر
                .OrderBy(a => a.AccountCode)
                .ToList();

            return View(trialBalanceList);
        }

        // ==========================================
        // 🛠️ دالة مساعدة ذكية لفلترة "الآباء" فقط
        // ==========================================
        private void PopulateParentAccounts(int? selectedValue = null, int? currentAccountId = null)
        {
            var allActiveAccounts = _context.Accounts
                .Where(a => a.IsActive == true)
                .OrderBy(a => a.AccountCode)
                .ToList();

            if (currentAccountId.HasValue)
            {
                allActiveAccounts = allActiveAccounts.Where(a => a.AccountId != currentAccountId.Value).ToList();
            }

            var eligibleParents = new List<object>();

            foreach (var acc in allActiveAccounts)
            {
                bool hasTransactions = _context.Journaldetails.Any(jd => jd.AccountId == acc.AccountId);

                if (!hasTransactions)
                {
                    string indent = new string(' ', (acc.AccountCode.Length - 1) * 3);
                    string prefix = acc.AccountCode.Length > 1 ? "└─ " : "";

                    eligibleParents.Add(new
                    {
                        AccountId = acc.AccountId,
                        DisplayName = $"{indent}{prefix}{acc.AccountCode} - {acc.AccountName}"
                    });
                }
            }

            ViewBag.ParentAccounts = new SelectList(eligibleParents, "AccountId", "DisplayName", selectedValue);
        }

        // ==========================================
        // ➕ 2. شاشة إضافة حساب جديد
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "Add")]
        public IActionResult Create()
        {
            PopulateParentAccounts();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Accounting", "Add")]
        public async Task<IActionResult> Create(Accounts account)
        {
            if (account.AccountType == "Assets" || account.AccountType == "Expenses")
            {
                account.AccountNature = true; // 1 = مدين
            }
            else
            {
                account.AccountNature = false; // 0 = دائن
            }

            ModelState.Remove("ParentAccount");
            ModelState.Remove("SubAccounts");
            ModelState.Remove("Customers");
            ModelState.Remove("FundtransfersFromAccount");
            ModelState.Remove("FundtransfersToAccount");
            ModelState.Remove("Journaldetails");
            ModelState.Remove("Sales");
            ModelState.Remove("Suppliers");

            if (ModelState.IsValid)
            {
                if (_context.Accounts.Any(a => a.AccountCode == account.AccountCode))
                {
                    ViewBag.Error = "كود الحساب هذا مسجل مسبقاً، يرجى اختيار كود آخر.";
                    PopulateParentAccounts(account.ParentAccountId);
                    return View(account);
                }

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                await RecordLog("Add", "Accounting", $"تم إنشاء حساب جديد: {account.AccountName} بكود ({account.AccountCode})");
                TempData["Success"] = "تم إضافة الحساب بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            PopulateParentAccounts(account.ParentAccountId);
            return View(account);
        }

        // ==========================================
        // 📝 3. شاشة تعديل حساب (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            PopulateParentAccounts(account.ParentAccountId, id);
            return View(account);
        }

        // ==========================================
        // 💾 4. حفظ تعديلات الحساب (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Accounting", "Edit")]
        public async Task<IActionResult> Edit(int id, Accounts updatedAccount)
        {
            if (id != updatedAccount.AccountId) return NotFound();

            ModelState.Remove("ParentAccount");
            ModelState.Remove("SubAccounts");
            ModelState.Remove("Customers");
            ModelState.Remove("FundtransfersFromAccount");
            ModelState.Remove("FundtransfersToAccount");
            ModelState.Remove("Journaldetails");
            ModelState.Remove("Sales");
            ModelState.Remove("Suppliers");

            if (ModelState.IsValid)
            {
                if (_context.Accounts.Any(a => a.AccountCode == updatedAccount.AccountCode && a.AccountId != id))
                {
                    ViewBag.Error = "عذراً، كود الحساب مسجل مسبقاً لحساب آخر.";
                    PopulateParentAccounts(updatedAccount.ParentAccountId, id);
                    return View(updatedAccount);
                }

                try
                {
                    var existingAccount = await _context.Accounts.FindAsync(id);
                    if (existingAccount == null) return NotFound();

                    // 🛡️ الحاجز الأمني (Anti-Tamper Logic): منع تعديل نوع الحساب أو ارتباطه بحساب أب إذا كانت هناك قيود مرحلة
                    bool hasPostedTransactions = await _context.Journaldetails
                        .AnyAsync(jd => jd.AccountId == id && jd.Journal.IsPosted == true);

                    if (hasPostedTransactions)
                    {
                        if (existingAccount.AccountType != updatedAccount.AccountType ||
                            existingAccount.ParentAccountId != updatedAccount.ParentAccountId)
                        {
                            ViewBag.Error = "حماية النظام (Anti-Tamper): لا يمكن تغيير (نوع الحساب) أو (الحساب الأب) لحساب مالي يمتلك قيوداً مرحلة. لتعديل ذلك، يجب عكس القيود أولاً.";
                            PopulateParentAccounts(updatedAccount.ParentAccountId, id);
                            return View(updatedAccount);
                        }
                    }

                    existingAccount.AccountCode = updatedAccount.AccountCode;
                    existingAccount.AccountName = updatedAccount.AccountName;
                    existingAccount.AccountType = updatedAccount.AccountType;
                    existingAccount.ParentAccountId = updatedAccount.ParentAccountId;
                    existingAccount.IsActive = updatedAccount.IsActive;

                    if (updatedAccount.AccountType == "Assets" || updatedAccount.AccountType == "Expenses")
                    {
                        existingAccount.AccountNature = true; // مدين
                    }
                    else
                    {
                        existingAccount.AccountNature = false; // دائن
                    }

                    _context.Update(existingAccount);
                    await _context.SaveChangesAsync();

                    await RecordLog("Edit", "Accounting", $"تم تعديل بيانات الحساب: {existingAccount.AccountName} (كود {existingAccount.AccountCode})");
                    TempData["Success"] = "تم تحديث بيانات الحساب بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Accounts.Any(e => e.AccountId == updatedAccount.AccountId)) return NotFound();
                    else throw;
                }
            }

            PopulateParentAccounts(updatedAccount.ParentAccountId, id);
            return View(updatedAccount);
        }

        // ==========================================
        // 🤖 دالة API لتوليد كود الحساب تلقائياً (AJAX) 
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetNextAccountCode(int? parentId)
        {
            if (parentId == null || parentId <= 0)
                return Json(new { success = false });

            var parent = await _context.Accounts.FindAsync(parentId);
            if (parent == null)
                return Json(new { success = false });

            var children = await _context.Accounts
                .Where(a => a.ParentAccountId == parentId)
                .OrderByDescending(a => a.AccountCode)
                .ToListAsync();

            string nextCode = "";

            if (children.Any())
            {
                string maxCodeStr = children.First().AccountCode;
                if (long.TryParse(maxCodeStr, out long maxCode))
                {
                    nextCode = (maxCode + 1).ToString();
                    nextCode = nextCode.PadLeft(maxCodeStr.Length, '0');
                }
            }
            else
            {
                if (parent.AccountCode.Length == 1)
                {
                    nextCode = parent.AccountCode + "1";
                }
                else if (parent.AccountCode.Length == 2)
                {
                    if (parent.AccountCode.StartsWith("1") || parent.AccountCode.StartsWith("2"))
                    {
                        nextCode = parent.AccountCode + "1";
                    }
                    else
                    {
                        nextCode = parent.AccountCode + "01";
                    }
                }
                else
                {
                    nextCode = parent.AccountCode + "01";
                }
            }

            return Json(new { success = true, code = nextCode });
        }

        // ==========================================
        // 📖 6. تقرير كشف الحساب التفصيلي
        // ==========================================
        [HttpGet]
        [HasPermission("Accounting", "View")]
        public async Task<IActionResult> Ledger(int? accountId, DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Accounts = await _context.Accounts.Where(a => a.IsActive == true).OrderBy(a => a.AccountCode).ToListAsync();

            if (accountId == null) return View(new List<Journaldetails>());

            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var selectedAccount = await _context.Accounts.FindAsync(accountId);
            ViewBag.SelectedAccount = selectedAccount;

            if (selectedAccount == null) return View(new List<Journaldetails>());

            ViewBag.FromDate = start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            ViewBag.ToDate = end.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            var targetAccountIds = await _context.Accounts
                .Where(a => a.AccountCode.StartsWith(selectedAccount.AccountCode))
                .Select(a => a.AccountId)
                .ToListAsync();

            var transactions = await _context.Journaldetails
                .Include(d => d.Journal)
                .Include(d => d.Account)
                .Where(d => targetAccountIds.Contains(d.AccountId) &&
                            d.Journal.JournalDate >= start &&
                            d.Journal.JournalDate <= end &&
                            d.Journal.IsPosted == true &&
                            d.Journal.BranchId == ActiveBranchId)
                .OrderBy(d => d.Journal.JournalDate)
                .ToListAsync();

            decimal openingDebit = await _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => targetAccountIds.Contains(d.AccountId) &&
                            d.Journal.JournalDate < start &&
                            d.Journal.IsPosted == true &&
                            d.Journal.BranchId == ActiveBranchId)
                .SumAsync(d => (decimal?)d.Debit) ?? 0m;

            decimal openingCredit = await _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => targetAccountIds.Contains(d.AccountId) &&
                            d.Journal.JournalDate < start &&
                            d.Journal.IsPosted == true &&
                            d.Journal.BranchId == ActiveBranchId)
                .SumAsync(d => (decimal?)d.Credit) ?? 0m;

            if (selectedAccount.AccountNature)
            {
                ViewBag.OpeningBalance = openingDebit - openingCredit;
            }
            else
            {
                ViewBag.OpeningBalance = openingCredit - openingDebit;
            }

            return View(transactions);
        }
    }

    public static class DateExtensions
    {
        public static DateTime AsDateTime(this DateTime date) => date;
    }
}
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using PharmaSmartWeb.Models;
//using PharmaSmartWeb.Filters;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//namespace PharmaSmartWeb.Controllers
//{
//    [Authorize]
//    public class AccountingController : BaseController
//    {
//        public AccountingController(ApplicationDbContext context) : base(context) { }

//        // ==========================================
//        // 🌳 1. عرض الدليل المحاسبي (شجرة عامة لا تعزل)
//        // ==========================================
//        [HttpGet]
//        [HasPermission("Accounting", "View")]
//        public async Task<IActionResult> Index()
//        {
//            // الدليل المحاسبي يظل Master Data موحد لكل الفروع
//            var accounts = await _context.Accounts
//                .Include(a => a.ParentAccount)
//                .Include(a => a.SubAccounts) 
//                .OrderBy(a => a.AccountCode)
//                .ToListAsync();

//            return View(accounts);
//        }

//    }
    

//    public static class DateExtensions
//    {
//        public static DateTime AsDateTime(this DateTime date) => date;
//    }
//}
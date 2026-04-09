using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class VouchersController : BaseController
    {
        public VouchersController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 🛡️ دالة استخراج رقم المستخدم بأمان
        // ==========================================
        private async Task<int> GetValidUserIdAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
            {
                if (await _context.Users.AnyAsync(u => u.UserId == parsedId)) return parsedId;
            }
            throw new Exception("انتهت صلاحية الجلسة أو تعذر التحقق من هوية المستخدم. يرجى تسجيل الدخول مجدداً.");
        }

        // ==========================================
        // 📄 1. عرض سجل السندات (معزول بالفرع النشط)
        // ==========================================
        [HttpGet]
        [HasPermission("Vouchers", "View")]
        public async Task<IActionResult> Index()
        {
            int currentBranchId = ActiveBranchId; // 🚀 حماية الاتصال (Thread Safety)

            var query = _context.Journalentries
                .Include(j => j.Journaldetails).ThenInclude(d => d.Account)
                .Where(j => (j.ReferenceType == "Receipt" || j.ReferenceType == "Payment") && j.BranchId == currentBranchId)
                .OrderByDescending(j => j.JournalDate);

            var vouchers = await query.ToListAsync();
            return View(vouchers);
        }

        // ==========================================
        // 👁️ 2. عرض تفاصيل السند
        // ==========================================
        [HttpGet]
        [HasPermission("Vouchers", "View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            int currentBranchId = ActiveBranchId; // 🚀 حماية الأمان (IDOR Prevention)

            var voucher = await _context.Journalentries
                .Include(j => j.Journaldetails).ThenInclude(d => d.Account)
                .Include(j => j.Branch)
                .Include(j => j.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.JournalId == id && m.BranchId == currentBranchId);

            if (voucher == null) return NotFound();

            return View(voucher);
        }

        // ==========================================
        // ➕ 3. شاشة إنشاء سند جديد (GET)
        // ==========================================
        [HttpGet]
        [HasPermission("Vouchers", "Add")]
        public IActionResult Create(string type)
        {
            if (string.IsNullOrEmpty(type)) type = "Receipt";
            ViewBag.VoucherType = type;
            PrepareViewData();
            return View();
        }

        // ==========================================
        // 💾 4. حفظ السند (الختم الآلي الموزع والمعزول)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Vouchers", "Add")]
        public async Task<IActionResult> Create(string voucherType, int mainAccountId, int secondAccountId,
                                              decimal amount, string notes, DateTime date,
                                              string paymentMode, string referenceNo, string payeePayerName)
        {
            int currentBranchId = ActiveBranchId;

            if (amount <= 0)
            {
                ViewBag.Error = "عذراً، يجب أن يكون مبلغ السند أكبر من الصفر.";
                return ReloadForm(voucherType);
            }

            if (mainAccountId == secondAccountId)
            {
                ViewBag.Error = "خطأ محاسبي: لا يمكن إجراء قيد بين الحساب ونفسه! يرجى اختيار حسابين مختلفين.";
                return ReloadForm(voucherType);
            }

            bool isMainParent = await _context.Accounts.AnyAsync(a => a.AccountId == mainAccountId && a.IsParent == true);
            bool isSecondParent = await _context.Accounts.AnyAsync(a => a.AccountId == secondAccountId && a.IsParent == true);

            if (isMainParent || isSecondParent)
            {
                ViewBag.Error = "عذراً، لا يمكن استخدام حسابات رئيسية (آباء) في السندات المالية. يرجى اختيار حسابات فرعية نهائية فقط.";
                return ReloadForm(voucherType);
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
                            int userId = await GetValidUserIdAsync();

                            var entry = new Journalentries
                            {
                                JournalDate = date,
                                Description = (voucherType == "Receipt" ? "قبض من: " : "صرف لـ: ") + payeePayerName + " - " + notes,
                                ReferenceType = voucherType,
                                ReferenceNo = referenceNo,
                                PayeePayerName = payeePayerName,
                                BranchId = currentBranchId,
                                CreatedBy = userId,
                                IsPosted = true
                            };

                            _context.Journalentries.Add(entry);
                            await _context.SaveChangesAsync();

                            decimal mainDebit = (voucherType == "Receipt") ? amount : 0;
                            decimal mainCredit = (voucherType == "Receipt") ? 0 : amount;
                            decimal secondDebit = (voucherType == "Receipt") ? 0 : amount;
                            decimal secondCredit = (voucherType == "Receipt") ? amount : 0;

                            _context.Journaldetails.Add(new Journaldetails { JournalId = entry.JournalId, AccountId = mainAccountId, Debit = mainDebit, Credit = mainCredit });
                            var mainAcc = await _context.Accounts.FindAsync(mainAccountId);
                            if (mainAcc != null) mainAcc.Balance += (mainDebit - mainCredit);

                            _context.Journaldetails.Add(new Journaldetails { JournalId = entry.JournalId, AccountId = secondAccountId, Debit = secondDebit, Credit = secondCredit });
                            var secondAcc = await _context.Accounts.FindAsync(secondAccountId);
                            if (secondAcc != null) secondAcc.Balance += (secondDebit - secondCredit);

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            string logMsg = $"إنشاء {(voucherType == "Receipt" ? "سند قبض" : "سند صرف")} رقم #{entry.JournalId} بمبلغ {amount:N2} من/إلى {payeePayerName}";
                            await RecordLog("Add", "Vouchers", logMsg);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception(ex.Message);
                        }
                    }
                });

                TempData["Success"] = "تم حفظ السند وترحيله وتحديث أرصدة الحسابات بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "حدث خطأ أثناء المعالجة المالية: " + ex.Message;
                return ReloadForm(voucherType);
            }
        }

        private IActionResult ReloadForm(string voucherType)
        {
            ViewBag.VoucherType = voucherType;
            PrepareViewData();
            return View();
        }

        private void PrepareViewData()
        {
            int currentBranchId = ActiveBranchId;

            // 🚀 استخدام الحقل IsParent لضمان عدم ظهور الحسابات التجميعية
            ViewBag.Funds = _context.Accounts
                .Where(a => a.IsActive == true &&
                            a.IsParent == false &&
                            (a.AccountName.Contains("صندوق") || a.AccountName.Contains("بنك") || a.AccountName.Contains("نقد")) &&
                            (a.BranchId == currentBranchId || a.BranchId == null))
                .ToList();

            ViewBag.AllAccounts = _context.Accounts
                .Where(a => a.IsActive == true &&
                            a.IsParent == false)
                .OrderBy(a => a.AccountCode)
                .ToList();
        }
    }
}
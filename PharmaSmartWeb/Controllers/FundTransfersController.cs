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
    public class FundTransfersController : BaseController
    {
        public FundTransfersController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📄 1. عرض سجل التحويلات (معزول بالفرع)
        // ==========================================
        [HttpGet]
        [HasPermission("FundTransfers", "View")]
        public async Task<IActionResult> Index()
        {
            var transfers = await _context.Fundtransfers
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .Include(t => t.User)
                .Where(t => t.BranchId == ActiveBranchId)
                .OrderByDescending(t => t.TransferDate)
                .ToListAsync();

            return View(transfers);
        }

        // ==========================================
        // ➕ 2. شاشة إجراء تحويل جديد
        // ==========================================
        [HttpGet]
        [HasPermission("FundTransfers", "Add")]
        public IActionResult Create()
        {
            PrepareAccounts();
            return View();
        }

        // ==========================================
        // 💾 3. حفظ واعتماد التحويل المالي (Core ERP Logic)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("FundTransfers", "Add")]
        public async Task<IActionResult> Create(int FromAccountId, int ToAccountId, decimal Amount, string Notes, string ReferenceNo)
        {
            if (Amount <= 0)
            {
                ViewBag.Error = "مبلغ التحويل يجب أن يكون أكبر من الصفر.";
                PrepareAccounts();
                return View();
            }

            if (FromAccountId == ToAccountId)
            {
                ViewBag.Error = "لا يمكن التحويل من الحساب إلى نفسه!";
                PrepareAccounts();
                return View();
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var fromAcc = await _context.Accounts.FindAsync(FromAccountId);
                        var toAcc = await _context.Accounts.FindAsync(ToAccountId);

                        if (fromAcc == null || toAcc == null) throw new Exception("أحد الحسابات المحددة غير موجود.");

                        // 1️⃣ التحقق من الرصيد الفعلي (نحسبه من القيود مباشرة لضمان الدقة المطلقة)
                        decimal debit = await _context.Journaldetails.Where(d => d.AccountId == FromAccountId && d.Journal.IsPosted).SumAsync(d => (decimal?)d.Debit) ?? 0;
                        decimal credit = await _context.Journaldetails.Where(d => d.AccountId == FromAccountId && d.Journal.IsPosted).SumAsync(d => (decimal?)d.Credit) ?? 0;
                        decimal actualBalance = fromAcc.AccountNature ? (debit - credit) : (credit - debit);

                        if (actualBalance < Amount)
                            throw new Exception($"الرصيد المتاح في ({fromAcc.AccountName}) غير كافٍ. الرصيد المتاح: {actualBalance:N2}");

                        int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "1");

                        // 2️⃣ إنشاء القيد المحاسبي (Contra Entry)
                        var journal = new Journalentries
                        {
                            JournalDate = DateTime.Now,
                            ReferenceType = "FundTransfer",
                            ReferenceNo = ReferenceNo,
                            Description = $"تحويل سيولة داخلية: من {fromAcc.AccountName} إلى {toAcc.AccountName} - {Notes}",
                            BranchId = ActiveBranchId,
                            CreatedBy = userId,
                            IsPosted = true
                        };
                        _context.Journalentries.Add(journal);
                        await _context.SaveChangesAsync();

                        // الطرف المدين (الحساب المُستقبل للأموال زاد)
                        _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = ToAccountId, Debit = Amount, Credit = 0 });

                        // الطرف الدائن (الحساب المُرسل للأموال نقص)
                        _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = FromAccountId, Debit = 0, Credit = Amount });

                        await _context.SaveChangesAsync();

                        // 3️⃣ توثيق العملية في جدول التحويلات التشغيلي
                        var transfer = new Fundtransfers
                        {
                            BranchId = ActiveBranchId,
                            FromAccountId = FromAccountId,
                            ToAccountId = ToAccountId,
                            Amount = Amount,
                            TransferDate = DateTime.Now,
                            ReferenceNo = ReferenceNo,
                            Notes = Notes,
                            CreatedBy = userId,
                            JournalId = journal.JournalId
                        };
                        _context.Fundtransfers.Add(transfer);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        await RecordLog("Transfer", "FundTransfers", $"تحويل {Amount:N2} من {fromAcc.AccountName} إلى {toAcc.AccountName}");
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });

                TempData["Success"] = "تم تنفيذ التحويل المالي وإنشاء القيد المحاسبي بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                PrepareAccounts();
                return View();
            }
        }

        private void PrepareAccounts()
        {
            // جلب حسابات الصناديق والبنوك النشطة التي ليس لها أبناء
            var funds = _context.Accounts
                .Where(a => a.IsActive == true && a.IsParent == false &&
                            (a.AccountName.Contains("صندوق") || a.AccountName.Contains("بنك") || a.AccountName.Contains("نقد")))
                .OrderBy(a => a.AccountName)
                .ToList();

            ViewBag.FromAccounts = new SelectList(funds, "AccountId", "AccountName");
            // الحساب المستقبل يمكن أن يكون في أي فرع (للسماح بالتحويل المالي بين الفروع)
            ViewBag.ToAccounts = new SelectList(funds, "AccountId", "AccountName");
        }
    }
}
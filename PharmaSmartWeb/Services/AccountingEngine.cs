using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;

namespace PharmaSmartWeb.Services
{
    public interface IAccountingEngine
    {
        Task<bool> ProcessTransactionAsync(AccountingPayload payload);
    }

    public class AccountingEngine : IAccountingEngine
    {
        private readonly ApplicationDbContext _context;

        public AccountingEngine(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ProcessTransactionAsync(AccountingPayload payload)
        {
            // 1. جلب القالب الخاص بالعملية (Template)
            var template = await _context.AccountingTemplates
                .Include(t => t.Lines)
                .FirstOrDefaultAsync(t => t.TransactionType == payload.TransactionType && t.IsActive);

            if (template == null)
                throw new Exception($"لا يوجد قالب توجيه محاسبي مفعل للعملية: {payload.TransactionType}");

            // 2. إنشاء رأس القيد (Journal Header)
            var journal = new Journalentries
            {
                BranchId = payload.BranchId,
                JournalDate = DateTime.Now,
                CreatedBy = payload.UserId,
                IsPosted = true,
                ReferenceType = payload.TransactionType.ToString(),
                ReferenceNo = payload.ReferenceNo,
                Description = payload.Description
            };

            _context.Journalentries.Add(journal);
            await _context.SaveChangesAsync(); // للحصول على الـ Id

            // 3. معالجة سطور القالب (Engine Processing)
            foreach (var line in template.Lines)
            {
                // 🔥 الـ Condition الذكي: جلب المبلغ، إذا كان 0 نتجاهل السطر!
                payload.Amounts.TryGetValue(line.Source, out decimal rawAmount);
                if (rawAmount <= 0) continue;

                // 🚀 ضبط الدقة العشرية للهللة لمنع تسريب الكسور المحاسبية (Decimal Precision Rule)
                decimal amount = Math.Round(rawAmount, 2, MidpointRounding.AwayFromZero);

                // 🧠 طبقة الـ Resolver (Account Resolver Layer)
                int resolvedAccountId = await ResolveAccountAsync(line.Role, payload);

                // إنشاء سطر القيد (Journal Detail)
                var journalDetail = new Journaldetails
                {
                    JournalId = journal.JournalId,
                    AccountId = resolvedAccountId,
                    Debit = line.IsDebit ? amount : 0,
                    Credit = !line.IsDebit ? amount : 0
                };

                _context.Journaldetails.Add(journalDetail);
            }

            // ⚖️ جدار الحماية الأخير: هل القيد متزن؟
            await _context.SaveChangesAsync();

            var totalDebit = Math.Round(await _context.Journaldetails.Where(j => j.JournalId == journal.JournalId).SumAsync(j => j.Debit), 2);
            var totalCredit = Math.Round(await _context.Journaldetails.Where(j => j.JournalId == journal.JournalId).SumAsync(j => j.Credit), 2);

            if (totalDebit != totalCredit)
                throw new Exception($"خطأ في القالب المحاسبي: القيد غير متزن! (مدين: {totalDebit} | دائن: {totalCredit})");

            return true;
        }

        // =================================================================
        // 🔥 طبقة الـ Resolver (الذكاء في اختيار الحساب) محصنة بتقنية Pattern Matching
        // =================================================================
        private async Task<int> ResolveAccountAsync(AccountRole role, AccountingPayload payload)
        {
            // أولوية 1: السياق المباشر من الـ Payload (Dynamic Context)
            if (role == AccountRole.Cash && payload.SpecificCashAccountId is int cashAcc && cashAcc > 0) return cashAcc;
            if (role == AccountRole.Bank && payload.SpecificBankAccountId is int bankAcc && bankAcc > 0) return bankAcc;

            if (role == AccountRole.Customer && payload.CustomerId is int cId && cId > 0)
            {
                var customer = await _context.Customers.FindAsync(cId);
                // 🚀 التصحيح: استخدام is int لاستخراج الرقم بأمان كـ non-nullable
                if (customer != null && customer.AccountId is int cAccId && cAccId > 0) return cAccId;
                throw new Exception("العميل المختار لا يمتلك حساباً مالياً مربوطاً.");
            }

            if (role == AccountRole.Supplier && payload.SupplierId is int sId && sId > 0)
            {
                var supplier = await _context.Suppliers.FindAsync(sId);
                // 🚀 التصحيح: استخراج الرقم كـ non-nullable لتفادي خطأ CS0266
                if (supplier != null && supplier.AccountId is int sAccId && sAccId > 0) return sAccId;
                throw new Exception("المورد المختار لا يمتلك حساباً مالياً مربوطاً.");
            }

            // أولوية 2: البحث في جدول المابس (AccountMappings) الخاص بالفرع
            var branchMapping = await _context.AccountMappings
                .FirstOrDefaultAsync(m => m.Role == role && m.BranchId == payload.BranchId);

            if (branchMapping != null) return branchMapping.AccountId;

            // أولوية 3: البحث في المابس العامة (التي تطبق على كل الفروع)
            var globalMapping = await _context.AccountMappings
                .FirstOrDefaultAsync(m => m.Role == role && m.BranchId == null);

            if (globalMapping != null) return globalMapping.AccountId;

            // إذا لم يجد شيئاً
            throw new Exception($"طبقة الـ Resolver لم تتمكن من العثور على حساب مالي مناسب للدور المحاسبي: {role}");
        }
    }
}
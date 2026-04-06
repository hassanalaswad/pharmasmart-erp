using System.Collections.Generic;

namespace PharmaSmartWeb.Models
{
    public class AccountingPayload
    {
        public TransactionType TransactionType { get; set; }
        public int BranchId { get; set; }
        public int UserId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty; // رقم الفاتورة
        public string Description { get; set; } = string.Empty; // بيان القيد

        // الـ Context (يستخدم للـ Resolver Layer)
        public int? CustomerId { get; set; }
        public int? SupplierId { get; set; }
        public int? SpecificCashAccountId { get; set; } // إذا اختار الكاشير صندوق محدد بيده
        public int? SpecificBankAccountId { get; set; } // إذا اختار الكاشير بنك محدد بيده

        // 💰 مصادر المبالغ (The Engine will pick from here based on Template Lines)
        public Dictionary<AmountSource, decimal> Amounts { get; set; } = new Dictionary<AmountSource, decimal>();
    }
}
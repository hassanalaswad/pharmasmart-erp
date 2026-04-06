using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Models
    {
        // ==========================================
        // 1. القواميس الثابتة (Enums)
        // ==========================================
        public enum TransactionType
        {
            SalesInvoice,
            SalesReturn,
            PurchaseInvoice,
            PurchaseReturn,
            InventoryAdjustment
        }

        public enum AccountRole
        {
            Cash,           // صندوق
            Bank,           // بنك
            Customer,       // ذمم عملاء
            Supplier,       // ذمم موردين
            SalesRevenue,   // إيراد مبيعات
            PurchaseRevenue,// إيراد مشتريات (بونص)
            COGS,           // تكلفة المبيعات
            Inventory,      // المخزون
            Tax,            // ضريبة
            Discount        // خصم
        }

        public enum AmountSource
        {
            NetTotalAmount, // صافي الفاتورة
            PaidCashAmount, // المدفوع كاش
            PaidBankAmount, // المدفوع بنكي
            CreditAmount,   // المتبقي آجل
            COGSAmount,     // قيمة التكلفة
            TaxAmount,      // قيمة الضريبة
            BonusAmount,     // قيمة البونص
        Discount
    }

        // ==========================================
        // 2. جداول قاعدة البيانات (DB Entities)
        // ==========================================

        // جدول قوالب القيود
        public class AccountingTemplate
        {
            [Key]
            public int TemplateId { get; set; }
            public string TemplateName { get; set; } = string.Empty; // مثال: قالب مبيعات قياسي
            public TransactionType TransactionType { get; set; }
            public bool IsActive { get; set; } = true;

            public virtual ICollection<AccountingTemplateLine> Lines { get; set; } = new HashSet<AccountingTemplateLine>();
        }

        // جدول سطور القالب (القواعد)
        public class AccountingTemplateLine
        {
            [Key]
            public int LineId { get; set; }
            public int TemplateId { get; set; }

            public bool IsDebit { get; set; } // هل هو مدين؟
            public AccountRole Role { get; set; } // الدور (مثال: Cash)
            public AmountSource Source { get; set; } // مصدر الرقم (مثال: PaidCashAmount)

            // 💡 خدعة برمجية ذكية كبديل لـ Condition Expression:
            // السطر ينفذ آلياً "فقط" إذا كانت قيمة المصدر (Source) أكبر من صفر في الـ Payload.
            // هذا يغنينا عن كتابة كود معقد لترجمة النصوص الرياضية.

            // TemplateId is non-nullable (required FK) → use null!
            public virtual AccountingTemplate Template { get; set; } = null!;
        }

        // جدول الربط الديناميكي (Account Resolver Mappings)
        public class AccountMapping
        {
            [Key]
            public int MappingId { get; set; }

            public AccountRole Role { get; set; } // الدور
            public int? BranchId { get; set; } // إذا كان null يعني يطبق على كل الفروع
            public int? PaymentMethodId { get; set; } // يمكن ربطه بطريقة دفع معينة

            public int AccountId { get; set; } // الحساب المالي الفعلي في الدليل
            [ForeignKey("AccountId")]
            // AccountId is non-nullable (required FK) → use null!
            public virtual Accounts Account { get; set; } = null!;
        }
 }


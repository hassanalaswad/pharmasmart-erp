using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("vouchers")]
    public partial class Vouchers
    {
        [Key]
        [Column("VoucherID")]
        public int VoucherId { get; set; }

        [Column("BranchID")]
        public int BranchId { get; set; }

        [Required]
        [StringLength(20)]
        [Column("VoucherType")]
        public string VoucherType { get; set; } = string.Empty; // Receipt (قبض) or Payment (صرف)

        [Column("VoucherDate")]
        public DateTime VoucherDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column("FromAccountID")]
        public int FromAccountId { get; set; } // الصندوق أو البنك المصدر

        [Column("ToAccountID")]
        public int ToAccountId { get; set; } // الحساب المستهدف

        [Column("Description")]
        public string? Description { get; set; }

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        // ==========================================
        // حقول إضافية غير موجودة في قاعدة البيانات حالياً
        // يتم تجاهلها من EF Core بواسطة [NotMapped]
        // ==========================================
        [NotMapped] public string? Notes { get; set; }
        [NotMapped] public string? PayeePayerName { get; set; }
        [NotMapped] public string? PaymentMode { get; set; }
        [NotMapped] public string? ReferenceNo { get; set; }
        [NotMapped] public int? JournalId { get; set; }
        // الأسماء القديمة للتوافق مع الكود الحالي
        [NotMapped] public int MainAccountId => FromAccountId;
        [NotMapped] public int SecondAccountId => ToAccountId;

        // ==========================================
        // العلاقات (Navigation Properties)
        // ==========================================
        [ForeignKey(nameof(BranchId))]
        public virtual Branches Branch { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual Users User { get; set; }

        [ForeignKey(nameof(FromAccountId))]
        public virtual Accounts FromAccount { get; set; }

        [ForeignKey(nameof(ToAccountId))]
        public virtual Accounts ToAccount { get; set; }
    }
}


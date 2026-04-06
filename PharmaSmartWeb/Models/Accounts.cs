using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    /// <summary>
    /// 🏦 موديل الحسابات الاحترافي (ERP Standard)
    /// يدعم شجرة الحسابات، تعدد الفروع، التوجيه المحاسبي التلقائي، وحقول التتبع
    /// </summary>
    [Table("accounts")]
    public partial class Accounts
    {
        public Accounts()
        {
            SubAccounts = new HashSet<Accounts>();
            Journaldetails = new HashSet<Journaldetails>();
            Customers = new HashSet<Customers>();
            Suppliers = new HashSet<Suppliers>();
            SalePayments = new HashSet<SalePayments>();
            BranchAccounts = new HashSet<BranchAccounts>();

            // العلاقات العكسية للفروع
            CashBranches = new HashSet<Branches>();
            SalesBranches = new HashSet<Branches>();
            CogsBranches = new HashSet<Branches>();
            InventoryBranches = new HashSet<Branches>();
            FundtransfersFromAccount = new HashSet<Fundtransfers>();
            FundtransfersToAccount = new HashSet<Fundtransfers>();
        }

        // ==========================================
        // 🔹 الحقول الأساسية
        // ==========================================
        [Key]
        [Column("AccountID", TypeName = "int(11)")]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "مطلوب إدخال كود الحساب")]
        [Column(TypeName = "varchar(50)")]
        public string AccountCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "مطلوب إدخال اسم الحساب")]
        [Column(TypeName = "varchar(150)")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "مطلوب تحديد نوع الحساب")]
        [Column(TypeName = "varchar(50)")]
        public string AccountType { get; set; } = string.Empty; // Asset, Liability, Revenue, Expense

        // optional branch ID (في حال الحساب مرتبط مباشرة بفرع واحد)
        [Column("BranchId", TypeName = "int(11)")]
        public int? BranchId { get; set; }

        // طبيعة الحساب: Debit / Credit
        [Column("AccountNature", TypeName = "tinyint(1)")]
        public bool AccountNature { get; set; }

        // الحساب الأب لشجرة الحسابات
        [Column("ParentAccountID", TypeName = "int(11)")]
        public int? ParentAccountId { get; set; }

        // هل الحساب تجميعي (رئيسي) أم فرعي (ابن)؟
        [Column("IsParent", TypeName = "tinyint(1)")]
        public bool IsParent { get; set; }

        // حالة الحساب
        [Column("IsActive", TypeName = "tinyint(1)")]
        public bool? IsActive { get; set; } = true;

        [Column("IsDeleted", TypeName = "tinyint(1)")]
        public bool? IsDeleted { get; set; }

        // ==========================================
        // 🔹 حقول التتبع (Audit Fields)
        // ==========================================
        [Column("CreatedAt", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; }

        [Column("CreatedBy", TypeName = "int(11)")]
        public int? CreatedBy { get; set; }

        [Column("UpdatedAt", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }

        [Column("UpdatedBy", TypeName = "int(11)")]
        public int? UpdatedBy { get; set; }

        // ==========================================
        // 🔹 الحقل المحسوب
        // ==========================================
        [NotMapped]
        public decimal Balance { get; set; }

        // ==========================================
        // 🌳 العلاقات الشجرية (Self-Referencing)
        // ==========================================
        [ForeignKey(nameof(ParentAccountId))]
        [InverseProperty(nameof(SubAccounts))]
        public virtual Accounts? ParentAccount { get; set; }

        [InverseProperty(nameof(Accounts.ParentAccount))]
        public virtual ICollection<Accounts> SubAccounts { get; set; }

        // ==========================================
        // 🔗 العلاقات التشغيلية
        // ==========================================
        [InverseProperty(nameof(Fundtransfers.FromAccount))]
        public virtual ICollection<Fundtransfers> FundtransfersFromAccount { get; set; }

        [InverseProperty(nameof(Fundtransfers.ToAccount))]
        public virtual ICollection<Fundtransfers> FundtransfersToAccount { get; set; }

        [InverseProperty("Account")]
        public virtual ICollection<Journaldetails> Journaldetails { get; set; }

        [InverseProperty("Account")]
        public virtual ICollection<Customers> Customers { get; set; }

        [InverseProperty("Account")]
        public virtual ICollection<Suppliers> Suppliers { get; set; }

        [InverseProperty("Account")]
        public virtual ICollection<SalePayments> SalePayments { get; set; }

        // ==========================================
        // 🚀 العلاقات العكسية للفروع (Default Accounts)
        // ==========================================
        [InverseProperty(nameof(Branches.DefaultCashAccount))]
        public virtual ICollection<Branches> CashBranches { get; set; }

        [InverseProperty(nameof(Branches.DefaultSalesAccount))]
        public virtual ICollection<Branches> SalesBranches { get; set; }

        [InverseProperty(nameof(Branches.DefaultCOGSAccount))]
        public virtual ICollection<Branches> CogsBranches { get; set; }

        [InverseProperty(nameof(Branches.DefaultInventoryAccount))]
        public virtual ICollection<Branches> InventoryBranches { get; set; }

        // ==========================================
        // 🔗 الربط المتعدد بالفروع
        // ==========================================
        [InverseProperty("Account")]
        public virtual ICollection<BranchAccounts> BranchAccounts { get; set; }

        // ==========================================
        // 🔹 الربط الاختياري بالفرع (للحسابات المباشرة)
        // ==========================================
        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Accounts))]
        public virtual Branches? Branch { get; set; }
    }
}

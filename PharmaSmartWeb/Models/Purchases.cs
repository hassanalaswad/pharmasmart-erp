using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("purchases")]
    public partial class Purchases
    {
        public Purchases()
        {
            Purchasedetails = new HashSet<Purchasedetails>();
        }

        [Key]
        [Column("PurchaseID", TypeName = "int(11)")]
        public int PurchaseId { get; set; }

        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Column("UserID", TypeName = "int(11)")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "رقم فاتورة المورد مطلوب")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب اختيار المورد")]
        public int SupplierId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; } = 0;
        // =========================================================
        // 🛡️ حقول معايير الـ ERP (التتبع والحذف المنطقي)
        // إضافة هذه الحقول ستحل خطأ CS1061 فوراً
        // =========================================================
        [Column(TypeName = "tinyint(1)")]
        public bool? IsDeleted { get; set; }

        //[Column(TypeName = "decimal(18,4)")]
        //public decimal RemainingAmount { get; set; }

        // 🚀 إضافة حقول المرتجع الجديدة
        [Column(TypeName = "tinyint(1)")]
        public bool IsReturn { get; set; } = false;

        public int? ParentPurchaseId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }

        [Column(TypeName = "int(11)")]
        public int? UpdatedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? DeletedAt { get; set; }

        [Column(TypeName = "int(11)")]
        public int? DeletedBy { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";

        public string? Notes { get; set; }

        [StringLength(500)]
        [Display(Name = "صورة الفاتورة المرفقة")]
        public string? InvoiceImagePath { get; set; }

        public decimal AmountPaid { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // --- العلاقات ---
        // BranchId non-nullable FK → null!
        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Purchases))]
        public virtual Branches Branch { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Users.Purchases))]
        // UserId non-nullable FK → null!
        public virtual Users User { get; set; } = null!;

        [ForeignKey(nameof(SupplierId))]
        // SupplierId non-nullable FK → null!
        public virtual Suppliers Supplier { get; set; } = null!;

        [InverseProperty("Purchase")]
        public virtual ICollection<Purchasedetails> Purchasedetails { get; set; }
    }
}

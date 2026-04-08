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

        [Required(ErrorMessage = "╪▒┘é┘à ┘╪د╪ز┘ê╪▒╪ر ╪د┘┘à┘ê╪▒╪» ┘à╪╖┘┘ê╪ذ")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "┘è╪ش╪ذ ╪د╪«╪ز┘è╪د╪▒ ╪د┘┘à┘ê╪▒╪»")]
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
        // ≡اؤةي╕ ╪ص┘é┘ê┘ ┘à╪╣╪د┘è┘è╪▒ ╪د┘┘ ERP (╪د┘╪ز╪ز╪ذ╪╣ ┘ê╪د┘╪ص╪░┘ ╪د┘┘à┘╪╖┘é┘è)
        // ╪ح╪╢╪د┘╪ر ┘ç╪░┘ç ╪د┘╪ص┘é┘ê┘ ╪│╪ز╪ص┘ ╪«╪╖╪ث CS1061 ┘┘ê╪▒╪د┘ï
        // =========================================================
        [Column(TypeName = "tinyint(1)")]
        public bool? IsDeleted { get; set; }

        //[Column(TypeName = "decimal(18,4)")]
        //public decimal RemainingAmount { get; set; }

        // ≡اأ ╪ح╪╢╪د┘╪ر ╪ص┘é┘ê┘ ╪د┘┘à╪▒╪ز╪ش╪╣ ╪د┘╪ش╪»┘è╪»╪ر
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
        [Display(Name = "╪╡┘ê╪▒╪ر ╪د┘┘╪د╪ز┘ê╪▒╪ر ╪د┘┘à╪▒┘┘é╪ر")]
        public string? InvoiceImagePath { get; set; }

        public decimal AmountPaid { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        // --- ╪د┘╪╣┘╪د┘é╪د╪ز ---
        // BranchId non-nullable FK ظْ null!
        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Purchases))]
        public virtual Branches Branch { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Users.Purchases))]
        // UserId non-nullable FK ظْ null!
        public virtual Users User { get; set; } = null!;

        [ForeignKey(nameof(SupplierId))]
        // SupplierId non-nullable FK ظْ null!
        public virtual Suppliers Supplier { get; set; } = null!;

        [InverseProperty("Purchase")]
        public virtual ICollection<Purchasedetails> Purchasedetails { get; set; }
    }
}

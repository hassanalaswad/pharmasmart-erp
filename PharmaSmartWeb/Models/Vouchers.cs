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
        public string VoucherType { get; set; } = string.Empty; // Receipt (┘é╪ذ╪╢) or Payment (╪╡╪▒┘)

        [Column("VoucherDate")]
        public DateTime VoucherDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column("FromAccountID")]
        public int FromAccountId { get; set; } // ╪د┘╪╡┘╪»┘ê┘é ╪ث┘ê ╪د┘╪ذ┘┘â ╪د┘┘à╪╡╪»╪▒

        [Column("ToAccountID")]
        public int ToAccountId { get; set; } // ╪د┘╪ص╪│╪د╪ذ ╪د┘┘à╪│╪ز┘ç╪»┘

        [Column("Description")]
        public string? Description { get; set; }

        [Column("CreatedBy")]
        public int CreatedBy { get; set; }

        // ==========================================
        // ╪ص┘é┘ê┘ ╪ح╪╢╪د┘┘è╪ر ╪║┘è╪▒ ┘à┘ê╪ش┘ê╪»╪ر ┘┘è ┘é╪د╪╣╪»╪ر ╪د┘╪ذ┘è╪د┘╪د╪ز ╪ص╪د┘┘è╪د┘ï
        // ┘è╪ز┘à ╪ز╪ش╪د┘ç┘┘ç╪د ┘à┘ EF Core ╪ذ┘ê╪د╪│╪╖╪ر [NotMapped]
        // ==========================================
        [NotMapped] public string? Notes { get; set; }
        [NotMapped] public string? PayeePayerName { get; set; }
        [NotMapped] public string? PaymentMode { get; set; }
        [NotMapped] public string? ReferenceNo { get; set; }
        [NotMapped] public int? JournalId { get; set; }
        // ╪د┘╪ث╪│┘à╪د╪ة ╪د┘┘é╪»┘è┘à╪ر ┘┘╪ز┘ê╪د┘┘é ┘à╪╣ ╪د┘┘â┘ê╪» ╪د┘╪ص╪د┘┘è
        [NotMapped] public int MainAccountId => FromAccountId;
        [NotMapped] public int SecondAccountId => ToAccountId;

        // ==========================================
        // ╪د┘╪╣┘╪د┘é╪د╪ز (Navigation Properties)
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


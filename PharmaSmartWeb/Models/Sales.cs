using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("sales")]
    public partial class Sales
    {
        public Sales()
        {
            Saledetails = new HashSet<Saledetails>();
            SalePayments = new HashSet<SalePayments>(); // ≡اأ ╪ز┘╪╣┘è┘ ╪╣┘╪د┘é╪ر ╪د┘╪»┘╪╣ ╪د┘┘à╪ز╪╣╪»╪»
        }

        [Key]
        [Column("SaleID", TypeName = "int(11)")]
        public int SaleId { get; set; }

        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Column("UserID", TypeName = "int(11)")]
        public int UserId { get; set; }

        [Column("CustomerID", TypeName = "int(11)")]
        public int? CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; } = 0;

        [ConcurrencyCheck]
        [Column(TypeName = "tinyint(1)")]
        public bool IsReturn { get; set; } = false;

        public int? ParentSaleId { get; set; }

        // --- ≡اؤةي╕ ╪ص┘é┘ê┘ ┘à╪╣╪د┘è┘è╪▒ ╪د┘┘ ERP (╪د┘╪ز╪ز╪ذ╪╣ ┘ê╪د┘╪ص╪░┘ ╪د┘┘à┘╪╖┘é┘è) ---
        public bool? IsDeleted { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }


        // --- ╪د┘╪╣┘╪د┘é╪د╪ز (Navigation Properties) ---

        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Sales))]
        public virtual Branches Branch { get; set; }

        [ForeignKey(nameof(CustomerId))]
        [InverseProperty(nameof(Customers.Sales))]
        public virtual Customers Customer { get; set; }

        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Users.Sales))]
        public virtual Users User { get; set; }

        [InverseProperty("Sale")]
        public virtual ICollection<Saledetails> Saledetails { get; set; }

        // ≡اأ ╪د┘╪╣┘╪د┘é╪ر ╪د┘╪ش╪»┘è╪»╪ر ┘à╪╣ ╪ش╪»┘ê┘ ╪د┘┘à╪»┘┘ê╪╣╪د╪ز ╪د┘┘à╪│╪ز┘é┘
        [InverseProperty("Sale")]
        public virtual ICollection<SalePayments> SalePayments { get; set; }
    }
}

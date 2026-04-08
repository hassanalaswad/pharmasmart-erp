using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("customers")]
    public partial class Customers
    {
        public Customers()
        {
            // ≡اأ ┘ç╪░┘ç ╪د┘╪ح╪╢╪د┘╪ر ┘ç┘è ╪د┘╪ز┘è ╪│╪ز╪ص┘ ╪«╪╖╪ث ┘à┘┘ Sales ╪ز┘à╪د┘à╪د┘ï
            Sales = new HashSet<Sales>();
        }

        [Key]
        [Column("CustomerID", TypeName = "int(11)")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "╪د╪│┘à ╪د┘╪╣┘à┘è┘ ┘à╪╖┘┘ê╪ذ")]
        [Column("FullName", TypeName = "varchar(150)")]
        public string FullName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(50)")]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CreditLimit { get; set; }

        [Column("AccountID", TypeName = "int(11)")]
        public int AccountId { get; set; }

        [Required]
        public bool? IsActive { get; set; }
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        // BranchId is non-nullable (required FK) ظْ EF Core guarantees initialization ظْ use null!
        public virtual Branches Branch { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }

        [ForeignKey(nameof(AccountId))]
        [InverseProperty(nameof(Accounts.Customers))]
        // AccountId is non-nullable (required FK) ظْ EF Core guarantees initialization ظْ use null!
        public virtual Accounts Account { get; set; } = null!;

        // ≡اأ ╪ز╪╣╪▒┘è┘ ╪د┘╪╣┘╪د┘é╪ر ╪د┘╪╣┘â╪│┘è╪ر ┘à╪╣ ╪د┘┘à╪ذ┘è╪╣╪د╪ز
        [InverseProperty("Customer")]
        public virtual ICollection<Sales> Sales { get; set; }
    }
}

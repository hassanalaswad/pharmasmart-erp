using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("suppliers")]
    public partial class Suppliers
    {
        [Key]
        [Column("SupplierId")]
        public int SupplierId { get; set; }
        // ≡اأ ╪د┘╪ح╪╢╪د┘╪ر ╪د┘╪ش┘ê┘ç╪▒┘è╪ر: ╪ص┘é┘ ╪د┘┘╪▒╪╣ ┘╪ز╪╖╪ذ┘è┘é ╪د┘╪╣╪▓┘ ╪د┘┘à╪د┘┘è ┘ê╪د┘┘à┘â╪د┘┘è
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        
        [Required(ErrorMessage = "╪د╪│┘à ╪د┘┘à┘ê╪▒╪» ┘à╪╖┘┘ê╪ذ")]
        [StringLength(150)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        public int? AccountId { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        // AccountId is nullable (int?) ظْ navigation must also be nullable
        [ForeignKey("AccountId")]
        public virtual Accounts? Account { get; set; }
    }
}

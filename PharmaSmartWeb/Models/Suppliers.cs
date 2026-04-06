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
        // 🚀 الإضافة الجوهرية: حقل الفرع لتطبيق العزل المالي والمكاني
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        
        [Required(ErrorMessage = "اسم المورد مطلوب")]
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

        // AccountId is nullable (int?) → navigation must also be nullable
        [ForeignKey("AccountId")]
        public virtual Accounts? Account { get; set; }
    }
}
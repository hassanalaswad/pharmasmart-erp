//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace PharmaSmartWeb.Models
//{
//    [Table("barcodegenerator")]
//    public partial class BarcodeGenerator
//    {
//        [Key]
//        [Column("Id", TypeName = "int(11)")]
//        public int Id { get; set; }

//        // 🚀 الحقل الجديد لضمان العزل
//        [Column("BranchId", TypeName = "int(11)")]
//        public int BranchId { get; set; }

//        [Column("DrugId", TypeName = "int(11)")]
//        public int DrugId { get; set; }

//        [Required]
//        [StringLength(50)]
//        [Column("BatchNumber", TypeName = "varchar(50)")]
//        public string BatchNumber { get; set; }

//        [Column("ExpiryDate", TypeName = "date")]
//        public DateTime ExpiryDate { get; set; }

//        [Column("CurrentPrice", TypeName = "decimal(18,4)")]
//        public decimal CurrentPrice { get; set; }

//        [Column("QuantityToPrint", TypeName = "int(11)")]
//        public int QuantityToPrint { get; set; }

//        [Required]
//        [StringLength(255)]
//        [Column("GeneratedCode", TypeName = "varchar(255)")]
//        public string GeneratedCode { get; set; }

//        [Column("IsPrinted")]
//        public bool IsPrinted { get; set; }

//        [Column("CreatedAt", TypeName = "datetime")]
//        public DateTime CreatedAt { get; set; }

//        [Column("UserId", TypeName = "int(11)")]
//        public int UserId { get; set; }

//        [ForeignKey(nameof(BranchId))]
//        public virtual Branches Branch { get; set; }

//        [ForeignKey(nameof(DrugId))]
//        public virtual Drugs Drug { get; set; }
//    }
//}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("barcodegenerator")]
    public partial class BarcodeGenerator
    {
        [Key]
        [Column("Id", TypeName = "int(11)")]
        public int Id { get; set; }

        [Column("BranchId", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Column("DrugId", TypeName = "int(11)")]
        public int DrugId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("BatchNumber", TypeName = "varchar(50)")]
        public string BatchNumber { get; set; } = string.Empty;

        [Column("ExpiryDate", TypeName = "date")]
        public DateTime ExpiryDate { get; set; }

        [Column("CurrentPrice", TypeName = "decimal(18,4)")]
        public decimal CurrentPrice { get; set; }

        [Column("QuantityToPrint", TypeName = "int(11)")]
        public int QuantityToPrint { get; set; }

        [Required]
        [StringLength(255)]
        [Column("GeneratedCode", TypeName = "varchar(255)")]
        public string GeneratedCode { get; set; } = string.Empty;

        [Column("IsPrinted")]
        public bool IsPrinted { get; set; }

        [Column("CreatedAt", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column("UserId", TypeName = "int(11)")]
        public int UserId { get; set; }

        // 🔗 العلاقات
        // BranchId is non-nullable → null!
        public virtual Branches Branch { get; set; } = null!;

        [ForeignKey(nameof(DrugId))]
        // DrugId is non-nullable → null!
        public virtual Drugs Drug { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        // UserId is non-nullable → null!
        public virtual Users User { get; set; } = null!;
    }
}
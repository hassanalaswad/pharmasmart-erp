using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("branchinventory")]
    public partial class Branchinventory
    {
        [Key]
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Key]
        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }

        // 🚀 إضافة هذا الحقل لحل مشكلة الرفوف
        public int? ShelfId { get; set; }

        [ConcurrencyCheck]
        [Column(TypeName = "int(11)")]
        public int StockQuantity { get; set; }

        [Column(TypeName = "int(11)")]
        public int MinimumStockLevel { get; set; }

        [Column("ABCCategory", TypeName = "char(1)")]
        public string? Abccategory { get; set; }
        // 🚀 الحقول المحاسبية الجديدة لمعايير الـ ERP
        [Column(TypeName = "decimal(18,4)")]
        public decimal? AverageCost { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? CurrentSellingPrice { get; set; }

        // BranchId is non-nullable (required FK) → use null!
        public virtual Branches Branch { get; set; } = null!;

        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Branchinventory))]
        // DrugId is non-nullable (required FK) → use null!
        public virtual Drugs Drug { get; set; } = null!;

        // 🔗 الربط العكسي مع الرفوف (ضروري جداً لحل الخطأ)
        //[ForeignKey(nameof(ShelfId))]
        //[InverseProperty(nameof(Shelves.Branchinventory))]
        //public virtual Shelves Shelf { get; set; }
    }
}
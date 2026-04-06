using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("shelves")]
    public partial class Shelves
    {
        [Key]
        [Column("ShelfId", TypeName = "int(11)")]
        public int ShelfId { get; set; }

        [Column("WarehouseId", TypeName = "int(11)")]
        public int WarehouseId { get; set; }

        // ?? ÇáÍŞá ÇáÌÏíÏ: ÑÈØ ÇáÑİ ÈÇáãÌãæÚÉ ÇáÚáÇÌíÉ
        [Column("GroupId", TypeName = "int(11)")]
        public int? GroupId { get; set; }

        [Required(ErrorMessage = "ÇÓã Ãæ ÑŞã ÇáÑİ ãØáæÈ")]
        [StringLength(100)]
        [Column("ShelfName", TypeName = "varchar(100)")]
        public string ShelfName { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("Notes", TypeName = "varchar(255)")]
        public string? Notes { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        [InverseProperty(nameof(Models.Warehouses.Shelves))]
        public virtual Warehouses Warehouse { get; set; }

        // ?? ÚáÇŞÉ ÇáÑÈØ ãÚ ÌÏæá ÇáãÌãæÚÇÊ ÇáÏæÇÆíÉ
        [ForeignKey(nameof(GroupId))]
        public virtual ItemGroups ItemGroup { get; set; }

    }
}


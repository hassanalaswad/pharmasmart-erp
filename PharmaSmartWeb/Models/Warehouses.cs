using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("warehouses")]
    public partial class Warehouses
    {
        public Warehouses()
        {
            Shelves = new HashSet<Shelves>();
        }

        [Key]
        [Column("WarehouseId", TypeName = "int(11)")]
        public int WarehouseId { get; set; }

        [Column("BranchId", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "«”„ «·„” Êœ⁄ „ÿ·Ê»")]
        [StringLength(150)]
        [Column("WarehouseName", TypeName = "varchar(150)")]
        public string WarehouseName { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("Location", TypeName = "varchar(255)")]
        public string? Location { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branches Branch { get; set; }

        [InverseProperty(nameof(Models.Shelves.Warehouse))]
        public virtual ICollection<Shelves> Shelves { get; set; }
    }
}


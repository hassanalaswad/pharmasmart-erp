using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("itemgroups")]
    public partial class ItemGroups
    {
        public ItemGroups()
        {
            Drugs = new HashSet<Drugs>();
            Shelves = new HashSet<Shelves>();
        }

        [Key]
        [Column("GroupId", TypeName = "int(11)")]
        public int GroupId { get; set; }

        [StringLength(50)]
        [Column("GroupCode", TypeName = "varchar(50)")]
        public string? GroupCode { get; set; }

        [Required(ErrorMessage = "اسم المجموعة مطلوب")]
        [StringLength(100)]
        [Column("GroupName", TypeName = "varchar(100)")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("Description", TypeName = "varchar(255)")]
        public string? Description { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }

        [InverseProperty(nameof(Models.Drugs.ItemGroup))]
        public virtual ICollection<Drugs> Drugs { get; set; }

        [InverseProperty(nameof(Models.Shelves.ItemGroup))]
        public virtual ICollection<Shelves> Shelves { get; set; }
    }
}
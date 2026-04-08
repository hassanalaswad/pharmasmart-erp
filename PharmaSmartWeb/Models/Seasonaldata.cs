using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("seasonaldata")]
    public partial class Seasonaldata
    {
        [Key]
        [Column("SeasonalID", TypeName = "int(11)")]
        public int SeasonalId { get; set; }
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }
        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string SeasonName { get; set; } = string.Empty;
        [Column(TypeName = "int(11)")]
        public int Year { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal SeasonalFactor { get; set; }

        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Seasonaldata))]
        public virtual Branches Branch { get; set; }
        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Seasonaldata))]
        public virtual Drugs Drug { get; set; }
    }
}



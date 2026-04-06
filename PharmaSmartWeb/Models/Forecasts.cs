using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("forecasts")]
    public partial class Forecasts
    {
        [Key]
        [Column("ForecastID", TypeName = "int(11)")]
        public int ForecastId { get; set; }
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }
        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }
        [Column(TypeName = "date")]
        public DateTime ForecastDate { get; set; }
        [Column(TypeName = "int(11)")]
        public int PredictedDemand { get; set; }

        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Forecasts))]
        public virtual Branches Branch { get; set; }
        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Forecasts))]
        public virtual Drugs Drug { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("purchaseplandetails")]
    public class PurchasePlanDetail
    {
        [Key]
        [Column("DetailId", TypeName = "int(11)")]
        public int DetailId { get; set; }

        [Column("PlanId", TypeName = "int(11)")]
        public int PlanId { get; set; }

        [Column("DrugId", TypeName = "int(11)")]
        public int DrugId { get; set; }

        public int CurrentStock { get; set; } = 0;
        
        [StringLength(10)]
        public string? ABCCategory { get; set; }

        public decimal ForecastedDemand { get; set; } = 0;
        
        public decimal ForecastAccuracy { get; set; } = 0; // Model accuracy mapping e.g. 95.5

        public int ProposedQuantity { get; set; } = 0; // EOQ output

        public int ApprovedQuantity { get; set; } = 0; // The actual quantity approved by user

        public decimal UnitCostEstimate { get; set; } = 0;

        public decimal TotalCost { get; set; } = 0;

        public bool IsLifeSaving { get; set; } = false;

        [StringLength(100)]
        public string? Status { get; set; } = "Pending"; // Within budget vs Deferred

        [ForeignKey(nameof(PlanId))]
        public virtual PurchasePlan PurchasePlan { get; set; }

        [ForeignKey(nameof(DrugId))]
        public virtual Drugs Drug { get; set; }
    }
}


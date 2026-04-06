using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("purchaseplans")]
    public class PurchasePlan
    {
        [Key]
        [Column("PlanId", TypeName = "int(11)")]
        public int PlanId { get; set; }

        [Column("BranchId", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Column("CreatedBy", TypeName = "int(11)")]
        public int CreatedBy { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime PlanDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? Status { get; set; } = "Draft"; // Draft, Approved, Executed

        [StringLength(500)]
        public string? Notes { get; set; }

        public decimal EstimatedTotalCost { get; set; } = 0;

        [ForeignKey(nameof(BranchId))]
        public virtual Branches Branch { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual Users User { get; set; }

        public virtual ICollection<PurchasePlanDetail> PlanDetails { get; set; } = new HashSet<PurchasePlanDetail>();
    }
}


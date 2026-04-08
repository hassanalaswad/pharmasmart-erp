using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("stockaudits")]
    public partial class Stockaudits
    {
        public Stockaudits()
        {
            Stockauditdetails = new HashSet<Stockauditdetails>();
        }

        [Key]
        public int AuditId { get; set; }

        public int BranchId { get; set; }

        public DateTime AuditDate { get; set; }

        public int UserId { get; set; }

        public string? Notes { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        // BranchId non-nullable FK → null!
        [ForeignKey("BranchId")]
        public virtual Branches Branch { get; set; } = null!;

        [ForeignKey("UserId")]
        // UserId non-nullable FK → null!
        public virtual Users User { get; set; } = null!;

        public virtual ICollection<Stockauditdetails> Stockauditdetails { get; set; }
    }

    public class StockAuditDetailViewModel
    {
        public int DrugId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int SystemQty { get; set; }
        public int PhysicalQty { get; set; }
        public decimal UnitCost { get; set; }
    }
}
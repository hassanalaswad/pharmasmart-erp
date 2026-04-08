using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("stockauditdetails")]
    public partial class Stockauditdetails
    {
        [Key]
        public int DetailId { get; set; }

        public int AuditId { get; set; }

        public int DrugId { get; set; }

        public int SystemQty { get; set; }

        public int PhysicalQty { get; set; }

        public int Difference { get; set; }

        public decimal UnitCost { get; set; }

        [ForeignKey("AuditId")]
        public virtual Stockaudits Audit { get; set; }

        [ForeignKey("DrugId")]
        public virtual Drugs Drug { get; set; }
    }
}

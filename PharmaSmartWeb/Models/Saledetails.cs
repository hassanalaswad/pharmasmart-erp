using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("saledetails")]
    public partial class Saledetails
    {
        [Key]
        [Column("SaleDetailID", TypeName = "int(11)")]
        public int SaleDetailId { get; set; }
        [Column("SaleID", TypeName = "int(11)")]
        public int SaleId { get; set; }
        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }
        [Column(TypeName = "int(11)")]
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Saledetails))]
        public virtual Drugs Drug { get; set; }
        [ForeignKey(nameof(SaleId))]
        [InverseProperty(nameof(Sales.Saledetails))]
        public virtual Sales Sale { get; set; }
    }
}

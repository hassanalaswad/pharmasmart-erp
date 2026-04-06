using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("purchasedetails")]
    public partial class Purchasedetails
    {
        [Key]
        [Column("DetailID", TypeName = "int(11)")]
        public int DetailId { get; set; }

        [Column("PurchaseID", TypeName = "int(11)")]
        public int PurchaseId { get; set; }

        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }

        [Column(TypeName = "int(11)")]
        public int Quantity { get; set; }

        // --- «·Õﬁ· «·√’·Ì „‰ ﬁ«⁄œ… »Ì«‰« ﬂ ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }
        public int RemainingQuantity { get; set; }

        // --- «·ÕﬁÊ· «·ÃœÌœ… (·‹ ERP) ---
        public int BonusQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }

        public DateTime ExpiryDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        // --- «·⁄·«ﬁ«  (Navigation Properties) ---
        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Purchasedetails))]
        public virtual Drugs Drug { get; set; }

        [ForeignKey(nameof(PurchaseId))]
        [InverseProperty(nameof(Purchases.Purchasedetails))]
        public virtual Purchases Purchase { get; set; }
    }
}


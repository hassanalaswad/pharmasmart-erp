using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("drug_batches")]
    public partial class DrugBatches
    {
        [Key]
        [Column("BatchId", TypeName = "int(11)")]
        public int BatchId { get; set; }

        [Column("DrugId", TypeName = "int(11)")]
        public int DrugId { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string BatchNumber { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime? ProductionDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime ExpiryDate { get; set; }

        [ForeignKey(nameof(DrugId))]
        public virtual Drugs Drug { get; set; }
    }
}


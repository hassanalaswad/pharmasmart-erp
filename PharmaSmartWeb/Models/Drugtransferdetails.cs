using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("drugtransferdetails")]
    public partial class Drugtransferdetails
    {
        [Key]
        [Column("DetailID", TypeName = "int(11)")]
        public int DetailId { get; set; }

        [Column("TransferID", TypeName = "int(11)")]
        public int TransferId { get; set; }

        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }

        [Column(TypeName = "int(11)")]
        public int Quantity { get; set; }

        // 🚀 الحقل الجديد: حفظ التكلفة وقت التحويل
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Drugtransferdetails))]
        public virtual Drugs Drug { get; set; }

        [ForeignKey(nameof(TransferId))]
        [InverseProperty(nameof(Drugtransfers.Drugtransferdetails))]
        public virtual Drugtransfers Transfer { get; set; }
    }
}
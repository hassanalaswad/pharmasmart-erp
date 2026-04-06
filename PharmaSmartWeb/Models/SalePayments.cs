using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("sale_payments")]
    public partial class SalePayments
    {
        [Key]
        [Column("PaymentId", TypeName = "int(11)")]
        public int PaymentId { get; set; }

        [Column("SaleId", TypeName = "int(11)")]
        public int SaleId { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Column("AccountId", TypeName = "int(11)")]
        public int? AccountId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Amount { get; set; }

        [ForeignKey(nameof(SaleId))]
        [InverseProperty(nameof(Sales.SalePayments))]
        public virtual Sales Sale { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual Accounts Account { get; set; }
    }
}


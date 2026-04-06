using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("currencies")]
    public partial class Currencies
    {
        public Currencies()
        {
            Branches = new HashSet<Branches>();
        }

        [Key]
        [Column("CurrencyId", TypeName = "int(11)")]
        public int CurrencyId { get; set; }

        [Required]
        [Column(TypeName = "varchar(10)")]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string CurrencyName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal ExchangeRate { get; set; }

        public bool IsBaseCurrency { get; set; }

        public bool IsActive { get; set; }

        [InverseProperty("DefaultCurrency")]
        public virtual ICollection<Branches> Branches { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("journaldetails")]
    public partial class Journaldetails
    {
        [Key]
        [Column("DetailID", TypeName = "int(11)")]
        public int DetailId { get; set; }
        [Column("JournalID", TypeName = "int(11)")]
        public int JournalId { get; set; }
        [Column("AccountID", TypeName = "int(11)")]
        public int AccountId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Debit { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Credit { get; set; }

        [ForeignKey(nameof(AccountId))]
        [InverseProperty(nameof(Accounts.Journaldetails))]
        public virtual Accounts Account { get; set; }
        [ForeignKey(nameof(JournalId))]
        [InverseProperty(nameof(Journalentries.Journaldetails))]
        public virtual Journalentries Journal { get; set; }
    }
}

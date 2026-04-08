using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("fundtransfers")]
    public partial class Fundtransfers
    {
        [Key]
        [Column("TransferID", TypeName = "int(11)")]
        public int TransferId { get; set; }

        // ?? ╟ط═▐ط ╟ط╠╧و╧ طط┌╥ط ╟طع╟طو
        [Column("BranchId", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Column("FromAccountID", TypeName = "int(11)")]
        public int FromAccountId { get; set; }

        [Column("ToAccountID", TypeName = "int(11)")]
        public int ToAccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime TransferDate { get; set; }

        // ?? ═▐ط ╟طع╤╠┌ ╟ط╚غ▀و
        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? ReferenceNo { get; set; }

        [StringLength(250)]
        [Column(TypeName = "varchar(250)")]
        public string? Notes { get; set; }

        [Column("CreatedBy", TypeName = "int(11)")]
        public int CreatedBy { get; set; }

        // ?? ═▐ط ╤╚╪ ╟ط▐و╧ ╟طع═╟╙╚و ╟طع╥╧µ╠
        [Column("JournalId", TypeName = "int(11)")]
        public int? JournalId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public virtual Branches Branch { get; set; }

        [ForeignKey(nameof(FromAccountId))]
        [InverseProperty(nameof(Accounts.FundtransfersFromAccount))]
        public virtual Accounts FromAccount { get; set; }

        [ForeignKey(nameof(ToAccountId))]
        [InverseProperty(nameof(Accounts.FundtransfersToAccount))]
        public virtual Accounts ToAccount { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual Users User { get; set; }

        [ForeignKey(nameof(JournalId))]
        public virtual Journalentries Journal { get; set; }

    }
}

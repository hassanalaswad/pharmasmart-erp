using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("drugtransfers")]
    public partial class Drugtransfers
    {
        public Drugtransfers()
        {
            Drugtransferdetails = new HashSet<Drugtransferdetails>();
        }

        [Key]
        [Column("TransferID", TypeName = "int(11)")]
        public int TransferId { get; set; }

        [Column("FromBranchID", TypeName = "int(11)")]
        public int FromBranchId { get; set; }

        [Column("ToBranchID", TypeName = "int(11)")]
        public int ToBranchId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime TransferDate { get; set; }

        // ?? «·Õﬁ· «·ÃœÌœ:  «—ÌŒ «·«” ·«„
        [Column(TypeName = "datetime")]
        public DateTime? ReceiveDate { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = string.Empty;

        [Column(TypeName = "int(11)")]
        public int CreatedBy { get; set; }

        // ?? «·Õﬁ· «·ÃœÌœ: «·„” Œœ„ «·„” ·„
        [Column(TypeName = "int(11)")]
        public int? ReceivedBy { get; set; }

        [Column(TypeName = "varchar(250)")]
        public string? Notes { get; set; }

        // ?? «·ﬁÌÊœ «·„Õ«”»Ì… ·· ÕÊÌ· «·„“œÊÃ
        [Column(TypeName = "int(11)")]
        public int? JournalId { get; set; }

        [Column(TypeName = "int(11)")]
        public int? ReceiptJournalId { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        [InverseProperty(nameof(Users.Drugtransfers))]
        public virtual Users CreatedByNavigation { get; set; }

        [ForeignKey(nameof(ReceivedBy))]
        public virtual Users ReceivedByNavigation { get; set; }

        [ForeignKey(nameof(FromBranchId))]
        [InverseProperty(nameof(Branches.DrugtransfersFromBranch))]
        public virtual Branches FromBranch { get; set; }

        [ForeignKey(nameof(ToBranchId))]
        [InverseProperty(nameof(Branches.DrugtransfersToBranch))]
        public virtual Branches ToBranch { get; set; }

        [InverseProperty("Transfer")]
        public virtual ICollection<Drugtransferdetails> Drugtransferdetails { get; set; }
    }
}


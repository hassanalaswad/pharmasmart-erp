using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("journalentries")]
    public partial class Journalentries
    {
        public Journalentries()
        {
            Journaldetails = new HashSet<Journaldetails>();
        }

        [Key]
        [Column("JournalID", TypeName = "int(11)")]
        public int JournalId { get; set; }
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime JournalDate { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "varchar(50)")]
        public string? ReferenceType { get; set; }
        public bool IsPosted { get; set; }
        [Column(TypeName = "int(11)")]
        public int CreatedBy { get; set; }

        // BranchId non-nullable FK ظْ null!
        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Journalentries))]
        public virtual Branches Branch { get; set; } = null!;
        [ForeignKey(nameof(CreatedBy))]
        [InverseProperty(nameof(Users.Journalentries))]
        // CreatedBy non-nullable FK ظْ null!
        public virtual Users CreatedByNavigation { get; set; } = null!;
        [InverseProperty("Journal")]
        
        [StringLength(100)]
        public string? ReferenceNo { get; set; } // ╪▒┘é┘à ╪د┘╪┤┘è┘â ╪ث┘ê ╪د┘╪ص┘ê╪د┘╪ر

        [NotMapped]
        [StringLength(200)]
        public string? PayeePayerName { get; set; } // ╪د╪│┘à ╪د┘╪┤╪«╪╡ ╪د┘┘à╪│╪ز┘┘à ╪ث┘ê ╪د┘┘à╪│┘┘à
        public virtual ICollection<Journaldetails> Journaldetails { get; set; }
    }
}

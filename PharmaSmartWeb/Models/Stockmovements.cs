using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("stockmovements")]
    public partial class Stockmovements
    {
        [Key]
        [Column("MovementID", TypeName = "int(11)")]
        public int MovementId { get; set; }
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }
        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime MovementDate { get; set; }
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string MovementType { get; set; } = string.Empty;
        [Column(TypeName = "int(11)")]
        public int Quantity { get; set; }
        [Column("ReferenceID", TypeName = "int(11)")]
        public int? ReferenceId { get; set; }
        [Column("UserID", TypeName = "int(11)")]
        public int UserId { get; set; }
        [Column(TypeName = "varchar(250)")]
        public string? Notes { get; set; }

        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Stockmovements))]
        public virtual Branches Branch { get; set; }
        [ForeignKey(nameof(DrugId))]
        [InverseProperty(nameof(Drugs.Stockmovements))]
        public virtual Drugs Drug { get; set; }
        [ForeignKey(nameof(UserId))]
        [InverseProperty(nameof(Users.Stockmovements))]
        public virtual Users User { get; set; }
    }
}



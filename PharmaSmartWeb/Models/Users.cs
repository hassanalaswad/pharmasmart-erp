//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace PharmaSmartWeb.Models
//{
//    [Table("users")]
//    public partial class Users
//    {
//        public Users()
//        {
//            Drugtransfers = new HashSet<Drugtransfers>();
//            Fundtransfers = new HashSet<Fundtransfers>();
//            Journalentries = new HashSet<Journalentries>();
//            Purchases = new HashSet<Purchases>();
//            Sales = new HashSet<Sales>();
//            Stockmovements = new HashSet<Stockmovements>();
//            Stockaudits = new HashSet<Stockaudits>();
//            Systemlogs = new HashSet<SystemLogs>();
//            BarcodeGenerator = new HashSet<BarcodeGenerator>();
//        }

//        [Key]
//        [Column("UserID", TypeName = "int(11)")]
//        public int UserId { get; set; }

//        [Required]
//        [Column(TypeName = "varchar(100)")]
//        [StringLength(100)]
//        public string? Username { get; set; }

//        [Required]
//        [Column(TypeName = "varchar(200)")]
//        [StringLength(200)]
//        public string? PasswordHash { get; set; }

//        [Column("RoleID", TypeName = "int(11)")]
//        public int RoleId { get; set; }

//        [Column("EmployeeID", TypeName = "int(11)")]
//        public int? EmployeeId { get; set; }

//        // ?? ╟ط┼╒ط╟═ ╟ط╠╨╤و: ╩═µوط ╟طغµ┌ ط▄ int? µ╩╦╚و╩ ╟ط╟╙ع ╟ط╚╤ع╠و
//        [Column("DefaultBranchID", TypeName = "int(11)")]
//        public int? DefaultBranchId { get; set; }

//        [Required]
//        public bool? IsActive { get; set; }

//        // ?? ┼╠╚╟╤ ╟طغ┘╟ع ┌طه ╟╙╩╬╧╟ع ╟ط┌عµ╧ ╟طع═╧╧ ╒╤╟═╔≡
//        [ForeignKey(nameof(DefaultBranchId))]
//        [InverseProperty(nameof(Branches.Users))]
//        public virtual Branches DefaultBranch { get; set; }

//        [ForeignKey(nameof(EmployeeId))]
//        [InverseProperty(nameof(Employees.Users))]
//        public virtual Employees Employee { get; set; }

//        [ForeignKey(nameof(RoleId))]
//        [InverseProperty(nameof(Userroles.Users))]
//        public virtual Userroles Role { get; set; }

//        // ==============================================================
//        // ?? ╟ط┌ط╟▐╟╩ ╟طعµ╠°ف╔ ╒╤╟═╔≡ (Explicit Inverse Properties)
//        // ==============================================================

//        [InverseProperty("CreatedByNavigation")]
//        public virtual ICollection<Drugtransfers> Drugtransfers { get; set; }

//        [InverseProperty("CreatedByNavigation")]
//        public virtual ICollection<Fundtransfers> Fundtransfers { get; set; }

//        [InverseProperty("CreatedByNavigation")]
//        public virtual ICollection<Journalentries> Journalentries { get; set; }

//        [InverseProperty("User")]
//        public virtual ICollection<Purchases> Purchases { get; set; }

//        [InverseProperty("User")]
//        public virtual ICollection<Sales> Sales { get; set; }

//        [InverseProperty("User")]
//        public virtual ICollection<Stockmovements> Stockmovements { get; set; }

//        [InverseProperty("User")]
//        public virtual ICollection<Stockaudits> Stockaudits { get; set; }

//        [InverseProperty("User")]
//        public virtual ICollection<SystemLogs> Systemlogs { get; set; }

//        [InverseProperty("User")]
//        public virtual ICollection<BarcodeGenerator> BarcodeGenerator { get; set; }
//    }
//}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("users")]
    public partial class Users
    {
        public Users()
        {
            Drugtransfers = new HashSet<Drugtransfers>();
            Fundtransfers = new HashSet<Fundtransfers>();
            Journalentries = new HashSet<Journalentries>();
            Purchases = new HashSet<Purchases>();
            Sales = new HashSet<Sales>();
            Stockmovements = new HashSet<Stockmovements>();
            Stockaudits = new HashSet<Stockaudits>();
            Systemlogs = new HashSet<SystemLogs>();
            BarcodeGenerator = new HashSet<BarcodeGenerator>();
        }

        [Key]
        [Column("UserID", TypeName = "int(11)")]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "varchar(200)")]
        [StringLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("RoleID", TypeName = "int(11)")]
        public int RoleId { get; set; }

        [Column("EmployeeID", TypeName = "int(11)")]
        public int? EmployeeId { get; set; }

        [Column("DefaultBranchID", TypeName = "int(11)")]
        public int? DefaultBranchId { get; set; }

        [Required]
        public bool? IsActive { get; set; }

        // ?? ╟ط┌ط╟▐╟╩
        [ForeignKey(nameof(DefaultBranchId))]
        [InverseProperty(nameof(Branches.Users))]
        public virtual Branches DefaultBranch { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        [InverseProperty(nameof(Employees.Users))]
        public virtual Employees Employee { get; set; }

        [ForeignKey(nameof(RoleId))]
        [InverseProperty(nameof(Userroles.Users))]
        public virtual Userroles Role { get; set; }

        // ?? ╟طع╠عµ┌╟╩ ╟طع╤╩╚╪╔ ╚╟طع╙╩╬╧ع
        [InverseProperty("CreatedByNavigation")]
        public virtual ICollection<Drugtransfers> Drugtransfers { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Fundtransfers> Fundtransfers { get; set; }

        [InverseProperty("CreatedByNavigation")]
        public virtual ICollection<Journalentries> Journalentries { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Purchases> Purchases { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Sales> Sales { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Stockmovements> Stockmovements { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Stockaudits> Stockaudits { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<SystemLogs> Systemlogs { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<BarcodeGenerator> BarcodeGenerator { get; set; }
    }
}


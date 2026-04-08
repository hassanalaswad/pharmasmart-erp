using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("userroles")]
    public partial class Userroles
    {
        public Userroles()
        {
            Screenpermissions = new HashSet<Screenpermissions>();
            Users = new HashSet<Users>();
        }

        [Key]
        [Column("RoleID", TypeName = "int(11)")]
        public int RoleId { get; set; }
        // ?? ╟ط┼╓╟▌╔ ╟ط╠µف╤و╔ ط═ط ╟ط╬╪├: 
        // و╠╚ ├غ و╩╪╟╚▐ ╟ط╟╙ع ╩ع╟ع╟≡ ع┌ ع╟ فµ عµ╠µ╧ ▌و ▐╟┌╧╔ ╟ط╚و╟غ╟╩
        [StringLength(100)]
        public string? RoleArabicName { get; set; }
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string RoleName { get; set; } = string.Empty;
        [Column(TypeName = "varchar(200)")]
        public string? RoleDescription { get; set; }
        [Required]
        public bool? IsActive { get; set; }

        [InverseProperty("Role")]
        public virtual ICollection<Screenpermissions> Screenpermissions { get; set; }
        [InverseProperty("Role")]
        public virtual ICollection<Users> Users { get; set; }


    }
}



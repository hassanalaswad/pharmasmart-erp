using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("screenpermissions")]
    public partial class Screenpermissions
    {
        [Key]
        [Column("PermissionID", TypeName = "int(11)")]
        public int PermissionId { get; set; }
        [Column("RoleID", TypeName = "int(11)")]
        public int RoleId { get; set; }
        [Column("ScreenID", TypeName = "int(11)")]
        public int ScreenId { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }

        [ForeignKey(nameof(RoleId))]
        [InverseProperty(nameof(Userroles.Screenpermissions))]
        public virtual Userroles Role { get; set; }
        [ForeignKey(nameof(ScreenId))]
        [InverseProperty(nameof(Systemscreens.Screenpermissions))]
        public virtual Systemscreens Screen { get; set; }
    }
}

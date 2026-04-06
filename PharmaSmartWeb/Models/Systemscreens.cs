using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace PharmaSmartWeb.Models
{
    [Table("systemscreens")]
    public partial class Systemscreens
    {
        public Systemscreens()
        {
            Screenpermissions = new HashSet<Screenpermissions>();
        }

        [Key]
        [Column("ScreenID", TypeName = "int(11)")]
        public int ScreenId { get; set; }
        [Required]
        [Column(TypeName = "varchar(100)")]
        public string ScreenName { get; set; } = string.Empty;
        [Required]
        [Column(TypeName = "varchar(100)")]
        public string ScreenArabicName { get; set; } = string.Empty;
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string ScreenCategory { get; set; } = string.Empty;

        [InverseProperty("Screen")]
        public virtual ICollection<Screenpermissions> Screenpermissions { get; set; }
    }
}



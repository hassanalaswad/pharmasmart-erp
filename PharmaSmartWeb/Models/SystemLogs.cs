using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("systemlogs")]
    public class SystemLogs
    {
        [Key]
        public int LogId { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ScreenName { get; set; }

        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(45)]
        public string? IPAddress { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }
    }
}


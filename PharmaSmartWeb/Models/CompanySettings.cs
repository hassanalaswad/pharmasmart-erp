using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    public class CompanySettings
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المؤسسة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم المؤسسة")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "مسار شعار المؤسسة")]
        public string? CompanyLogoPath { get; set; }

        [StringLength(500)]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        [StringLength(50)]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Display(Name = "الرقم الضريبي")]
        public string? TaxNumber { get; set; }
    }
}

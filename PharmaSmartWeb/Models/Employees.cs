using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("employees")]
    public partial class Employees
    {
        public Employees()
        {
            Users = new HashSet<Users>();
        }

        [Key]
        [Column("EmployeeID", TypeName = "int(11)")]
        public int EmployeeId { get; set; }

        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "يرجى إدخال الاسم الكامل")]
        [Column(TypeName = "varchar(150)")]
        public string FullName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string? Position { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? Phone { get; set; }

        // 💰 الإضافة الجديدة: الراتب
        [Required(ErrorMessage = "يرجى تحديد الراتب")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        [Required]
        public bool? IsActive { get; set; }

        // BranchId is non-nullable (required FK) → use null!
        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.Employees))]
        public virtual Branches Branch { get; set; } = null!;

        [InverseProperty("Employee")]
        public virtual ICollection<Users> Users { get; set; }
    }
}
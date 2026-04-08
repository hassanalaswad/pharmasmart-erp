using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("drugs")]
    public partial class Drugs
    {
        public Drugs()
        {
            Branchinventory = new HashSet<Branchinventory>();
            Drugtransferdetails = new HashSet<Drugtransferdetails>();
            DrugBatches = new HashSet<DrugBatches>();
            Purchasedetails = new HashSet<Purchasedetails>();
            Saledetails = new HashSet<Saledetails>();
            Forecasts = new HashSet<Forecasts>();
            Seasonaldata = new HashSet<Seasonaldata>();
        }

        [Key]
        [Column("DrugID", TypeName = "int(11)")]
        public int DrugId { get; set; }

        [Required(ErrorMessage = "╪د╪│┘à ╪د┘╪»┘ê╪د╪ة ┘à╪╖┘┘ê╪ذ")]
        [Column(TypeName = "varchar(150)")]
        public string DrugName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(150)")]
        public string? Manufacturer { get; set; }
        [Column("GroupId", TypeName = "int(11)")]
        public int? GroupId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? Barcode { get; set; }

        // ≡اأ ╪ز┘à ╪ص╪░┘ CostPrice ┘ê SellPrice ┘╪ز╪╖╪ذ┘è┘é ┘à╪╣╪د┘è┘è╪▒ ╪د┘┘ ERP

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "varchar(10)")]
        public string? SaremaCategory { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? CategoryName { get; set; }

        [Column(TypeName = "int(11)")]
        public int? CategoryId { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? ImagePath { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string MainUnit { get; set; } = "╪╣┘╪ذ╪ر";

        [Column(TypeName = "int(11)")]
        public int? UnitId { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string SubUnit { get; set; } = "╪ص╪ذ╪ر";

        public int ConversionFactor { get; set; } = 1;


        public bool? IsDeleted { get; set; }
        public bool? IsLifeSaving { get; set; } = false;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
      
      

        // ╪▒╪ذ╪╖ ╪د┘╪»┘ê╪د╪ة ╪ذ╪╖╪د╪ذ┘ê╪▒ ╪د┘╪ذ╪د╪▒┘â┘ê╪»
        [InverseProperty(nameof(Models.BarcodeGenerator.Drug))]
        public virtual ICollection<BarcodeGenerator> BarcodeQueue { get; set; } = new HashSet<BarcodeGenerator>();

        // ╪د┘╪╣┘╪د┘é╪د╪ز
        [InverseProperty("Drug")]
        public virtual ICollection<Branchinventory> Branchinventory { get; set; }
        // GroupId is nullable (optional FK) ظْ properly nullable navigation
        [ForeignKey(nameof(GroupId))]
        [InverseProperty(nameof(ItemGroups.Drugs))]
        public virtual ItemGroups? ItemGroup { get; set; }
        [InverseProperty("Drug")]
        public virtual ICollection<Drugtransferdetails> Drugtransferdetails { get; set; }
        [InverseProperty("Drug")]
        public virtual ICollection<DrugBatches> DrugBatches { get; set; }
        [InverseProperty("Drug")]
        public virtual ICollection<Purchasedetails> Purchasedetails { get; set; }
        [InverseProperty("Drug")]
        public virtual ICollection<Saledetails> Saledetails { get; set; }
        [InverseProperty("Drug")]
        public virtual ICollection<Forecasts> Forecasts { get; set; }
        [InverseProperty("Drug")]
        public virtual ICollection<Seasonaldata> Seasonaldata { get; set; }
 
        [InverseProperty("Drug")]
        public virtual ICollection<Stockmovements> Stockmovements { get; set; } = new HashSet<Stockmovements>();
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("branches")]
    public partial class Branches
    {
        public Branches()
        {
            Branchinventory = new HashSet<Branchinventory>();
            DrugtransfersFromBranch = new HashSet<Drugtransfers>();
            DrugtransfersToBranch = new HashSet<Drugtransfers>();
            Employees = new HashSet<Employees>();
            Forecasts = new HashSet<Forecasts>();
            Journalentries = new HashSet<Journalentries>();
            Purchases = new HashSet<Purchases>();
            Sales = new HashSet<Sales>();
            Seasonaldata = new HashSet<Seasonaldata>();
            Stockmovements = new HashSet<Stockmovements>();
            Users = new HashSet<Users>();
            Warehouses = new HashSet<Warehouses>();
            BranchAccounts = new HashSet<BranchAccounts>(); // ≡اأ ╪ز┘ç┘è╪خ╪ر ╪د┘┘à╪ش┘à┘ê╪╣╪ر ╪د┘╪ش╪»┘è╪»╪ر
            Accounts = new HashSet<Accounts>(); // ≡اأ ╪د┘╪ز╪╡╪ص┘è╪ص: ╪ز┘ç┘è╪خ╪ر ┘à╪ش┘à┘ê╪╣╪ر ╪د┘╪ص╪│╪د╪ذ╪د╪ز

        }

        [Key]
        [Column("BranchID", TypeName = "int(11)")]
        public int BranchId { get; set; }
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string BranchCode { get; set; } = string.Empty;
        [Required]
        [Column(TypeName = "varchar(150)")]
        public string BranchName { get; set; } = string.Empty;
        [Column(TypeName = "varchar(200)")]
        public string? Location { get; set; }
        [Required]
        public bool? IsActive { get; set; }

        // ≡اأ ┘à╪╣╪د┘è┘è╪▒ ╪د┘┘ ERP (╪د┘╪ز┘ê╪ش┘è┘ç ╪د┘┘à╪ص╪د╪│╪ذ┘è ┘ê╪د┘╪╣┘à┘╪د╪ز)
        public int? DefaultCashAccountId { get; set; }
        public int? DefaultSalesAccountId { get; set; }
        public int? DefaultCOGSAccountId { get; set; }
        public int? DefaultInventoryAccountId { get; set; }
        public int? DefaultCurrencyId { get; set; }

        [ForeignKey(nameof(DefaultCashAccountId))]
        public virtual Accounts? DefaultCashAccount { get; set; }

        [ForeignKey(nameof(DefaultSalesAccountId))]
        public virtual Accounts? DefaultSalesAccount { get; set; }

        [ForeignKey(nameof(DefaultCOGSAccountId))]
        public virtual Accounts? DefaultCOGSAccount { get; set; }

        [ForeignKey(nameof(DefaultInventoryAccountId))]
        public virtual Accounts? DefaultInventoryAccount { get; set; }

        [ForeignKey(nameof(DefaultCurrencyId))]
        [InverseProperty(nameof(Currencies.Branches))]
        public virtual Currencies? DefaultCurrency { get; set; }

        // ===========================================

        [InverseProperty("Branch")]
        public virtual ICollection<Branchinventory> Branchinventory { get; set; }
        [InverseProperty(nameof(Drugtransfers.FromBranch))]
        public virtual ICollection<Drugtransfers> DrugtransfersFromBranch { get; set; }
        [InverseProperty(nameof(Drugtransfers.ToBranch))]
        public virtual ICollection<Drugtransfers> DrugtransfersToBranch { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Employees> Employees { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Forecasts> Forecasts { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Journalentries> Journalentries { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Purchases> Purchases { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Sales> Sales { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Seasonaldata> Seasonaldata { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Stockmovements> Stockmovements { get; set; }
        [InverseProperty("DefaultBranch")]
        public virtual ICollection<Users> Users { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<Warehouses> Warehouses { get; set; }
        [InverseProperty("Branch")]
        public virtual ICollection<BranchAccounts> BranchAccounts { get; set; }

        // ... ╪ذ┘é┘è╪ر ╪د┘┘ ICollections ╪د┘╪│╪د╪ذ┘é╪ر ...
        // ≡اأ ╪د┘╪ز╪╡╪ص┘è╪ص: ╪ح╪╢╪د┘╪ر ╪د┘╪╣┘╪د┘é╪ر ╪د┘╪╣┘â╪│┘è╪ر ╪د┘┘à┘┘é┘ê╪»╪ر ╪د┘╪ز┘è ╪│╪ذ╪ذ╪ز ╪د┘╪«╪╖╪ث!
        [InverseProperty("Branch")]
        public virtual ICollection<Accounts> Accounts { get; set; }

    }
}

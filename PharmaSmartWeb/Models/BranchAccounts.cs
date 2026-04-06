using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSmartWeb.Models
{
    [Table("branch_accounts")]
    public partial class BranchAccounts
    {
        [Key]
        public int Id { get; set; }

        public int BranchId { get; set; }

        public int AccountId { get; set; }

        [ForeignKey(nameof(BranchId))]
        [InverseProperty(nameof(Branches.BranchAccounts))]
        // BranchId is non-nullable (required FK) → EF Core guarantees initialization
        public virtual Branches Branch { get; set; } = null!;

        [ForeignKey(nameof(AccountId))]
        [InverseProperty(nameof(Accounts.BranchAccounts))]
        // AccountId is non-nullable (required FK) → EF Core guarantees initialization
        public virtual Accounts Account { get; set; } = null!;
    }
}
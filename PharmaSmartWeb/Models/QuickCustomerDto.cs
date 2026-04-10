namespace PharmaSmartWeb.Models
{
    /// <summary>
    /// DTO for quick customer creation from POS modal (AJAX)
    /// </summary>
    public class QuickCustomerDto
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal CreditLimit { get; set; }
    }
}

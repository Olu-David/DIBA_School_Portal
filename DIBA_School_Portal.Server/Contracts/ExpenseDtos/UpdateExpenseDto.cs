using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts.ExpenseDtos
{
    public class UpdateExpenseDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Category { get; set; } = "";

        [Required]
        [StringLength(500, MinimumLength = 2)]
        public string Description { get; set; } = "";

        [Required]
        [Range(0.01, 100_000_000)]
        public decimal Amount { get; set; }

        [Required]
        public PaymentGateway PaymentGateway { get; set; }   // 👈 changed

        [StringLength(200)]
        public string? Vendor { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public string? Notes { get; set; }
    }
}

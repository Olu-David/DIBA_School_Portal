using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts.ExpenseDtos
{
    public class CreateExpenseDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Category { get; set; } = "";

        [Required]
        [StringLength(500, MinimumLength = 2)]
        public string Description { get; set; } = "";

        [Required]
        [Range(0.01, 100_000_000, ErrorMessage = "Amount must be positive.")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentGateway PaymentGateway { get; set; }   // 👈 changed from string PaymentMethod

        [StringLength(200)]
        public string? Vendor { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public string? Notes { get; set; }

        public Guid? AcademicSessionId { get; set; }

        public Guid? TermId { get; set; }

    }
}

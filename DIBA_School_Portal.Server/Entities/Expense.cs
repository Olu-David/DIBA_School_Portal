using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class Expense
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "";

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = "";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // 👈 Changed from string PaymentMethod to PaymentGateway enum
        [Required]
        public PaymentGateway PaymentGateway { get; set; }

        [MaxLength(200)]
        public string? Vendor { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = "";

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        public string? ReceiptUrl { get; set; }

        public string? Notes { get; set; }

        public string RecordedByUserId { get; set; } = "";

        public Guid? AcademicSessionId { get; set; }

        public Guid? TermId { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
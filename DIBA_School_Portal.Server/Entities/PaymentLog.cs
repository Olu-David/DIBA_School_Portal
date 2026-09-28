using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class PaymentLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string StudentId { get; set; } = string.Empty; // Links payment to a specific student

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } // e.g., 50000.00

        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty; // Transaction ref from gateway

        [Required]
        public PaymentGateway Gateway { get; set; } = PaymentGateway.Paystack; // Enum used here

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending; // Enum used here

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty; // e.g., "2026/2027 First Term School Fees"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; } // When the webhook confirmed the payment
    }
}

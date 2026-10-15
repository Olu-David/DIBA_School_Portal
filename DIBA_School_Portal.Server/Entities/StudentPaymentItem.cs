using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class StudentPaymentItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentPaymentId { get; set; }

        [ForeignKey(nameof(StudentPaymentId))]
        public StudentPayment StudentPayment { get; set; } = null!;

        // ★ nullable — the fee may be soft-deleted later, but the payment history survives
        public Guid? FeeId { get; set; }

        [ForeignKey(nameof(FeeId))]
        public Fee? Fee { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountAllocated { get; set; }
    }
}
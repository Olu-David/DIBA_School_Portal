using DIBA_School_Portal.Server.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class StudentPaymentItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid? StudentPaymentId { get; set; }
    [ForeignKey(nameof(StudentPaymentId))]
    public StudentPayment StudentPayment { get; set; } = null!;

    // Can point to a Fee (like Tuition) OR a Book (like Math Textbook)
    public Guid? FeeId { get; set; }
    [ForeignKey(nameof(FeeId))]
    public Fee? Fee { get; set; }

    public Guid? ClassBookId { get; set; }
    [ForeignKey(nameof(ClassBookId))]
    public ClassBook? ClassBook { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountAllocated { get; set; }
}
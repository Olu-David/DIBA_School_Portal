using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Entities
{
    public class ClassFeePrice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid FeeId { get; set; }

        [ForeignKey(nameof(FeeId))]
        public Fee Fee { get; set; } = null!;

        [Required]
        public Guid SchoolClassId { get; set; }

        [ForeignKey(nameof(SchoolClassId))]
        public SchoolClass SchoolClass { get; set; } = null!;
        public decimal Amount { get;  set; }
        public decimal MinimumAmountToPay { get;  set; }
    }
}

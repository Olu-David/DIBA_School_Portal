using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class FeeCategory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Tuition", "Uniform", "ICT Levy"

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public ICollection<Fee> FeeStructures { get; set; } = new List<Fee>();
    }
}

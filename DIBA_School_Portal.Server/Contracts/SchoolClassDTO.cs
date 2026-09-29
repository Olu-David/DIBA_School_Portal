using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolClassDTO
    {

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., "JSS 1", "SS 1"

        public int DisplayOrder { get; set; }
    }
}

using DIBA_School_Portal.Server.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ClassRoom
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty; // e.g., "A", "B"

    [Required]
    public Guid ClassLevelId { get; set; }

    [ForeignKey(nameof(ClassLevelId))]
    public ClassLevel ClassLevel { get; set; } = null!;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
using DIBA_School_Portal.Server.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Database_Entities
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public Guid? ClassId { get; set; }

        [ForeignKey(nameof(ClassId))]
        public virtual SchoolClass? SchoolClass { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        [MaxLength(50)]
        public string? PrimaryRole { get; set; }

        // Role-specific navigation
        public virtual Student? StudentProfile { get; set; }
        public virtual Teacher? TeacherProfile { get; set; }

        // Parent → children links
        public virtual ICollection<ParentStudent> Children { get; set; }
            = new List<ParentStudent>();

        // Notifications
        public virtual ICollection<NotificationLog> Notifications { get; set; }
            = new List<NotificationLog>();
        public bool MustChangePassword { get;  set; }
      
    }
}
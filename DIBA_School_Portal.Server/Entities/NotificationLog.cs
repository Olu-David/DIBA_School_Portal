using DIBA_School_Portal.Server.Enums;
using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Entities
{
    public class NotificationLog
    {
   
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            [Required]
            public string UserId { get; set; } = string.Empty; // The recipient (FK to AspNetUsers)

            [Required]
            [MaxLength(150)]
            public string Title { get; set; } = string.Empty; // e.g., "Fee Payment Successful"

            [Required]
            public string Message { get; set; } = string.Empty; // The full body text of the alert

            [Required]
            [MaxLength(50)]
            public NotificationChannel Channel { get; set; }  // e.g., "InApp", "Email", "SMS", "Push"

            [Required]
            [MaxLength(50)]
            public string Category { get; set; } = "General"; // e.g., "Finance", "Academic", "Security"

            public bool IsRead { get; set; } = false; // For in-app bell notification tracking

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? ReadAt { get; set; } // When the user clicked and read it
        }
    }

using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class ContactRequest
    {
       
            [Required, MaxLength(150)]
            public string Name { get; set; } = string.Empty;

            [Required, EmailAddress, MaxLength(200)]
            public string Email { get; set; } = string.Empty;

            [MaxLength(30)]
            public string? Phone { get; set; }

            [MaxLength(100)]
            public string? Subject { get; set; }

            [Required, MaxLength(4000)]
            public string Message { get; set; } = string.Empty;
        }
    }



using Microsoft.AspNetCore.Http;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class CreateAccountDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;

        // Students only
        public Guid? SchoolClassId { get; set; }
        public string? Stream { get; set; }        

        // Staff only
        public string? Password { get; set; }
        public string? Qualification { get; set; }
        public DateTime? DateEmployed { get; set; }
        public string? PrimaryRole { get; set; }

        // Optional — uploaded to Supabase if present
        public IFormFile? ProfilePicture { get; set; }
    }
}
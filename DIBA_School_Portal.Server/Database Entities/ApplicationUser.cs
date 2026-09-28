using Microsoft.AspNetCore.Identity;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; } 
        public string? LastName { get; set; }

    }
}

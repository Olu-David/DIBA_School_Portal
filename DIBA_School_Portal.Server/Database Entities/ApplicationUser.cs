using DIBA_School_Portal.Server.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace DIBA_School_Portal.Server.Configurations
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; } 
        public string? LastName { get; set; }
        public Guid ClassId { get; set;  }
        [ForeignKey(nameof(ClassId))]
        public virtual SchoolClass? SchoolClass { get; set; }

    }
}

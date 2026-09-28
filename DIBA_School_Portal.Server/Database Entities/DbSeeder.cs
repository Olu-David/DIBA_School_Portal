using Microsoft.AspNetCore.Identity;

namespace DIBA_School_Portal.Server.Configurations
{
    public class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider _serviceProvider)
        {
            //Create a scope to get the required services
            var scope = _serviceProvider.CreateScope();
            //Get the required services
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>();

            
            // Seed roles
            var roles = new[] { "SuperAdmin","Principal","Headmaster","VicePrincipal","Bursar","Registrar", "Teacher","FormTeacher","Student", "Parent" };

            foreach(var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{role}' created successfully.");
                    }
                    else
                    {
                        logger.LogError($"Error creating role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}

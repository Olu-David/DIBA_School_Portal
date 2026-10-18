using DIBA_School_Portal.Server.Database_Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DIBA_School_Portal.Server.Database_Entities
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ---------- DbContext (Dynamic Provider Switcher) ----------
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            services.AddDbContext<AppDbContext>(options =>
            {
                // Check if the connection string looks like PostgreSQL (Supabase/Npgsql) or SQL Server
                if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                    connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
                {
                    // Use PostgreSQL (Supabase)
                    options.UseNpgsql(connectionString, pgOptions =>
                        pgOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null));
                }
                else
                {
                    // Use SQL Server (LocalDB / Local testing)
                    options.UseSqlServer(connectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null));
                }

                // ⚠️ Suppress PendingModelChangesWarning
                // The migration snapshot is stale because entities were edited after
                // the migration was generated. Suppressing lets the app start.
                // REMOVE this once migrations are regenerated (see fix below).
                options.ConfigureWarnings(w =>
                    w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            // ---------- Identity ----------
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password policy
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // Lockout
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User
                options.User.RequireUniqueEmail = false;

                // Permits '/' for admission numbers like DIBA/2026/009
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+/";
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}
using DIBA_School_Portal.Server.Configurations;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace DIBA_School_Portal.Server.Database_Entities
{
   
        public static class WebApplicationExtensions
        {
            public static async Task<WebApplication> UseAppPipelineAsync(
                this WebApplication app)
            {
                var env = app.Environment;

                // ---------- Static files ----------
                app.UseDefaultFiles();
                app.MapStaticAssets();

                // ---------- Dev-only tooling ----------
                if (env.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.MapScalarApiReference();
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/error");
                    app.UseHsts();
                }

                // ---------- CORS before auth ----------
                app.UseCors(CorsExtensions.PolicyName);

                // ---------- Rate limiting before auth (protects /login) ----------
                app.UseRateLimiter();

                // ---------- HTTPS redirect (skip in dev for Vite proxy) ----------
                if (!env.IsDevelopment())
                {
                    app.UseHttpsRedirection();
                }

                // ---------- Auth ----------
                app.UseAuthentication();
                app.UseAuthorization();

                // ---------- Endpoints ----------
                app.MapControllers();

                // SignalR hub (after auth so [Authorize] on Hub works)
                app.MapHub<DashboardHub>("/dashboardHub").RequireAuthorization();

                // Aspire defaults
                app.MapDefaultEndpoints();

                // SPA fallback (must be last)
                app.MapFallbackToFile("/index.html");

                // ---------- Health ----------
                app.MapGet("/health", () => Results.Ok(new
                {
                    status = "healthy",
                    time = DateTime.UtcNow,
                    env = env.EnvironmentName,
                }));

                return app;
            }

            public static async Task MigrateAndSeedAsync(this WebApplication app)
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    await context.Database.MigrateAsync();
                    await DbSeeder.SeedAsync(app.Services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during migration/seeding.");
                    throw;   // ← do NOT swallow — you want to know if startup fails
                }
            }
        }
    }

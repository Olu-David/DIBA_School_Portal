using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;                 // 👈 THIS is what fixes 28 and 35
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class PromotionAutoRunner : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PromotionAutoRunner> _logger;

        public PromotionAutoRunner(
            IServiceProvider services,
            ILogger<PromotionAutoRunner> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give the app 1 minute to finish startup before doing anything.
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();

                    var context = scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();
                    var promotion = scope.ServiceProvider
                        .GetRequiredService<IPromotionService>();

                    var session = await context.AcademicSessions
                        .Where(s => s.IsCurrentSession)
                        .FirstOrDefaultAsync(stoppingToken);           // ✅ now works

                    if (session == null)
                    {
                        _logger.LogDebug("Auto-promotion: no current session.");
                        await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                        continue;
                    }

                    var already = await context.PromotionRecords
                        .AnyAsync(r => r.AcademicSessionId == session.Id, stoppingToken);  // ✅ now works

                    if (!already)
                    {
                        var preview = await promotion.BuildPreviewAsync(session.Id);

                        if (preview.Success
                            && preview.Data?.IsReady == true
                            && preview.Data.TotalRequiresStream == 0)
                        {
                            var result = await promotion.ExecuteAsync(
                                session.Id,
                                new(),
                                "system-auto");

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "Auto-promotion for {Session}: {Promoted} promoted, {Graduated} graduated.",
                                    session.Name,
                                    result.Data?.PromotedCount ?? 0,
                                    result.Data?.GraduatedCount ?? 0);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Auto-promotion skipped: {Message}",
                                    result.Message);
                            }
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Auto-promotion deferred — stream picks required ({Count}).",
                                preview.Data?.TotalRequiresStream ?? 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auto-promotion cycle failed.");
                }

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}
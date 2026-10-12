using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class SmsWorker : BackgroundService
    {
        private readonly IBackgroundSmsQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SmsWorker> _logger;

        public SmsWorker(
            IBackgroundSmsQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<SmsWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SMS worker started.");

            var sendDelay = TimeSpan.FromMilliseconds(400);

            await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sms = scope.ServiceProvider.GetRequiredService<ISmsService>();

                    var ok = await sms.SendSmsAsync(job.ToPhone, job.Message);

                    if (ok)
                        _logger.LogInformation(
                            "[SmsWorker] Sent → {Phone} (queue {QueueId})",
                            job.ToPhone, job.QueueId);
                    else
                        _logger.LogWarning(
                            "[SmsWorker] Provider returned false → {Phone} (queue {QueueId})",
                            job.ToPhone, job.QueueId);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[SmsWorker] FAILED → {Phone} (queue {QueueId})",
                        job.ToPhone, job.QueueId);
                }

                try { await Task.Delay(sendDelay, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("SMS worker stopped.");
        }
    }
}
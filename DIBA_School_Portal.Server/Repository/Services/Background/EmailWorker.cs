using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class EmailWorker : BackgroundService
    {
        private readonly IBackgroundEmailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailWorker> _logger;

        public EmailWorker(
            IBackgroundEmailQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email worker started.");

            var sendDelay = TimeSpan.FromMilliseconds(400);

            await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await email.EmailSendAsync(job.ToEmail, job.Subject, job.HtmlBody);

                    _logger.LogInformation(
                        "[EmailWorker] Sent -> {Email} (queue {QueueId})",
                        job.ToEmail, job.QueueId);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[EmailWorker] FAILED -> {Email} (queue {QueueId})",
                        job.ToEmail, job.QueueId);
                }

                try { await Task.Delay(sendDelay, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("Email worker stopped.");
        }
    }
}
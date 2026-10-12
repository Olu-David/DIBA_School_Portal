namespace DIBA_School_Portal.Server.Services.Background
{
    public interface IBackgroundEmailQueue
    {
        /// <summary>Enqueue a single email job.</summary>
        void Enqueue(EmailJob job);

        /// <summary>Drain all pending jobs (used by the worker).</summary>
        IAsyncEnumerable<EmailJob> DequeueAllAsync(CancellationToken ct);
    }

    public class EmailJob
    {
        public string QueueId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }
}
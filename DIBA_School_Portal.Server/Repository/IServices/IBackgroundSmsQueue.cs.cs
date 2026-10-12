namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IBackgroundSmsQueue
    {
        void Enqueue(SmsJob job);
        IAsyncEnumerable<SmsJob> DequeueAllAsync(CancellationToken ct);
    }

    public class SmsJob
    {
        public string QueueId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string ToPhone { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }
}
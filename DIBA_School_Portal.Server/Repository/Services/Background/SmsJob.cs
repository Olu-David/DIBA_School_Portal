namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class SmsJob
    {
     
            public string QueueId { get; set; } = string.Empty;
            public string ToPhone { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string RecipientName { get; set; } = string.Empty;
            public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
        }
    }


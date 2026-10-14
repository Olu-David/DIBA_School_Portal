namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class EmailJob
    {  
            public string QueueId { get; set; } = string.Empty;
            public string ToEmail { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string HtmlBody { get; set; } = string.Empty;
            public string RecipientName { get; set; } = string.Empty;
            public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
        }
    }

namespace DIBA_School_Portal.Server.Contracts
{
    public class SendQueuedResponse
    {

        public int RecipientCount { get; set; }
        public string QueueId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

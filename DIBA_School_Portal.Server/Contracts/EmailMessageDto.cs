namespace DIBA_School_Portal.Server.Contracts
{
    public class EmailMessageDto
    {
        public string To { get; set; }
        public string? Subject { get; set; }
        public string? HtmlBody { get; set; }
    }
}

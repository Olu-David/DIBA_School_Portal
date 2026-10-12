using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SendSmsRequest
    {
        [Required, MaxLength(480)]
        public string Message { get; set; } = string.Empty;

        public List<string>? ParentUserIds { get; set; }
        public string Target { get; set; } = "List";
        public Guid? ClassId { get; set; }
        public string? Section { get; set; }
    }
}

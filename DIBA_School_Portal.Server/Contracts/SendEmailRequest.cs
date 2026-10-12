using System.ComponentModel.DataAnnotations;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SendEmailRequest
    {
        [Required, MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(10000)]
        public string HtmlBody { get; set; } = string.Empty;

        /// <summary>One or more parent User IDs. Ignored when Target=All.</summary>
        public List<string>? ParentUserIds { get; set; }

        /// <summary>
        /// Who to send to:
        ///   "All"       → every active parent
        ///   "Class"     → parents of a specific class
        ///   "Section"   → parents of a specific section
        ///   "List"      → only the ParentUserIds list
        /// </summary>
        public string Target { get; set; } = "List";

        public Guid? ClassId { get; set; }
        public string? Section { get; set; }
    }
}

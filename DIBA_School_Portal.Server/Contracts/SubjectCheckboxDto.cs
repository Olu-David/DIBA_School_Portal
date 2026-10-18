using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class SubjectCheckboxDto
    {
        public Guid SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
        public ClassStream? Stream { get; set; }
    }
}

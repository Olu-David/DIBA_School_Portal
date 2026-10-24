using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class AssignClassSubjectItemDto
    {
        public Guid SubjectId { get; set; }
        public ClassStream? Stream { get; set; }
    }
}

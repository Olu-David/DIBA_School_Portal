using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentFilterDto
    {
        public Guid? ClassId { get; set; }
        public AcademicSection? Section { get; set; }
        public ClassStream? Stream { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasOutstanding { get; set; }
        public string? SearchTerm { get; set; }  // name / admission number
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}

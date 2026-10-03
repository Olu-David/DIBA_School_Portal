using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class UpdateStudentDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public Guid SchoolClassId { get; set; }
        public ClassStream? Stream { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

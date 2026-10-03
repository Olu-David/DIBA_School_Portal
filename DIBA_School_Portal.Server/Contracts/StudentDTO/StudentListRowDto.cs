using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentListRowDto
    {
        public Guid Id { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public ClassStream? Stream { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
        public decimal OutstandingBalance { get; set; }
        public bool IsFullyPaid { get; set; }
        public string? PrimaryGuardianPhone { get; set; }
    }
}

namespace DIBA_School_Portal.Server.Contracts
{
    public class CreateStudentWithParentResponseDto
    {
        public string StudentUserId { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string StudentTempPassword { get; set; } = string.Empty;

        // Parent
        public string ParentUserId { get; set; } = string.Empty;
        public string ParentLogin { get; set; } = string.Empty; // phone or email
        public string? ParentTempPassword { get; set; }         // null if parent already existed
        public bool ParentWasCreated { get; set; }
    }
}

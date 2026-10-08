namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class TeacherFilterDto
    {
        public string? SearchTerm { get; set; }       // name / staff ID / email
        public string? Qualification { get; set; }
        public string? PrimaryRole { get; set; }      // "Teacher" / "FormTeacher"
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}

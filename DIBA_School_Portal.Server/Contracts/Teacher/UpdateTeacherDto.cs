namespace DIBA_School_Portal.Server.Contracts.Teacher
{
    public class UpdateTeacherDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Qualification { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}

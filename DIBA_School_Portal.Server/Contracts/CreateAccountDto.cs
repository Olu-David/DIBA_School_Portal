namespace DIBA_School_Portal.Server.Contracts
{
    public class CreateAccountDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }        // required for staff
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid? SchoolClassId { get; set; }  // required for students
    }
}

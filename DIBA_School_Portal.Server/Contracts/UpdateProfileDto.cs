namespace DIBA_School_Portal.Server.Contracts
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? PhoneNumber { get; set; }
    }
}

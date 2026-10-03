namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class CreateAccountResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AdmissionNumber { get; set; }
        public string TemporaryPassword { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}

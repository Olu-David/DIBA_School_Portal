namespace DIBA_School_Portal.Server.Contracts
{
    public class StudentLoginRequestDto
    {
        /// e.g. "DIBA/2026/001"
        public string AdmissionNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

namespace DIBA_School_Portal.Server.Contracts
{
    public class ParentLoginRequestDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

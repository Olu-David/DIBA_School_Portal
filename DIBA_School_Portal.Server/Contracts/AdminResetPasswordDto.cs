namespace DIBA_School_Portal.Server.Contracts
{
    public class AdminResetPasswordDto
    {
        public string TargetUserId { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public object StudentProfileId { get; internal set; }
    }
}

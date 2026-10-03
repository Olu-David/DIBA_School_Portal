namespace DIBA_School_Portal.Server.Contracts
{
    public class UpdateAcademicSessionRequest
    {
        public AcademicSessionDto Session { get; set; } = null!;
        public SchoolTermDto Term { get; set; } = null!;
    }
}

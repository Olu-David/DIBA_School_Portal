namespace DIBA_School_Portal.Server.Contracts
{
    public class ParentChildDto
    {
        public Guid StudentProfileId { get; set; }
        public string AdmissionNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimaryContact { get; set; }
    }
}

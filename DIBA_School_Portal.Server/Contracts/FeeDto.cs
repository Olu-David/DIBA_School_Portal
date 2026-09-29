using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class FeeDto
    {
        public Guid Id { get; set; }
        public string FeeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public Guid TermId { get; set; }
        public Guid SchoolClassId { get; set; } 
        public AcademicSection Section { get; set; }
        public ClassStream Stream { get; set; } = ClassStream.General;
        public Guid AcademicSessionId { get;  set; }
    }
}
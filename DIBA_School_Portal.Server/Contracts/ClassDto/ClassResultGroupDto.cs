using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts.ClassDto
{
    public class ClassResultGroupDto
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public AcademicSection Section { get; set; }
        public string SectionName => Section.ToString();
        public int DisplayOrder { get; set; }

        public int StudentCount { get; set; }
        public decimal ClassAverage { get; set; }
        public decimal HighestAverage { get; set; }
        public decimal LowestAverage { get; set; }
        public int PublishedCount { get; set; }
        public int PendingCount { get; set; }

        public List<StreamResultGroupDto> Streams { get; set; } = new();
    }
}

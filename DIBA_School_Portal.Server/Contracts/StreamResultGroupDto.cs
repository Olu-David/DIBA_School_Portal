using DIBA_School_Portal.Server.Enums;

namespace DIBA_School_Portal.Server.Contracts
{
    public class StreamResultGroupDto
    {
        public ClassStream? Stream { get; set; }              
        public string StreamName { get; set; } = "No Stream";
        public int StudentCount { get; set; }
        public decimal StreamAverage { get; set; }
        public List<StudentResultSummaryDto> Students { get; set; } = new();
    }
}

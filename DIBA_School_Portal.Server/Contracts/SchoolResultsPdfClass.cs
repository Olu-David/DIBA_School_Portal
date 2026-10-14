namespace DIBA_School_Portal.Server.Contracts
{
    public class SchoolResultsPdfClass
    {

        public string ClassName { get; set; } = "";
        public int StudentCount { get; set; }
        public decimal ClassAverage { get; set; }
        public decimal HighestAverage { get; set; }
        public decimal LowestAverage { get; set; }
        public int CompleteCount { get; set; }
        public int PendingCount { get; set; }

        public List<SchoolResultsPdfStudent> Students { get; set; } = new();
    }
}

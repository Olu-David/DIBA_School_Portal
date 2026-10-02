namespace DIBA_School_Portal.Server.Contracts
{
    public class ArrearsRowDto
    {
        public Guid StudentProfileId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public decimal Expected { get; set; }
        public decimal Paid { get; set; }
        public decimal Outstanding { get; set; }
        public bool IsFullyPaid { get; set; }

    }
}

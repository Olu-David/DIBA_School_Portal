namespace DIBA_School_Portal.Server.Contracts
{
    public class ChildEnrollmentDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicSessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public Guid SchoolClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmountPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public bool IsFullyPaid { get; set; }
        public DateTime EnrolledAt { get; set; }
    }
}

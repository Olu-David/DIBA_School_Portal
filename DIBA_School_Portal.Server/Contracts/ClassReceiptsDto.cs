using DIBA_School_Portal.Server.Contracts.StudentDTO;

namespace DIBA_School_Portal.Server.Contracts
{
    public class ClassReceiptsDto
    {
        public string ClassName { get; set; } = "";
        public string TermName { get; set; } = "";
        public string AcademicSessionName { get; set; } = "";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public List<StudentPaymentReceiptDto> Receipts { get; set; } = new();
    }
}

using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Services;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentPaymentReceiptDto
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public List<FeeItemDto> FeeItems { get; set; } = new();
        public string? ProfilePictureUrl { get;  set; }
    }
}
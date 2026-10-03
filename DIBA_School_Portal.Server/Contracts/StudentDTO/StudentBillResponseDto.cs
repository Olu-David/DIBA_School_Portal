using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Repository.Services;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentBillResponseDto
    {
        public Guid StudentProfileId { get; set; }
        public string? StudentName { get; set; }
        public string? ClassName { get; set; }
        public Guid TermId { get; set; }
        public Guid AcademicSessionId { get; set; }
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalPaidSoFar { get; set; }
        public decimal TotalBalanceRemaining { get; set; }
        public List<FeeItemBreakdownDto> FeeItems { get; set; } = new List<FeeItemBreakdownDto>();
        public List<BookItemBreakdownDto> AvailableBooks { get; set; }= new List<BookItemBreakdownDto>();   
        public ICollection<PaymentHistoryDto>? PaymentHistory { get; set; }
        public decimal MinimumAmountRequired { get;  set; }
    }
}
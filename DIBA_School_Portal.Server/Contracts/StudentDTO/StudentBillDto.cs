using DIBA_School_Portal.Server.Contracts.FinanceDto;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentBillDto
    {
        public string TermName { get; set; } = "";
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalPaidSoFar { get; set; }
        public decimal TotalBalanceRemaining { get; set; }
        public List<FeeItemBreakdownDto> FeeItems { get; set; } = new();
        public List<PaymentHistoryDto> PaymentHistory { get; set; } = new();
    }
}

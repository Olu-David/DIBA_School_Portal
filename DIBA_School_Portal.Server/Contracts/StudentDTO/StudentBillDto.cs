using DIBA_School_Portal.Server.Contracts.FinanceDto;

namespace DIBA_School_Portal.Server.Contracts.StudentDTO
{
    public class StudentBillDto
    {
        public Guid StudentProfileId { get; set; }          // ← must exist
        public Guid AcademicSessionId { get; set; }         // ← must exist
        public Guid TermId { get; set; }                    // ← must exist
        public string TermName { get; set; } = "";
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalPaidSoFar { get; set; }
        public decimal TotalBalanceRemaining { get; set; }
        public decimal MinimumAmountRequired { get; set; }  // ← must exist
        public List<FeeItemBreakdownDto> FeeItems { get; set; } = new();
        public List<PaymentHistoryDto> PaymentHistory { get; set; } = new();
    }
    }

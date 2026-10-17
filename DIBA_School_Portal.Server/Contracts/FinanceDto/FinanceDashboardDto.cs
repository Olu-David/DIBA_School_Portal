using DIBA_School_Portal.Server.Contracts.ClassDto;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class FinanceDashboardDto
    {
        public Guid AcademicSessionId { get; set; }
        public string AcademicSessionName { get; set; } = string.Empty;
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // ---- Expected vs collected ----
        public decimal TotalExpected { get; set; }       // total fees billed
        public decimal TotalCollected { get; set; }      // all successful payments
        public decimal TotalOutstanding { get; set; }    // expected - collected
        public decimal CollectionRate { get; set; }      // percentage

        // ---- Breakdown by gateway ----
        public decimal TotalOnline { get; set; }         // Flutterwave + Paystack
        public decimal TotalCash { get; set; }           // Cash
        public decimal TotalBankTransfer { get; set; }   // BankTransfer
        public decimal TotalManual { get; set; }         // Manual

        // ---- Student stats ----
        public int TotalStudents { get; set; }
        public int FullyPaidCount { get; set; }
        public int PartiallyPaidCount { get; set; }
        public int UnpaidCount { get; set; }

        // ---- Payment stats ----
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransactions { get; set; }

        // ---- Per-class breakdown ----
        public List<ClassFinanceSummaryDto> ClassBreakdown { get; set; } = new();

        // ---- Per-gateway breakdown ----
        public List<GatewaySummaryDto> GatewayBreakdown { get; set; } = new();

        // ---- Recent transactions ----
        public List<FinancePaymentRowDto> RecentPayments { get; set; } = new();

        public decimal TotalMiscIncome { get; set; }
        public int MiscIncomeTransactionCount { get; set; }

        /// <summary>Fee collections + misc income.</summary>
        public decimal GrandTotalInflow { get; set; }

        public List<MiscIncomeCategorySummaryDto> MiscIncomeByCategory { get; set; } = new();
    }
}

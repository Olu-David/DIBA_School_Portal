using DIBA_School_Portal.Server.Contracts.ClassDto;

namespace DIBA_School_Portal.Server.Contracts.FinanceDto
{
    public class FinanceDashboardDto
    {

        public Guid AcademicSessionId { get; set; }
        public string AcademicSessionName { get; set; } = "";
        public Guid TermId { get; set; }
        public string TermName { get; set; } = "";

        // ---------- Fee collection ----------
        public decimal TotalExpected { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal CollectionRate { get; set; }

        // ---------- Fee payment methods ----------
        public decimal TotalOnline { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalBankTransfer { get; set; }
        public decimal TotalManual { get; set; }

        // ---------- Student fee status ----------
        public int TotalStudents { get; set; }
        public int FullyPaidCount { get; set; }
        public int PartiallyPaidCount { get; set; }
        public int UnpaidCount { get; set; }

        // ---------- Fee transaction counts ----------
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int FailedTransactions { get; set; }

        // ---------- Breakdowns ----------
        public List<ClassFinanceSummaryDto> ClassBreakdown { get; set; } = new();
        public List<GatewaySummaryDto> GatewayBreakdown { get; set; } = new();
        public List<FinancePaymentRowDto> RecentPayments { get; set; } = new();

        // ---------- Misc income ----------
        public decimal TotalMiscIncome { get; set; }
        public int MiscIncomeTransactionCount { get; set; }
        public List<MiscIncomeCategorySummaryDto> MiscIncomeByCategory { get; set; } = new();

        // ---------- INCOME TOTAL ----------
        public decimal TotalIncome { get; set; }

        // ---------- EXPENSES (👈 NEW) ----------
        public decimal TotalExpenses { get; set; }
        public int ExpenseTransactionCount { get; set; }
        public List<ExpenseCategorySummaryDto> ExpensesByCategory { get; set; } = new();
        public List<ExpenseGatewaySummaryDto> ExpensesByGateway { get; set; } = new();

        // ---------- NET INFLOW ----------
        public decimal NetInflow { get; set; }

        // ---------- Legacy ----------
        public decimal GrandTotalInflow { get; set; }
    }
}

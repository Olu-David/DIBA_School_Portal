export interface FinancePaymentRow {
  id: string;
  studentName: string;
  admissionNumber: string;
  referenceNumber: string;
  gateway: string;
  gatewayName?: string;
  status: string;
  statusName?: string;
  paymentDate: string;
  amountPaid: number;
}

export interface FinanceRecentPayment {
  id: string;
  studentName: string;
  referenceNumber: string;
  amountPaid: number;
  paymentDate: string;
}

export interface FinanceClassBreakdown {
  classId: string;
  className: string;
  studentCount: number;
  expected: number;
  collected: number;
  collectionRate: number;
}

// 👇 NEW — matches backend MiscIncomeCategorySummaryDto
export interface MiscIncomeCategorySummary {
  category: string;
  count: number;
  totalAmount: number;
}

export interface FinanceDashboard {
  academicSessionId: string;
  academicSessionName: string;
  termId: string;
  termName: string;
  totalStudents: number;
  totalExpected: number;
  totalCollected: number;
  totalOutstanding: number;
  fullyPaidCount: number;
  unpaidCount: number;
  partiallyPaidCount: number;
  collectionRate: number;
  totalCash: number;
  totalOnline: number;
  totalBankTransfer: number;
  totalManual: number;
  recentPayments: FinanceRecentPayment[];
  classBreakdown: FinanceClassBreakdown[];

  totalMiscIncome: number;
  miscIncomeTransactionCount: number;
  grandTotalInflow: number;
  miscIncomeByCategory: MiscIncomeCategorySummary[];

  totalIncome: number;
  totalExpenses: number;
  expenseTransactionCount: number;
  netInflow: number;
  expensesByCategory: {
    category: string;
    count: number;
    totalAmount: number;
  }[];
  expensesByGateway: {
    gateway: number;
    gatewayName: string;
    count: number;
    totalAmount: number;
  }[];
}
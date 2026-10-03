export interface FinanceDashboard {
  academicSessionId: string;
  academicSessionName: string;
  termId: string;
  termName: string;
  generatedAt: string;
  totalExpected: number;
  totalCollected: number;
  totalOutstanding: number;
  collectionRate: number;
  totalOnline: number;
  totalCash: number;
  totalBankTransfer: number;
  totalManual: number;
  totalStudents: number;
  fullyPaidCount: number;
  partiallyPaidCount: number;
  unpaidCount: number;
  totalTransactions: number;
  successfulTransactions: number;
  pendingTransactions: number;
  failedTransactions: number;
  classBreakdown: ClassFinanceSummary[];
  gatewayBreakdown: GatewaySummary[];
  recentPayments: FinancePaymentRow[];
}

export interface ClassFinanceSummary {
  classId: string;
  className: string;
  studentCount: number;
  expected: number;
  collected: number;
  outstanding: number;
  collectionRate: number;
  fullyPaidCount: number;
  unpaidCount: number;
}

export interface GatewaySummary {
  gateway: string;
  gatewayName: string;
  transactionCount: number;
  totalAmount: number;
  percentage: number;
}

export interface FinancePaymentRow {
  id: string;
  referenceNumber: string;
  studentName: string;
  admissionNumber: string;
  className: string;
  amountPaid: number;
  gateway: string;
  gatewayName: string;
  status: string;
  statusName: string;
  paymentDate: string;
  recordedBy: string | null;
  note: string | null;
}

export interface ArrearsRow {
  studentProfileId: string;
  studentName: string;
  admissionNumber: string;
  className: string;
  guardianPhone: string;
  expected: number;
  paid: number;
  outstanding: number;
  isFullyPaid: boolean;
}export interface FinanceDashboard {
  academicSessionId: string;
  academicSessionName: string;
  termId: string;
  termName: string;
  generatedAt: string;
  totalExpected: number;
  totalCollected: number;
  totalOutstanding: number;
  collectionRate: number;
  totalOnline: number;
  totalCash: number;
  totalBankTransfer: number;
  totalManual: number;
  totalStudents: number;
  fullyPaidCount: number;
  partiallyPaidCount: number;
  unpaidCount: number;
  totalTransactions: number;
  successfulTransactions: number;
  pendingTransactions: number;
  failedTransactions: number;
  classBreakdown: ClassFinanceSummary[];
  gatewayBreakdown: GatewaySummary[];
  recentPayments: FinancePaymentRow[];
}

export interface ClassFinanceSummary {
  classId: string;
  className: string;
  studentCount: number;
  expected: number;
  collected: number;
  outstanding: number;
  collectionRate: number;
  fullyPaidCount: number;
  unpaidCount: number;
}

export interface GatewaySummary {
  gateway: string;
  gatewayName: string;
  transactionCount: number;
  totalAmount: number;
  percentage: number;
}

export interface FinancePaymentRow {
  id: string;
  referenceNumber: string;
  studentName: string;
  admissionNumber: string;
  className: string;
  amountPaid: number;
  gateway: string;
  gatewayName: string;
  status: string;
  statusName: string;
  paymentDate: string;
  recordedBy: string | null;
  note: string | null;
}

export interface ArrearsRow {
  studentProfileId: string;
  studentName: string;
  admissionNumber: string;
  className: string;
  guardianPhone: string;
  expected: number;
  paid: number;
  outstanding: number;
  isFullyPaid: boolean;
}
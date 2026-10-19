export interface ExpenseRow {
  id: string;
  category: string;
  description: string;
  amount: number;
  paymentGateway: number;
  gatewayName: string;
  vendor: string | null;
  referenceNumber: string;
  expenseDate: string;
  receiptUrl: string | null;
  notes: string | null;
  recordedByName: string | null;
  createdAt: string;
}

export interface ExpenseDashboard {
  totalExpenses: number;
  totalTransactions: number;
  byCategory: {
    category: string;
    count: number;
    totalAmount: number;
  }[];
}

export interface ExpenseFilter {
  pageNumber?: number;
  pageSize?: number;
  category?: string;
  paymentGateway?: number;
  fromDate?: string;
  toDate?: string;
  searchTerm?: string;
  academicSessionId?: string;
  termId?: string;
}

export interface CreateExpensePayload {
  category: string;
  description: string;
  amount: number;
  paymentGateway: number;
  vendor?: string | null;
  expenseDate?: string;
  notes?: string | null;
  academicSessionId?: string | null;
  termId?: string | null;
}

export const PAYMENT_GATEWAYS = [
  { value: 0, label: "Cash" },
  { value: 1, label: "Bank Transfer" },
  { value: 4, label: "Manual" },
  { value: 5, label: "POS" },
] as const;

export const PAYMENT_GATEWAY_LABEL = (n: number) =>
  PAYMENT_GATEWAYS.find((g) => g.value === n)?.label ?? "—";

export const EXPENSE_CATEGORY_SUGGESTIONS = [
  "Salaries",
  "Utilities",
  "Rent",
  "Supplies",
  "Maintenance",
  "Transport",
  "Food",
  "Events",
  "Marketing",
  "Training",
  "Furniture",
  "Equipment",
  "Medical",
  "Insurance",
  "Statutory",
  "Bank Charges",
  "Professional Fees",
  "Refund",
  "Other",
];
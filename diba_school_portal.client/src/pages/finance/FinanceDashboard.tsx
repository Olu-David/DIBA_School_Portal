import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Wallet, TrendingUp, TrendingDown, Users, BadgeDollarSign,
  CreditCard, Banknote, ArrowUpRight, PieChart as PieIcon,
  AlertCircle, RefreshCw, Download, Coins, Plus,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, StatCard, Skeleton, Badge } from "../../components/ui";
import { formatNaira, formatNairaShort, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";
import type { FinanceDashboard as FD } from "../../types/Finance";

interface SessionItem {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

export default function FinanceDashboard() {
  const navigate = useNavigate();

  const [data, setData] = useState<FD | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    setData(null);

    try {
      const { data: sres } = await api.get<
        ApiResponse<Paginated<SessionItem>>
      >("/academicsession/all?pageNumber=1&pageSize=20");

      if (!sres.success) {
        setError(sres.message ?? "Could not load academic sessions.");
        return;
      }

      const sessions = sres.data?.items ?? [];
      if (sessions.length === 0) {
        setError("No academic sessions found.");
        return;
      }

      const current =
        sessions.find((s) => s.isCurrentSession) ?? sessions[0];

      const term =
        current.terms?.find((t) => t.isCurrentTerm) ?? current.terms?.[0];

      if (!term) {
        setError("No current term found for this session.");
        return;
      }

      const { data: dres } = await api.get<ApiResponse<FD>>(
        "/account/dashboard",
        { params: { academicSessionId: current.id, termId: term.id } }
      );

      if (!dres.success || !dres.data) {
        setError(dres.message ?? "Could not load finance dashboard.");
        return;
      }

      setData(dres.data);
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      const message =
        e2?.response?.data?.message ??
        e2?.message ??
        "Unknown error loading dashboard.";

      console.error("[FinanceDashboard] Error:", status, message, err);

      if (status === 403) {
        setError("You don't have permission to view financial data.");
      } else if (status === 404) {
        setError("Finance endpoint not found (404).");
      } else if (status === 502) {
        setError(
          "The proxy couldn't reach the API (502). Check the API is running and the Vite proxy target is correct."
        );
      } else {
        setError(message);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /* ==========================================================
     EXPORT — Professional CSV financial statement
     ========================================================== */
  const handleExport = () => {
    if (!data) return;
    setExporting(true);
    try {
      const now = new Date();
      const dateStamp = now.toISOString().slice(0, 10);
      const timeStamp = now.toISOString().slice(11, 19);
      const reportRef = `DIBA/FIN/${dateStamp.replace(/-/g, "")}/${timeStamp.replace(/:/g, "")}`;

      const esc = (v: string | number | null | undefined): string => {
        if (v === null || v === undefined) return "";
        const s = String(v);
        if (s.includes(",") || s.includes('"') || s.includes("\n")) {
          return `"${s.replace(/"/g, '""')}"`;
        }
        return s;
      };

      const money = (n: number): string => (Number(n) || 0).toFixed(2);
      const pct = (n: number): string => `${(Number(n) || 0).toFixed(2)}%`;
      const row = (...cols: (string | number | null | undefined)[]): string =>
        cols.map(esc).join(",");

      const lines: string[] = [];

      // ---------- 1. REPORT HEADER ----------
      lines.push(row("DIBA SCHOOL PORTAL"));
      lines.push(row("FINANCIAL STATEMENT — TERM SUMMARY"));
      lines.push(row(""));
      lines.push(row("Report Reference", reportRef));
      lines.push(row("Academic Session", data.academicSessionName));
      lines.push(row("Term", data.termName));
      lines.push(row("Generated", `${dateStamp} ${timeStamp} UTC`));
      lines.push(row("Currency", "NGN (Nigerian Naira)"));
      lines.push(row(""));

      // ---------- 2. EXECUTIVE SUMMARY ----------
      lines.push(row("EXECUTIVE SUMMARY"));
      lines.push(row(""));
      lines.push(row("Item", "Amount (NGN)", "Notes"));
      lines.push(row("Fees Collected", money(data.totalCollected), "Tuition & related fees only"));
      lines.push(row("Other Income", money(data.totalMiscIncome), "Uniforms, fines, donations, PTA, etc."));
      lines.push(row("TOTAL INCOME", money(data.totalIncome), "Fees + other income"));
      lines.push(row("TOTAL EXPENSES", money(data.totalExpenses), "Operational costs"));
      lines.push(row("NET INFLOW", money(data.netInflow), data.netInflow >= 0 ? "Surplus" : "Deficit"));
      lines.push(row(""));

      // ---------- 3. FEE COLLECTION ----------
      lines.push(row("FEE COLLECTION SUMMARY"));
      lines.push(row(""));
      lines.push(row("Metric", "Value"));
      lines.push(row("Total Expected", money(data.totalExpected)));
      lines.push(row("Total Collected", money(data.totalCollected)));
      lines.push(row("Total Outstanding", money(data.totalOutstanding)));
      lines.push(row("Collection Rate", pct(data.collectionRate)));
      lines.push(row(""));
      lines.push(row("Student Status", "Count", "Percentage"));
      const total = Math.max(1, data.totalStudents);
      lines.push(row("Total Students", data.totalStudents, "100.00%"));
      lines.push(row("Fully Paid", data.fullyPaidCount, pct((data.fullyPaidCount / total) * 100)));
      lines.push(row("Partially Paid", data.partiallyPaidCount, pct((data.partiallyPaidCount / total) * 100)));
      lines.push(row("Unpaid", data.unpaidCount, pct((data.unpaidCount / total) * 100)));
      lines.push(row(""));

      // ---------- 4. FEE PAYMENT METHODS ----------
      lines.push(row("FEE PAYMENT METHODS"));
      lines.push(row(""));
      lines.push(row("Method", "Amount (NGN)", "% of Collected"));
      const collPct = (v: number) =>
        data.totalCollected > 0 ? pct((v / data.totalCollected) * 100) : "0.00%";
      lines.push(row("Cash", money(data.totalCash), collPct(data.totalCash)));
      lines.push(row("Online (Flutterwave / Paystack)", money(data.totalOnline), collPct(data.totalOnline)));
      lines.push(row("Bank Transfer", money(data.totalBankTransfer), collPct(data.totalBankTransfer)));
      lines.push(row("Manual", money(data.totalManual), collPct(data.totalManual)));
      lines.push(row("TOTAL", money(data.totalCollected), "100.00%"));
      lines.push(row(""));

      // ---------- 5. FEE COLLECTION BY CLASS ----------
      lines.push(row("FEE COLLECTION BY CLASS"));
      lines.push(row(""));
      lines.push(row("Class", "Students", "Expected (NGN)", "Collected (NGN)", "Outstanding (NGN)", "Rate"));
      if (data.classBreakdown.length === 0) {
        lines.push(row("(no classes with enrolments)", "", "", "", "", ""));
      } else {
        data.classBreakdown.forEach((c) => {
          lines.push(
            row(
              c.className,
              c.studentCount,
              money(c.expected),
              money(c.collected),
              money(c.outstanding),
              pct(c.collectionRate)
            )
          );
        });
        const totalStudents = data.classBreakdown.reduce((s, c) => s + c.studentCount, 0);
        const totalExpected = data.classBreakdown.reduce((s, c) => s + c.expected, 0);
        const totalCollectedCls = data.classBreakdown.reduce((s, c) => s + c.collected, 0);
        const totalOutstandingCls = data.classBreakdown.reduce((s, c) => s + c.outstanding, 0);
        lines.push(
          row(
            "TOTAL",
            totalStudents,
            money(totalExpected),
            money(totalCollectedCls),
            money(totalOutstandingCls),
            pct(data.collectionRate)
          )
        );
      }
      lines.push(row(""));

      // ---------- 6. OTHER INCOME ----------
      lines.push(row("OTHER INCOME (NON-FEE)"));
      lines.push(row(""));
      lines.push(row("Category", "Transactions", "Amount (NGN)", "% of Other Income"));
      if (!data.miscIncomeByCategory || data.miscIncomeByCategory.length === 0) {
        lines.push(row("(none recorded this term)", 0, "0.00", "0.00%"));
      } else {
        data.miscIncomeByCategory.forEach((c) => {
          const share = data.totalMiscIncome > 0 ? (c.totalAmount / data.totalMiscIncome) * 100 : 0;
          lines.push(row(c.category, c.count, money(c.totalAmount), pct(share)));
        });
      }
      lines.push(row("TOTAL", data.miscIncomeTransactionCount, money(data.totalMiscIncome), "100.00%"));
      lines.push(row(""));

      // ---------- 7. EXPENSES BY CATEGORY ----------
      lines.push(row("EXPENSES BY CATEGORY"));
      lines.push(row(""));
      lines.push(row("Category", "Transactions", "Amount (NGN)", "% of Expenses"));
      if (!data.expensesByCategory || data.expensesByCategory.length === 0) {
        lines.push(row("(none recorded this term)", 0, "0.00", "0.00%"));
      } else {
        data.expensesByCategory.forEach((c) => {
          const share = data.totalExpenses > 0 ? (c.totalAmount / data.totalExpenses) * 100 : 0;
          lines.push(row(c.category, c.count, money(c.totalAmount), pct(share)));
        });
      }
      lines.push(row("TOTAL", data.expenseTransactionCount, money(data.totalExpenses), "100.00%"));
      lines.push(row(""));

      // ---------- 8. EXPENSES BY PAYMENT METHOD ----------
      lines.push(row("EXPENSES BY PAYMENT METHOD"));
      lines.push(row(""));
      lines.push(row("Method", "Transactions", "Amount (NGN)", "% of Expenses"));
      if (!data.expensesByGateway || data.expensesByGateway.length === 0) {
        lines.push(row("(none recorded this term)", 0, "0.00", "0.00%"));
      } else {
        data.expensesByGateway.forEach((g) => {
          const share = data.totalExpenses > 0 ? (g.totalAmount / data.totalExpenses) * 100 : 0;
          lines.push(row(g.gatewayName, g.count, money(g.totalAmount), pct(share)));
        });
      }
      lines.push(row("TOTAL", data.expenseTransactionCount, money(data.totalExpenses), "100.00%"));
      lines.push(row(""));

      // ---------- 9. RECENT FEE PAYMENTS ----------
      lines.push(row("RECENT FEE PAYMENTS (LAST 10)"));
      lines.push(row(""));
      lines.push(row("Student", "Admission No.", "Reference", "Amount (NGN)", "Method", "Status", "Date"));
      if (data.recentPayments.length === 0) {
        lines.push(row("(no payments recorded this term)", "", "", "", "", "", ""));
      } else {
        data.recentPayments.forEach((p) => {
          lines.push(
            row(
              p.studentName,
              p.admissionNumber,
              p.referenceNumber,
              money(p.amountPaid),
              p.gateway,
              p.status,
              p.paymentDate
            )
          );
        });
      }
      lines.push(row(""));

      // ---------- 10. SIGN-OFF BLOCK ----------
      lines.push(row(""));
      lines.push(row("PREPARED BY"));
      lines.push(row(""));
      lines.push(row("Name", "_______________________________"));
      lines.push(row("Role", "Bursar / Finance Officer"));
      lines.push(row("Signature", "_______________________________"));
      lines.push(row("Date", "_______________________________"));
      lines.push(row(""));
      lines.push(row("REVIEWED BY"));
      lines.push(row(""));
      lines.push(row("Name", "_______________________________"));
      lines.push(row("Role", "Principal / Head of School"));
      lines.push(row("Signature", "_______________________________"));
      lines.push(row("Date", "_______________________________"));
      lines.push(row(""));

      // ---------- 11. FOOTNOTES ----------
      lines.push(row("NOTES"));
      lines.push(row(""));
      lines.push(row("1. All amounts are in Nigerian Naira (NGN) and rounded to 2 decimal places."));
      lines.push(row("2. Net Inflow = Fees Collected + Other Income - Total Expenses."));
      lines.push(row("3. Other Income is non-fee revenue (uniforms, fines, donations, PTA, etc.) and does NOT affect student enrollment balances."));
      lines.push(row("4. Outstanding Fees = Total Expected - Total Collected (as of report generation time)."));
      lines.push(row("5. This report was generated from the DIBA School Portal and is subject to reconciliation with physical records."));
      lines.push(row(`6. Report Reference: ${reportRef}`));
      lines.push(row(""));

      const csv = lines.join("\r\n");
      const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `DIBA_Financial_Statement_${data.academicSessionName.replace(
        /[\\/]/g,
        "-"
      )}_${data.termName.replace(/[\\/ ]/g, "_")}_${dateStamp}.csv`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } finally {
      setExporting(false);
    }
  };

  /* ==========================================================
     NAVIGATION
     ========================================================== */
  const handleRecordCash = () => {
    navigate("/admin/finance/record-cash");
  };

  const handleRecordExpense = () => {
    navigate("/admin/finance/record-expense");
  };

  /* ==========================================================
     LOADING
     ========================================================== */
  if (loading) {
    return (
      <div className="space-y-6">
        <PageHeader eyebrow="Finance" title="Financial overview" />
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <Card key={i}>
              <Skeleton className="w-10 h-10 rounded-xl mb-5" />
              <Skeleton className="h-3 w-24 mb-2" />
              <Skeleton className="h-6 w-32" />
            </Card>
          ))}
        </div>
      </div>
    );
  }

  /* ==========================================================
     ERROR
     ========================================================== */
  if (error || !data) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Finance"
          title="Financial overview"
          actions={
            <Button variant="secondary" icon={RefreshCw} onClick={load}>
              Retry
            </Button>
          }
        />
        <Card>
          <div className="flex items-start gap-3 py-6">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">
                Couldn't load financial data
              </p>
              <p className="text-[13px] text-red-800/80 mt-1">
                {error ?? "Unknown error."}
              </p>
              <p className="text-[12px] text-surface-500 mt-3">
                Open the browser console for the full response.
              </p>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  /* ==========================================================
     DERIVED
     ========================================================== */
  const collectionPercent = Math.min(100, Math.round(data.collectionRate));
  const netPercentOfIncome =
    data.totalIncome > 0
      ? Math.round((data.netInflow / data.totalIncome) * 100)
      : 0;
  const expenseShareOfIncome =
    data.totalIncome > 0
      ? Math.round((data.totalExpenses / data.totalIncome) * 100)
      : 0;

  /* ==========================================================
     RENDER
     ========================================================== */
  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow={`${data.academicSessionName} - ${data.termName}`}
        title="Financial overview"
        description="Live collections, outstanding fees, non-fee income, expenses, and net inflow for the current term."
        actions={
          <>
            <Button
              variant="secondary"
              icon={Download}
              onClick={handleExport}
              loading={exporting}
            >
              Export report
            </Button>
            <Button
              variant="secondary"
              icon={TrendingDown}
              onClick={handleRecordExpense}
            >
              Record expense
            </Button>
            <Button icon={BadgeDollarSign} onClick={handleRecordCash}>
              Record cash
            </Button>
          </>
        }
      />

      {/* ---------- Stat strip ---------- */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 lg:gap-6">
        <StatCard
          label="Total collected"
          value={formatNairaShort(data.totalCollected)}
          hint={`${collectionPercent}% of expected`}
          icon={TrendingUp}
          tone="emerald"
        />
        <StatCard
          label="Outstanding"
          value={formatNairaShort(data.totalOutstanding)}
          hint={`${data.unpaidCount + data.partiallyPaidCount} student(s) owing`}
          icon={TrendingDown}
          tone="rose"
        />
        <StatCard
          label="Total expenses"
          value={formatNairaShort(data.totalExpenses)}
          hint={`${data.expenseTransactionCount} transaction(s)`}
          icon={Coins}
          tone="amber"
        />
        <StatCard
          label="Net inflow"
          value={formatNairaShort(data.netInflow)}
          hint={
            data.totalIncome > 0
              ? `${netPercentOfIncome}% of income`
              : "—"
          }
          icon={Wallet}
          tone={data.netInflow >= 0 ? "emerald" : "rose"}
        />
      </div>

      {/* ---------- Second stat strip ---------- */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 lg:gap-6">
        <StatCard
          label="Expected this term"
          value={formatNairaShort(data.totalExpected)}
          hint={`${data.totalStudents} enrolled`}
          icon={Wallet}
          tone="sky"
        />
        <StatCard
          label="Fully paid"
          value={`${data.fullyPaidCount} / ${data.totalStudents}`}
          hint={`${Math.round(
            (data.fullyPaidCount / Math.max(1, data.totalStudents)) * 100
          )}% cleared`}
          icon={Users}
          tone="violet"
        />
        <StatCard
          label="Other income"
          value={formatNairaShort(data.totalMiscIncome)}
          hint={`${data.miscIncomeTransactionCount} transaction(s)`}
          icon={Coins}
          tone="sky"
        />
        <StatCard
          label="Total income"
          value={formatNairaShort(data.totalIncome)}
          hint="Fees + other income"
          icon={TrendingUp}
          tone="emerald"
        />
      </div>

      {/* ---------- Collection progress ---------- */}
      <Card>
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-[15px] font-semibold text-surface-900">
              Collection progress
            </h3>
            <p className="text-[12px] text-surface-400 mt-0.5">
              {formatNaira(data.totalCollected)} of{" "}
              {formatNaira(data.totalExpected)}
            </p>
          </div>
          <span className="text-2xl font-semibold text-brand-600 tabular-nums">
            {collectionPercent}%
          </span>
        </div>
        <div className="h-3 bg-surface-100 rounded-full overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-brand-400 to-brand-600 rounded-full transition-all"
            style={{ width: `${collectionPercent}%` }}
          />
        </div>
      </Card>

      {/* ---------- Income vs Expenses ---------- */}
      <Card>
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-[15px] font-semibold text-surface-900">
              Income vs Expenses
            </h3>
            <p className="text-[12px] text-surface-400 mt-0.5">
              Fees + other income − expenses = net
            </p>
          </div>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="rounded-xl bg-emerald-50/60 border border-emerald-100 p-4">
            <p className="text-[10px] uppercase tracking-wider text-emerald-700 font-medium">
              Total income
            </p>
            <p className="text-xl font-semibold text-emerald-700 tabular-nums mt-1">
              {formatNairaShort(data.totalIncome)}
            </p>
            <p className="text-[11px] text-emerald-600/80 mt-1">
              {formatNaira(data.totalCollected)} fees +{" "}
              {formatNaira(data.totalMiscIncome)} other
            </p>
          </div>
          <div className="rounded-xl bg-rose-50/60 border border-rose-100 p-4">
            <p className="text-[10px] uppercase tracking-wider text-rose-700 font-medium">
              Total expenses
            </p>
            <p className="text-xl font-semibold text-rose-700 tabular-nums mt-1">
              -{formatNairaShort(data.totalExpenses)}
            </p>
            <p className="text-[11px] text-rose-600/80 mt-1">
              {expenseShareOfIncome}% of income
            </p>
          </div>
          <div
            className={`rounded-xl border p-4 ${
              data.netInflow >= 0
                ? "bg-brand-50/60 border-brand-100"
                : "bg-rose-50/60 border-rose-100"
            }`}
          >
            <p
              className={`text-[10px] uppercase tracking-wider font-medium ${
                data.netInflow >= 0 ? "text-brand-700" : "text-rose-700"
              }`}
            >
              Net inflow
            </p>
            <p
              className={`text-xl font-semibold tabular-nums mt-1 ${
                data.netInflow >= 0 ? "text-brand-700" : "text-rose-700"
              }`}
            >
              {formatNairaShort(data.netInflow)}
            </p>
            <p
              className={`text-[11px] mt-1 ${
                data.netInflow >= 0 ? "text-brand-600/80" : "text-rose-600/80"
              }`}
            >
              {netPercentOfIncome}% of income retained
            </p>
          </div>
        </div>
      </Card>

      {/* ---------- Payment methods + Other income ---------- */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <div className="flex items-center gap-2 mb-5">
            <PieIcon className="w-4 h-4 text-surface-500" />
            <h3 className="text-[15px] font-semibold text-surface-900">
              Payment methods (fees)
            </h3>
          </div>
          <div className="space-y-4">
            <ChannelBar
              icon={Banknote}
              label="Cash"
              amount={data.totalCash}
              total={data.totalCollected}
              tone="emerald"
            />
            <ChannelBar
              icon={CreditCard}
              label="Online"
              amount={data.totalOnline}
              total={data.totalCollected}
              tone="sky"
            />
            <ChannelBar
              icon={TrendingUp}
              label="Bank transfer"
              amount={data.totalBankTransfer}
              total={data.totalCollected}
              tone="violet"
            />
            <ChannelBar
              icon={Wallet}
              label="Manual"
              amount={data.totalManual}
              total={data.totalCollected}
              tone="amber"
            />
          </div>
        </Card>

        <Card>
          <div className="flex items-center gap-2 mb-5">
            <Coins className="w-4 h-4 text-surface-500" />
            <h3 className="text-[15px] font-semibold text-surface-900">
              Other income
            </h3>
          </div>

          {!data.miscIncomeByCategory ||
          data.miscIncomeByCategory.length === 0 ? (
            <div className="py-8 text-center text-[13px] text-surface-400">
              No non-fee income recorded this term.
            </div>
          ) : (
            <div className="space-y-4">
              {data.miscIncomeByCategory.map((c) => (
                <div key={c.category}>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-[12px] font-medium text-surface-700">
                      {c.category}{" "}
                      <span className="text-surface-400">· {c.count}</span>
                    </span>
                    <span className="text-[12px] font-semibold text-surface-900 tabular-nums">
                      {formatNairaShort(c.totalAmount)}
                    </span>
                  </div>
                  <div className="h-1.5 bg-surface-100 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-amber-500 rounded-full transition-all"
                      style={{
                        width: `${
                          data.totalMiscIncome > 0
                            ? Math.round(
                                (c.totalAmount / data.totalMiscIncome) * 100
                              )
                            : 0
                        }%`,
                      }}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      {/* ---------- Expenses by category + payment method ---------- */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <div className="flex items-center justify-between mb-5">
            <div className="flex items-center gap-2">
              <TrendingDown className="w-4 h-4 text-surface-500" />
              <h3 className="text-[15px] font-semibold text-surface-900">
                Expenses by category
              </h3>
            </div>
            <Button
              variant="ghost"
              size="sm"
              iconRight={ArrowUpRight}
              onClick={() => navigate("/admin/finance/expenses")}
            >
              View all
            </Button>
          </div>

          {!data.expensesByCategory ||
          data.expensesByCategory.length === 0 ? (
            <div className="py-8 text-center text-[13px] text-surface-400">
              No expenses recorded this term.
            </div>
          ) : (
            <div className="space-y-4">
              {data.expensesByCategory.slice(0, 8).map((c) => (
                <div key={c.category}>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-[12px] font-medium text-surface-700">
                      {c.category}{" "}
                      <span className="text-surface-400">· {c.count}</span>
                    </span>
                    <span className="text-[12px] font-semibold text-rose-600 tabular-nums">
                      -{formatNairaShort(c.totalAmount)}
                    </span>
                  </div>
                  <div className="h-1.5 bg-surface-100 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-rose-500 rounded-full transition-all"
                      style={{
                        width: `${
                          data.totalExpenses > 0
                            ? Math.round(
                                (c.totalAmount / data.totalExpenses) * 100
                              )
                            : 0
                        }%`,
                      }}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-5">
            <div className="flex items-center gap-2">
              <PieIcon className="w-4 h-4 text-surface-500" />
              <h3 className="text-[15px] font-semibold text-surface-900">
                Expenses by payment method
              </h3>
            </div>
          </div>

          {!data.expensesByGateway ||
          data.expensesByGateway.length === 0 ? (
            <div className="py-8 text-center text-[13px] text-surface-400">
              No expenses recorded this term.
            </div>
          ) : (
            <div className="space-y-4">
              {data.expensesByGateway.map((g) => (
                <div key={g.gateway}>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-[12px] font-medium text-surface-700">
                      {g.gatewayName}{" "}
                      <span className="text-surface-400">· {g.count}</span>
                    </span>
                    <span className="text-[12px] font-semibold text-surface-900 tabular-nums">
                      {formatNairaShort(g.totalAmount)}
                    </span>
                  </div>
                  <div className="h-1.5 bg-surface-100 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-slate-500 rounded-full transition-all"
                      style={{
                        width: `${
                          data.totalExpenses > 0
                            ? Math.round(
                                (g.totalAmount / data.totalExpenses) * 100
                              )
                            : 0
                        }%`,
                      }}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      {/* ---------- Recent fee payments ---------- */}
      <Card padding="none">
        <div className="p-6 pb-4 flex items-center justify-between">
          <div>
            <h3 className="text-[15px] font-semibold text-surface-900">
              Recent fee payments
            </h3>
            <p className="text-[12px] text-surface-400 mt-0.5">
              Last {data.recentPayments.length} transactions
            </p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            iconRight={ArrowUpRight}
            onClick={() => navigate("/admin/finance/payments")}
          >
            View all
          </Button>
        </div>

        <div className="divide-y divide-surface-100 max-h-[420px] overflow-y-auto">
          {data.recentPayments.map((p) => (
            <div
              key={p.id}
              className="px-6 py-3.5 flex items-center gap-4 hover:bg-surface-50/50"
            >
              <div className="w-9 h-9 rounded-xl bg-brand-50 grid place-items-center shrink-0">
                <BadgeDollarSign className="w-4 h-4 text-brand-600" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-[13px] font-medium text-surface-800 truncate">
                  {p.studentName}
                </p>
                <p className="text-[11px] text-surface-400 font-mono truncate">
                  {p.referenceNumber}
                </p>
              </div>
              <div className="text-right shrink-0">
                <p className="text-[13px] font-semibold text-surface-900 tabular-nums">
                  {formatNaira(p.amountPaid)}
                </p>
                <p className="text-[11px] text-surface-400">
                  {formatDate(p.paymentDate)}
                </p>
              </div>
            </div>
          ))}
          {data.recentPayments.length === 0 && (
            <div className="py-12 text-center text-[13px] text-surface-400">
              No payments recorded yet this term.
            </div>
          )}
        </div>
      </Card>

      {/* ---------- Collection by class ---------- */}
      <Card padding="none">
        <div className="p-6 pb-4">
          <h3 className="text-[15px] font-semibold text-surface-900">
            Fee collection by class
          </h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-y border-surface-100 bg-surface-50/50">
                {["Class", "Students", "Expected", "Collected", "Rate"].map(
                  (h, i) => (
                    <th
                      key={h}
                      className={`text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3 ${
                        i === 0 ? "text-left" : "text-right"
                      }`}
                    >
                      {h}
                    </th>
                  )
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-100">
              {data.classBreakdown.map((c) => {
                const rate = Math.round(c.collectionRate);
                const tone: "emerald" | "amber" | "rose" =
                  rate >= 80 ? "emerald" : rate >= 50 ? "amber" : "rose";
                return (
                  <tr key={c.classId} className="hover:bg-surface-50/50">
                    <td className="px-6 py-3.5 text-[13px] font-medium text-surface-800">
                      {c.className}
                    </td>
                    <td className="px-6 py-3.5 text-right text-[13px] text-surface-600 tabular-nums">
                      {c.studentCount}
                    </td>
                    <td className="px-6 py-3.5 text-right text-[13px] text-surface-600 tabular-nums">
                      {formatNairaShort(c.expected)}
                    </td>
                    <td className="px-6 py-3.5 text-right text-[13px] font-semibold text-surface-900 tabular-nums">
                      {formatNairaShort(c.collected)}
                    </td>
                    <td className="px-6 py-3.5 text-right">
                      <Badge variant={tone}>{rate}%</Badge>
                    </td>
                  </tr>
                );
              })}
              {data.classBreakdown.length === 0 && (
                <tr>
                  <td
                    colSpan={5}
                    className="px-6 py-12 text-center text-[13px] text-surface-400"
                  >
                    No classes with enrolments yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}

/* ==========================================================
   Sub-components
   ========================================================== */
interface ChannelBarProps {
  icon: typeof Banknote;
  label: string;
  amount: number;
  total: number;
  tone: "emerald" | "sky" | "violet" | "amber";
}

const CHANNEL_TONES = {
  emerald: { fg: "text-brand-600", bg: "bg-brand-500" },
  sky: { fg: "text-sky-600", bg: "bg-sky-500" },
  violet: { fg: "text-violet-600", bg: "bg-violet-500" },
  amber: { fg: "text-flame-600", bg: "bg-flame-500" },
};

function ChannelBar({
  icon: Icon,
  label,
  amount,
  total,
  tone,
}: ChannelBarProps) {
  const pct = total > 0 ? Math.round((amount / total) * 100) : 0;
  const t = CHANNEL_TONES[tone];
  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <Icon className={`w-3.5 h-3.5 ${t.fg}`} />
          <span className="text-[12px] font-medium text-surface-700">
            {label}
          </span>
        </div>
        <span className="text-[12px] font-semibold text-surface-900 tabular-nums">
          {formatNairaShort(amount)}
        </span>
      </div>
      <div className="h-1.5 bg-surface-100 rounded-full overflow-hidden">
        <div
          className={`h-full ${t.bg} rounded-full transition-all`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}
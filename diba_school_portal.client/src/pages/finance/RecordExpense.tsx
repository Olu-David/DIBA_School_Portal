import { useEffect, useMemo, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  TrendingDown, Plus, RefreshCw, AlertCircle, Search,
  Calendar, Download, Pencil, Trash2,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Skeleton, Badge, Avatar,
  SearchInput, Select, Pagination, Input,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";
import {
  type ExpenseRow, type ExpenseDashboard,
  PAYMENT_GATEWAYS, PAYMENT_GATEWAY_LABEL,
} from "../../types/Expense";

interface SessionItem {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

export default function ExpensesPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [rows, setRows] = useState<ExpenseRow[]>([]);
  const [summary, setSummary] = useState<ExpenseDashboard | null>(null);
  const [sessions, setSessions] = useState<SessionItem[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(20);

  const [search, setSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [gatewayFilter, setGatewayFilter] = useState<string>("");

  // Load sessions once
  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<Paginated<SessionItem>>>(
        "/academicsession/all?pageNumber=1&pageSize=20"
      );
      if (data.success) {
        setSessions(data.data.items);
        const current =
          data.data.items.find((s) => s.isCurrentSession) ?? data.data.items[0];
        if (current) {
          setSessionId(current.id);
          const t =
            current.terms.find((x) => x.isCurrentTerm) ?? current.terms[0];
          if (t) setTermId(t.id);
        }
      }
    })();
  }, []);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({
        pageNumber: String(page),
        pageSize: String(pageSize),
      });
      if (search.trim()) params.set("searchTerm", search.trim());
      if (categoryFilter) params.set("category", categoryFilter);
      if (gatewayFilter) params.set("paymentGateway", gatewayFilter);
      if (sessionId) params.set("academicSessionId", sessionId);
      if (termId) params.set("termId", termId);

      const [listRes, summaryRes] = await Promise.all([
        api.get<ApiResponse<Paginated<ExpenseRow>>>(`/expense?${params}`),
        api.get<ApiResponse<ExpenseDashboard>>(
          `/expense/dashboard?academicSessionId=${sessionId}&termId=${termId}`
        ),
      ]);

      if (!listRes.data.success || !listRes.data.data) {
        setError(listRes.data.message ?? "Could not load expenses.");
        setRows([]);
        setTotal(0);
      } else {
        setRows(listRes.data.data.items ?? []);
        setTotal(listRes.data.data.totalCount ?? 0);
      }

      if (summaryRes.data.success) {
        setSummary(summaryRes.data.data);
      }
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      setError(e2?.response?.data?.message ?? e2?.message ?? "Could not load expenses.");
      setRows([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!sessionId || !termId) return;
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, search, categoryFilter, gatewayFilter, sessionId, termId]);

  const session = sessions.find((s) => s.id === sessionId);

  const handleDelete = async (id: string) => {
    if (!window.confirm("Delete this expense? This cannot be undone.")) return;
    try {
      const { data } = await api.delete<ApiResponse<boolean>>(`/expense/${id}`);
      if (data.success) {
        toast({ variant: "success", title: "Expense deleted" });
        await load();
      } else {
        toast({ variant: "error", title: data.message ?? "Delete failed" });
      }
    } catch {
      toast({ variant: "error", title: "Could not delete expense" });
    }
  };

  const exportCsv = () => {
    const lines: string[] = [];
    lines.push("DIBA School Portal — Expense Report");
    lines.push(`Session,${session?.name ?? "—"}`);
    lines.push(`Term,${session?.terms.find((t) => t.id === termId)?.name ?? "—"}`);
    lines.push(`Generated,${new Date().toISOString()}`);
    lines.push("");
    lines.push("Category,Description,Vendor,Reference,Amount,Gateway,Date,Recorded By");
    rows.forEach((r) => {
      lines.push(
        `"${r.category}","${r.description}","${r.vendor ?? ""}",` +
        `"${r.referenceNumber}",${r.amount},"${r.gatewayName}",` +
        `"${r.expenseDate.slice(0, 10)}","${r.recordedByName ?? ""}"`
      );
    });
    lines.push("");
    lines.push(`Total Rows,${rows.length}`);
    lines.push(`Total Amount,${rows.reduce((s, r) => s + r.amount, 0)}`);

    const csv = lines.join("\n");
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `Expenses_${session?.name ?? "session"}_${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Expenses"
        description="Every outgoing payment recorded this term."
        actions={
          <>
            <Button icon={Download} variant="secondary" onClick={exportCsv}>
              Export
            </Button>
            <Button
              icon={Plus}
              onClick={() => navigate("/admin/finance/record-expense")}
            >
              Record expense
            </Button>
          </>
        }
      />

      {/* Session / term / filters */}
      <Card padding="sm">
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-3">
          <Select
            value={sessionId}
            onChange={(e) => {
              setSessionId(e.target.value);
              const s = sessions.find((x) => x.id === e.target.value);
              if (s?.terms[0]) setTermId(s.terms[0].id);
              setPage(1);
            }}
            options={sessions.map((s) => ({ value: s.id, label: s.name }))}
          />
          <Select
            value={termId}
            onChange={(e) => {
              setTermId(e.target.value);
              setPage(1);
            }}
            options={session?.terms.map((t) => ({ value: t.id, label: t.name })) ?? []}
          />
          <Select
            value={gatewayFilter}
            onChange={(e) => {
              setGatewayFilter(e.target.value);
              setPage(1);
            }}
            options={[
              { value: "", label: "All payment methods" },
              ...PAYMENT_GATEWAYS.map((g) => ({
                value: String(g.value),
                label: g.label,
              })),
            ]}
          />
          <Input
            value={categoryFilter}
            onChange={(e) => {
              setCategoryFilter(e.target.value);
              setPage(1);
            }}
            placeholder="Filter by category…"
          />
        </div>
        <div className="mt-3">
          <SearchInput
            placeholder="Search description, vendor, reference…"
            value={search}
            onChange={(e: ChangeEvent<HTMLInputElement>) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>
      </Card>

      {/* Summary cards */}
      {summary && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Card padding="sm">
            <p className="text-[10px] uppercase tracking-wider text-surface-400">
              Total expenses
            </p>
            <p className="text-2xl font-semibold text-rose-600 tabular-nums mt-1">
              {formatNaira(summary.totalExpenses)}
            </p>
          </Card>
          <Card padding="sm">
            <p className="text-[10px] uppercase tracking-wider text-surface-400">
              Transactions
            </p>
            <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
              {summary.totalTransactions}
            </p>
          </Card>
          <Card padding="sm">
            <p className="text-[10px] uppercase tracking-wider text-surface-400">
              Top category
            </p>
            <p className="text-[15px] font-semibold text-surface-900 mt-1">
              {summary.byCategory[0]?.category ?? "—"}
            </p>
            <p className="text-[11px] text-surface-500 mt-0.5">
              {summary.byCategory[0]
                ? formatNaira(summary.byCategory[0].totalAmount)
                : ""}
            </p>
          </Card>
        </div>
      )}

      {/* Table */}
      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : error ? (
          <div className="p-6 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">Couldn't load expenses</p>
              <p className="text-[13px] text-red-800/80 mt-1">{error}</p>
            </div>
            <Button variant="secondary" icon={RefreshCw} onClick={load}>Retry</Button>
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={TrendingDown}
            title="No expenses recorded"
            description="Record your first expense to start tracking outgoing payments."
            action={
              <Button icon={Plus} onClick={() => navigate("/admin/finance/record-expense")}>
                Record expense
              </Button>
            }
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50/50">
                    {["Date", "Category", "Description", "Vendor", "Amount", "Method", ""].map(
                      (h, i) => (
                        <th
                          key={h || i}
                          className={`text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3 ${
                            i === 4 ? "text-right" : "text-left"
                          }`}
                        >
                          {h}
                        </th>
                      )
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-100">
                  {rows.map((r) => (
                    <tr key={r.id} className="hover:bg-surface-50/40">
                      <td className="px-6 py-3.5 text-[12px] text-surface-500">
                        {formatDate(r.expenseDate)}
                      </td>
                      <td className="px-6 py-3.5">
                        <Badge variant="rose">{r.category}</Badge>
                      </td>
                      <td className="px-6 py-3.5">
                        <p className="text-[13px] font-medium text-surface-800 max-w-[280px] truncate">
                          {r.description}
                        </p>
                        <p className="text-[10px] text-surface-400 font-mono mt-0.5">
                          {r.referenceNumber}
                        </p>
                      </td>
                      <td className="px-6 py-3.5 text-[12px] text-surface-600">
                        {r.vendor ?? "—"}
                      </td>
                      <td className="px-6 py-3.5 text-right text-[13px] font-semibold text-rose-600 tabular-nums">
                        -{formatNaira(r.amount)}
                      </td>
                      <td className="px-6 py-3.5">
                        <Badge variant="sky">{r.gatewayName}</Badge>
                      </td>
                      <td className="px-6 py-3.5 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            type="button"
                            onClick={() => navigate(`/admin/finance/record-expense?id=${r.id}`)}
                            className="p-1.5 rounded-lg text-surface-400 hover:text-brand-600 hover:bg-brand-50"
                            title="Edit"
                          >
                            <Pencil className="w-3.5 h-3.5" />
                          </button>
                          <button
                            type="button"
                            onClick={() => handleDelete(r.id)}
                            className="p-1.5 rounded-lg text-surface-400 hover:text-rose-600 hover:bg-rose-50"
                            title="Delete"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="px-6">
              <Pagination
                pageNumber={page}
                pageSize={pageSize}
                totalCount={total}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
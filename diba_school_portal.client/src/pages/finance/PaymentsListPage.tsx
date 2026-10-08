import { useEffect, useState, type ChangeEvent } from "react";
import { Receipt, Filter, Download, TrendingUp } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Pagination, SearchInput,
  Select, Badge, Skeleton, Avatar,
} from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { formatNaira, formatDateTime } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";
import type { FinancePaymentRow } from "../../types/finance";

const STATUS_TONE: Record<string, "emerald" | "amber" | "rose" | "neutral"> = {
  Successful: "emerald",
  Pending: "amber",
  Failed: "rose",
};

export default function PaymentsListPage() {
  const [rows, setRows] = useState<FinancePaymentRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(20);

  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [gateway, setGateway] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    setLoading(true);
    (async () => {
      try {
        const params = new URLSearchParams({
          pageNumber: String(page),
          pageSize: String(pageSize),
        });
        if (debounced.trim()) params.set("searchTerm", debounced.trim());
        if (gateway) params.set("gateway", gateway);
        if (status) params.set("status", status);

        const { data } = await api.get<
          ApiResponse<Paginated<FinancePaymentRow>>
        >(`/account/payments?${params}`);
        if (data.success) {
          setRows(data.data.items);
          setTotal(data.data.totalCount);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [debounced, gateway, status, page, pageSize]);

  const totalOnPage = rows.reduce((s, r) => s + r.amountPaid, 0);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Payments"
        description="Every transaction recorded at the bursary."
        actions={
          <>
            <Button variant="secondary" icon={Download}>Export</Button>
            <Button variant="secondary" icon={Filter}>Advanced</Button>
          </>
        }
      />

      {/* Summary strip */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">
            Transactions found
          </p>
          <p className="mt-1 text-xl font-semibold text-slate-900 tabular-nums">
            {total.toLocaleString()}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">
            Total on this page
          </p>
          <p className="mt-1 text-xl font-semibold text-emerald-600 tabular-nums">
            {formatNaira(totalOnPage)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">
            Awaiting confirmation
          </p>
          <p className="mt-1 text-xl font-semibold text-amber-600 tabular-nums">
            {rows.filter((r) => r.status === "Pending").length}
          </p>
        </Card>
      </div>

      {/* Filters */}
      <Card padding="sm">
        <div className="flex flex-col lg:flex-row gap-3">
          <div className="flex-1">
            <SearchInput
              placeholder="Search by reference, student, or admission no…"
              value={search}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="w-full lg:w-48">
            <Select
              value={gateway}
              onChange={(e) => {
                setGateway(e.target.value);
                setPage(1);
              }}
              options={[
                { value: "", label: "All methods" },
                { value: "Cash", label: "Cash" },
                { value: "BankTransfer", label: "Bank transfer" },
                { value: "Flutterwave", label: "Flutterwave" },
                { value: "Paystack", label: "Paystack" },
                { value: "Manual", label: "Manual" },
              ]}
            />
          </div>
          <div className="w-full lg:w-40">
            <Select
              value={status}
              onChange={(e) => {
                setStatus(e.target.value);
                setPage(1);
              }}
              options={[
                { value: "", label: "Any status" },
                { value: "Successful", label: "Successful" },
                { value: "Pending", label: "Pending" },
                { value: "Failed", label: "Failed" },
              ]}
            />
          </div>
        </div>
      </Card>

      {/* Table */}
      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-12" />
            ))}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={Receipt}
            title="No payments found"
            description="Adjust the filters or record a new cash payment."
          />
        ) : (
          <>
            {/* Desktop */}
            <div className="hidden lg:block overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/50">
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">Student</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">Reference</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">Method</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">Status</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">Date</th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">Amount</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {rows.map((r) => (
                    <tr key={r.id} className="hover:bg-slate-50/50 transition-colors">
                      <td className="px-6 py-3.5">
                        <div className="flex items-center gap-3">
                          <Avatar name={r.studentName} size="sm" />
                          <div>
                            <p className="text-[13px] font-medium text-slate-800">{r.studentName}</p>
                            <p className="text-[11px] text-slate-400 font-mono">{r.admissionNumber}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-3.5">
                        <span className="text-[12px] font-mono text-slate-600">{r.referenceNumber}</span>
                      </td>
                      <td className="px-6 py-3.5">
                        <Badge variant="neutral">{r.gatewayName ?? r.gateway}</Badge>
                      </td>
                      <td className="px-6 py-3.5">
                        <Badge variant={STATUS_TONE[r.statusName ?? r.status] ?? "neutral"}>
                          {r.statusName ?? r.status}
                        </Badge>
                      </td>
                      <td className="px-6 py-3.5">
                        <span className="text-[12px] text-slate-500">{formatDateTime(r.paymentDate)}</span>
                      </td>
                      <td className="px-6 py-3.5 text-right">
                        <span className="text-[13px] font-semibold text-slate-900 tabular-nums">
                          {formatNaira(r.amountPaid)}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Mobile */}
            <div className="lg:hidden divide-y divide-slate-100">
              {rows.map((r) => (
                <div key={r.id} className="p-4 flex items-center gap-3">
                  <Avatar name={r.studentName} size="md" />
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-slate-800 truncate">{r.studentName}</p>
                    <p className="text-[11px] text-slate-400 font-mono truncate">{r.referenceNumber}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge variant={STATUS_TONE[r.statusName ?? r.status] ?? "neutral"}>
                        {r.statusName ?? r.status}
                      </Badge>
                      <span className="text-[11px] text-slate-400">
                        {formatDateTime(r.paymentDate)}
                      </span>
                    </div>
                  </div>
                  <p className="text-[13px] font-semibold text-slate-900 tabular-nums">
                    {formatNaira(r.amountPaid)}
                  </p>
                </div>
              ))}
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
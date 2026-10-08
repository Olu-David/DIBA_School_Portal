import { useEffect, useState } from "react";
import { Wallet, BookOpen, CreditCard, AlertCircle } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, Badge, Skeleton, Button, EmptyState } from "../../components/ui";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function MyBillPage() {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<any>>("/student/me/bill");
        if (res.success) setData(res.data);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  if (loading) {
    return <Card><Skeleton className="h-64" /></Card>;
  }

  if (!data) {
    return (
      <Card>
        <EmptyState icon={Wallet} title="No bill available for the current term" />
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={data.termName}
        title="My bill"
        description="Detailed breakdown of what you owe for the current term."
      />

      {/* Summary strip */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">Total billed</p>
          <p className="mt-1 text-xl font-semibold text-slate-900 tabular-nums">
            {formatNaira(data.totalBilledAmount)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">Paid so far</p>
          <p className="mt-1 text-xl font-semibold text-emerald-600 tabular-nums">
            {formatNaira(data.totalPaidSoFar)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">Balance</p>
          <p className={`mt-1 text-xl font-semibold tabular-nums ${data.totalBalanceRemaining > 0 ? "text-rose-600" : "text-emerald-600"}`}>
            {formatNaira(data.totalBalanceRemaining)}
          </p>
        </Card>
      </div>

      {/* Fee breakdown */}
      <Card padding="none">
        <div className="p-6 pb-4">
          <h3 className="text-[15px] font-semibold text-slate-900">Fee items</h3>
        </div>
        <div className="divide-y divide-slate-100">
          {data.feeItems.map((f: any) => (
            <div key={f.feeId} className="px-6 py-4 flex items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                <div className={`w-9 h-9 rounded-xl grid place-items-center ${f.isFullyPaid ? "bg-emerald-50 text-emerald-600" : "bg-slate-100 text-slate-500"}`}>
                  <Wallet className="w-4 h-4" />
                </div>
                <div>
                  <p className="text-[13px] font-medium text-slate-800">{f.feeName}</p>
                  {f.description && <p className="text-[11px] text-slate-400 mt-0.5">{f.description}</p>}
                </div>
              </div>
              <div className="text-right">
                <p className="text-[13px] font-semibold text-slate-900 tabular-nums">{formatNaira(f.amount)}</p>
                <p className="text-[11px] text-slate-400 mt-0.5">
                  Paid {formatNaira(f.amountPaidTowardThis)}
                </p>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Books */}
      {data.availableBooks?.length > 0 && (
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-slate-900">Books</h3>
          </div>
          <div className="divide-y divide-slate-100">
            {data.availableBooks.map((b: any) => (
              <div key={b.classBookId} className="px-6 py-4 flex items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-violet-50 grid place-items-center">
                    <BookOpen className="w-4 h-4 text-violet-600" />
                  </div>
                  <div>
                    <p className="text-[13px] font-medium text-slate-800">{b.title}</p>
                    {b.description && <p className="text-[11px] text-slate-400 mt-0.5">{b.description}</p>}
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  {b.isPurchased && <Badge variant="emerald">Paid</Badge>}
                  <p className="text-[13px] font-semibold text-slate-900 tabular-nums">{formatNaira(b.price)}</p>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Payment CTA */}
      {data.totalBalanceRemaining > 0 && (
        <div className="bg-gradient-to-br from-emerald-50 to-emerald-100/50 rounded-2xl border border-emerald-100 p-6 flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-white grid place-items-center shadow-sm shrink-0">
            <CreditCard className="w-5 h-5 text-emerald-600" />
          </div>
          <div className="flex-1">
            <p className="text-[14px] font-semibold text-slate-900">
              Pay your fees online
            </p>
            <p className="text-[12px] text-slate-600 mt-0.5">
              Card, bank transfer, or USSD — secured by Flutterwave.
            </p>
          </div>
          <Button>Pay now</Button>
        </div>
      )}

      {/* Payment history */}
      {data.paymentHistory?.length > 0 && (
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-slate-900">Payment history</h3>
          </div>
          <div className="divide-y divide-slate-100">
            {data.paymentHistory.map((p: any) => (
              <div key={p.paymentId} className="px-6 py-3.5 flex items-center gap-3">
                <div className="w-9 h-9 rounded-xl bg-emerald-50 grid place-items-center shrink-0">
                  <Wallet className="w-4 h-4 text-emerald-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-[12px] font-mono text-slate-600">{p.referenceNumber}</p>
                  <p className="text-[11px] text-slate-400 mt-0.5">
                    {p.paymentDate ? formatDate(p.paymentDate) : "—"}
                  </p>
                </div>
                <p className="text-[13px] font-semibold text-slate-900 tabular-nums">
                  {formatNaira(p.amountPaid)}
                </p>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
}
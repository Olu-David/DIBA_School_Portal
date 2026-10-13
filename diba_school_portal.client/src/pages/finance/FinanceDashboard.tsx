import { useEffect, useState } from "react";
import {
  Wallet, TrendingUp, TrendingDown, Users, BadgeDollarSign,
  CreditCard, Banknote, ArrowUpRight, PieChart as PieIcon,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, StatCard, Skeleton, Badge } from "../../components/ui";
import { formatNaira, formatNairaShort, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";
import type { FinanceDashboard as FD } from "../../types/Finance";

interface SessionItem {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

export default function FinanceDashboard() {
  const [data, setData] = useState<FD | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data: sres } = await api.get<
          ApiResponse<{ items: SessionItem[] }>
        >("/academicsession/all?pageNumber=1&pageSize=20");

        if (!sres.success) return;

        const current =
          sres.data.items.find((s) => s.isCurrentSession) ?? sres.data.items[0];
        const term = current?.terms.find((t) => t.isCurrentTerm) ?? current?.terms[0];

        if (!current || !term) return;

        const { data: dres } = await api.get<ApiResponse<FD>>(
          `/account/dashboard?academicSessionId=${current.id}&termId=${term.id}`
        );
        if (dres.success) setData(dres.data);
      } catch {
        /* silent */
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  if (loading || !data) {
    return (
      <div className="space-y-6">
        <PageHeader eyebrow="Finance" title="Financial overview" />
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
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

  const collectionPercent = Math.min(100, Math.round(data.collectionRate));

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow={`${data.academicSessionName} - ${data.termName}`}
        title="Financial overview"
        description="Live collections, outstanding fees, and payment breakdown for the current term."
        actions={
          <>
            <Button variant="secondary">Export report</Button>
            <Button icon={BadgeDollarSign}>Record cash</Button>
          </>
        }
      />

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
      </div>

      <Card>
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-[15px] font-semibold text-surface-900">
              Collection progress
            </h3>
            <p className="text-[12px] text-surface-400 mt-0.5">
              {formatNaira(data.totalCollected)} of {formatNaira(data.totalExpected)}
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

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card>
          <div className="flex items-center gap-2 mb-5">
            <PieIcon className="w-4 h-4 text-surface-500" />
            <h3 className="text-[15px] font-semibold text-surface-900">
              Payment methods
            </h3>
          </div>
          <div className="space-y-4">
            <ChannelBar icon={Banknote} label="Cash" amount={data.totalCash} total={data.totalCollected} tone="emerald" />
            <ChannelBar icon={CreditCard} label="Online" amount={data.totalOnline} total={data.totalCollected} tone="sky" />
            <ChannelBar icon={TrendingUp} label="Bank transfer" amount={data.totalBankTransfer} total={data.totalCollected} tone="violet" />
            <ChannelBar icon={Wallet} label="Manual" amount={data.totalManual} total={data.totalCollected} tone="amber" />
          </div>
        </Card>

        <Card className="lg:col-span-2" padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <div>
              <h3 className="text-[15px] font-semibold text-surface-900">
                Recent payments
              </h3>
              <p className="text-[12px] text-surface-400 mt-0.5">
                Last {data.recentPayments.length} transactions
              </p>
            </div>
            <Button variant="ghost" size="sm" iconRight={ArrowUpRight}>
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
      </div>

      <Card padding="none">
        <div className="p-6 pb-4">
          <h3 className="text-[15px] font-semibold text-surface-900">
            Collection by class
          </h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-y border-surface-100 bg-surface-50/50">
                {["Class", "Students", "Expected", "Collected", "Rate"].map((h, i) => (
                  <th
                    key={h}
                    className={`text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3 ${
                      i === 0 ? "text-left" : "text-right"
                    }`}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-100">
              {data.classBreakdown.map((c) => {
                const rate = Math.round(c.collectionRate);
                const tone: "emerald" | "amber" | "rose" =
                  rate >= 80 ? "emerald" : rate >= 50 ? "amber" : "rose";
                return (
                  <tr key={c.classId} className="hover:bg-surface-50/50">
                    <td className="px-6 py-3.5 text-[13px] font-medium text-surface-800">{c.className}</td>
                    <td className="px-6 py-3.5 text-right text-[13px] text-surface-600 tabular-nums">{c.studentCount}</td>
                    <td className="px-6 py-3.5 text-right text-[13px] text-surface-600 tabular-nums">{formatNairaShort(c.expected)}</td>
                    <td className="px-6 py-3.5 text-right text-[13px] font-semibold text-surface-900 tabular-nums">{formatNairaShort(c.collected)}</td>
                    <td className="px-6 py-3.5 text-right">
                      <Badge variant={tone}>{rate}%</Badge>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}

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

function ChannelBar({ icon: Icon, label, amount, total, tone }: ChannelBarProps) {
  const pct = total > 0 ? Math.round((amount / total) * 100) : 0;
  const t = CHANNEL_TONES[tone];
  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <Icon className={`w-3.5 h-3.5 ${t.fg}`} />
          <span className="text-[12px] font-medium text-surface-700">{label}</span>
        </div>
        <span className="text-[12px] font-semibold text-surface-900 tabular-nums">
          {formatNairaShort(amount)}
        </span>
      </div>
      <div className="h-1.5 bg-surface-100 rounded-full overflow-hidden">
        <div className={`h-full ${t.bg} rounded-full transition-all`} style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}
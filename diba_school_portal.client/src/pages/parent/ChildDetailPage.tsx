import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Wallet, TrendingUp, Receipt, FileText, GraduationCap,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card, Avatar, Badge, Button, Skeleton, EmptyState, StatCard,
} from "../../components/ui";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function ChildDetailPage() {
  const { studentProfileId } = useParams<{ studentProfileId: string }>();
  const navigate = useNavigate();
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<any>>(
          `/parent/children/${studentProfileId}/dashboard`
        );
        if (res.success) setData(res.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [studentProfileId]);

  if (loading) return <Card><Skeleton className="h-64" /></Card>;

  if (!data) {
    return (
      <Card>
        <EmptyState
          icon={GraduationCap}
          title="Child not found"
          action={<Button onClick={() => navigate("/parent")}>Back</Button>}
        />
      </Card>
    );
  }

  const c = data.child;

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/parent")}
        className="inline-flex items-center gap-1.5 text-[12px] text-slate-500 hover:text-slate-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All children
      </button>

      {/* Header */}
      <Card>
        <div className="flex flex-col sm:flex-row gap-5 items-start sm:items-center">
          <Avatar name={c.fullName} src={c.profilePictureUrl} size="xl" />
          <div className="flex-1 min-w-0">
            <h1 className="text-xl font-semibold text-slate-900">{c.fullName}</h1>
            <p className="text-[12px] text-slate-400 font-mono mt-0.5">{c.admissionNumber}</p>
            <div className="mt-2 flex items-center gap-3 flex-wrap text-[12px] text-slate-500">
              <span className="inline-flex items-center gap-1.5">
                <GraduationCap className="w-3.5 h-3.5" />
                {c.className}
              </span>
              <Badge variant="neutral">{c.relationship}</Badge>
            </div>
          </div>
          <div className="flex gap-2 flex-wrap">
            <Button variant="secondary" icon={FileText} onClick={() => navigate(`/parent/children/${studentProfileId}/results`)}>
              Results
            </Button>
            <Button icon={Receipt} onClick={() => navigate(`/parent/children/${studentProfileId}/payments`)}>
              Payments
            </Button>
          </div>
        </div>
      </Card>

      {/* KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Outstanding"
          value={formatNaira(c.outstandingBalance)}
          hint={c.isFullyPaid ? "Fully paid ✓" : "Due"}
          icon={Wallet}
          tone={c.isFullyPaid ? "emerald" : "rose"}
        />
        <StatCard
          label="Total billed"
          value={formatNaira(data.totalBilled)}
          hint={data.currentTermName ?? "—"}
          icon={Receipt}
          tone="sky"
        />
        <StatCard
          label="Paid so far"
          value={formatNaira(data.totalPaid)}
          hint="This term"
          icon={TrendingUp}
          tone="violet"
        />
        <StatCard
          label="Latest average"
          value={c.latestAverageScore ? `${c.latestAverageScore.toFixed(1)}%` : "—"}
          hint={c.latestTermName ?? "No results yet"}
          icon={TrendingUp}
          tone="amber"
        />
      </div>

      {/* Two-col: payments + enrolments */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-slate-900">Recent payments</h3>
          </div>
          {data.recentPayments.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={Receipt} title="No payments yet" />
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {data.recentPayments.map((p: any) => (
                <div key={p.id} className="px-6 py-3.5 flex items-center justify-between gap-3">
                  <div className="min-w-0">
                    <p className="text-[12px] font-mono text-slate-600 truncate">{p.referenceNumber}</p>
                    <p className="text-[11px] text-slate-400 mt-0.5">{p.termName}</p>
                  </div>
                  <div className="text-right shrink-0">
                    <p className="text-[13px] font-semibold text-slate-900 tabular-nums">{formatNaira(p.amountPaid)}</p>
                    <p className="text-[11px] text-slate-400">{formatDate(p.paymentDate)}</p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-slate-900">Enrolment history</h3>
          </div>
          <div className="divide-y divide-slate-100">
            {data.enrollments.map((e: any) => (
              <div key={e.enrollmentId} className="px-6 py-3.5 flex items-center justify-between gap-3">
                <div>
                  <p className="text-[13px] font-medium text-slate-800">{e.className}</p>
                  <p className="text-[11px] text-slate-400 mt-0.5">{e.sessionName} · {e.termName}</p>
                </div>
                <div className="text-right">
                  <p className="text-[12px] text-slate-500">Paid {formatNaira(e.totalAmountPaid)}</p>
                  {e.isFullyPaid ? <Badge variant="emerald">Complete</Badge> : <Badge variant="rose">Owing</Badge>}
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}
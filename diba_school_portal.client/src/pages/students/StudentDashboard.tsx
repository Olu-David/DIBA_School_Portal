import { useEffect, useState } from "react";
import {
  BookOpen, Wallet, TrendingUp, Trophy, Calendar,
  ArrowUpRight, FileText, Receipt,
} from "lucide-react";
import { useAuth } from "../../hooks/useAuth";
import PageHeader from "../../components/PageHeader";
import { Card, StatCard, Skeleton, Button, Badge, Avatar } from "../../components/ui";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function StudentDashboard() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<any>(null);

  useEffect(() => {
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<any>>("/student/me/dashboard");
        if (res.success) setData(res.data);
      } catch {
        /* silent */
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const greeting = (() => {
    const h = new Date().getHours();
    if (h < 12) return "Good morning";
    if (h < 17) return "Good afternoon";
    return "Good evening";
  })();

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow={data?.className ?? "Student"}
        title={`${greeting}, ${user?.firstName ?? "there"}`}
        description="Your fees, results, and class updates in one place."
        actions={<Button variant="secondary" icon={Calendar}>Term calendar</Button>}
      />

      {/* KPI grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}><Skeleton className="h-20" /></Card>
          ))
        ) : (
          <>
            <StatCard
              label="Outstanding fees"
              value={formatNaira(data?.outstandingBalance ?? 0)}
              hint={data?.isFullyPaid ? "Fully paid ✓" : "Amount due"}
              icon={Wallet}
              tone={data?.isFullyPaid ? "emerald" : "rose"}
            />
            <StatCard
              label="Latest average"
              value={data?.latestAverageScore ? `${data.latestAverageScore.toFixed(1)}%` : "—"}
              hint={data?.latestTermName ?? "No results yet"}
              icon={TrendingUp}
              tone="sky"
            />
            <StatCard
              label="Class position"
              value={data?.latestClassPosition ? `#${data.latestClassPosition}` : "—"}
              hint="Most recent term"
              icon={Trophy}
              tone="amber"
            />
            <StatCard
              label="Subjects"
              value={data?.subjectCount ?? "—"}
              hint="This term"
              icon={BookOpen}
              tone="violet"
            />
          </>
        )}
      </div>

      {/* Quick links */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <h3 className="text-[15px] font-semibold text-slate-900 mb-4">
            Quick access
          </h3>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            {[
              { icon: FileText, label: "My results", tone: "sky" },
              { icon: Receipt, label: "My payments", tone: "emerald" },
              { icon: Wallet, label: "My bill", tone: "violet" },
            ].map((a) => (
              <button
                key={a.label}
                type="button"
                className="p-4 rounded-xl border border-slate-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
              >
                <div className={`w-9 h-9 rounded-lg grid place-items-center mb-3 bg-${a.tone}-50 text-${a.tone}-600`}>
                  <a.icon className="w-4 h-4" />
                </div>
                <p className="text-[13px] font-medium text-slate-800">{a.label}</p>
              </button>
            ))}
          </div>
        </Card>

        {/* Profile card */}
        <Card>
          <div className="text-center">
            <Avatar name={`${user?.firstName} ${user?.lastName}`} src={user?.profilePictureUrl} size="xl" />
            <p className="mt-4 text-[14px] font-semibold text-slate-900">
              {user?.firstName} {user?.lastName}
            </p>
            <p className="text-[12px] text-slate-400 font-mono mt-0.5">
              {user?.admissionNumber ?? "—"}
            </p>
            <Badge variant="emerald">{user?.className ?? "Student"}</Badge>
          </div>
        </Card>
      </div>
    </div>
  );
}
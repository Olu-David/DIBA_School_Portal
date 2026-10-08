import { useEffect, useState } from "react";
import {
  Users,
  Wallet,
  GraduationCap,
  ClipboardCheck,
  Calendar,
  CircleDot,
  AlertCircle,
} from "lucide-react";
import { useAuth } from "../../hooks/useAuth";
import PageHeader from "../../components/PageHeader";
import { Button, Card, StatCard, Skeleton } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

export default function Dashboard() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [studentCount, setStudentCount] = useState<number | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<ApiResponse<Paginated<unknown>>>(
          "/student?pageNumber=1&pageSize=1"
        );
        setStudentCount(res.data.data.totalCount);
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
        eyebrow="Overview"
        title={`${greeting}, ${user?.firstName ?? ""}`}
        description="Here's what's happening across the school today."
        actions={
          <>
            <Button variant="secondary" icon={Calendar}>
              Current Term
            </Button>
            <Button icon={CircleDot}>Quick action</Button>
          </>
        }
      />

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 lg:gap-6">
        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} padding="md">
              <Skeleton className="w-10 h-10 rounded-xl mb-5" />
              <Skeleton className="h-3 w-24 mb-2" />
              <Skeleton className="h-6 w-32" />
            </Card>
          ))
        ) : (
          <>
            <StatCard
              label="Total Students"
              value={studentCount?.toLocaleString() ?? "—"}
              hint="Active enrollments"
              icon={Users}
              tone="emerald"
            />
            <StatCard label="Fees Collected" value="—" hint="Term to date" icon={Wallet} tone="sky" />
            <StatCard label="Classes" value="—" hint="Across all sections" icon={GraduationCap} tone="violet" />
            <StatCard label="Pending Results" value="—" hint="Awaiting approval" icon={ClipboardCheck} tone="amber" />
          </>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <h2 className="text-[15px] font-semibold text-slate-900">Recent activity</h2>
          <p className="text-[12px] text-slate-400 mb-5">Latest events across the school</p>
          <div className="py-8 text-center">
            <p className="text-[13px] text-slate-400">No activity to display yet.</p>
          </div>
        </Card>

        <Card>
          <h2 className="text-[15px] font-semibold text-slate-900 mb-5">Quick actions</h2>
          <div className="space-y-2">
            {["Add new student", "Record cash payment", "Create academic session", "Publish results"].map((label) => (
              <button
                key={label}
                type="button"
                className="w-full text-left p-3 rounded-xl border border-slate-100 hover:border-emerald-200 hover:bg-emerald-50/40 transition-colors text-[13px] font-medium text-slate-800"
              >
                {label}
              </button>
            ))}
          </div>
        </Card>
      </div>

      <div className="bg-gradient-to-br from-emerald-50 to-emerald-100/50 rounded-2xl border border-emerald-100 p-6 flex flex-col sm:flex-row sm:items-center gap-4">
        <div className="w-11 h-11 rounded-xl bg-white grid place-items-center shadow-sm shrink-0">
          <AlertCircle className="w-5 h-5 text-emerald-600" />
        </div>
        <div className="flex-1">
          <p className="text-[14px] font-semibold text-slate-900">Keep your data fresh</p>
          <p className="text-[12px] text-slate-600 mt-0.5">
            Add students, record payments, and update sessions to see accurate figures.
          </p>
        </div>
        <Button variant="secondary">Learn more</Button>
      </div>
    </div>
  );
}

import { BookOpen, Wallet, TrendingUp, Trophy, Calendar, FileText, Receipt } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";
import PageHeader from "../../components/PageHeader";
import { Button, Card, StatCard, Badge, Avatar } from "../../components/ui";
import { formatNaira } from "../../lib/format";

export default function StudentDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const greeting = (() => {
    const h = new Date().getHours();
    if (h < 12) return "Good morning";
    if (h < 17) return "Good afternoon";
    return "Good evening";
  })();

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow={user?.className ?? "Student"}
        title={`${greeting}, ${user?.firstName ?? "there"}`}
        description="Your fees, results, and class updates in one place."
        actions={<Button variant="secondary" icon={Calendar}>Term calendar</Button>}
      />

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard label="Outstanding fees" value={formatNaira(0)} hint="Current term" icon={Wallet} tone="emerald" />
        <StatCard label="Latest average" value="-" hint="No results yet" icon={TrendingUp} tone="sky" />
        <StatCard label="Class position" value="-" hint="Most recent term" icon={Trophy} tone="amber" />
        <StatCard label="Subjects" value="-" hint="This term" icon={BookOpen} tone="violet" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <h3 className="text-[15px] font-semibold text-slate-900 mb-4">Quick access</h3>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            <button
              type="button"
              onClick={() => navigate("/student/results")}
              className="p-4 rounded-xl border border-slate-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-slate-50 text-slate-600">
                <FileText className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-slate-800">My results</p>
            </button>
            <button
              type="button"
              onClick={() => navigate("/student/bill")}
              className="p-4 rounded-xl border border-slate-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-slate-50 text-slate-600">
                <Receipt className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-slate-800">My payments</p>
            </button>
            <button
              type="button"
              onClick={() => navigate("/student/bill")}
              className="p-4 rounded-xl border border-slate-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-slate-50 text-slate-600">
                <Wallet className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-slate-800">My bill</p>
            </button>
          </div>
        </Card>

        <Card>
          <div className="text-center">
            <Avatar name={`${user?.firstName} ${user?.lastName}`} src={user?.profilePictureUrl} size="xl" />
            <p className="mt-4 text-[14px] font-semibold text-slate-900">
              {user?.firstName} {user?.lastName}
            </p>
            <p className="text-[12px] text-slate-400 font-mono mt-0.5">
              {user?.admissionNumber ?? "-"}
            </p>
            <div className="mt-2">
              <Badge variant="emerald">{user?.className ?? "Student"}</Badge>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}

import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Users, Wallet, TrendingUp, ArrowUpRight, GraduationCap,
} from "lucide-react";
import { useAuth } from "../../hooks/useAuth";
import PageHeader from "../../components/PageHeader";
import { Card, Avatar, Badge, Button, Skeleton, EmptyState } from "../../components/ui";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function ParentDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [children, setChildren] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<any[]>>("/parent/children");
        if (data.success) setChildren(data.data);
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
        eyebrow="Parent portal"
        title={`${greeting}, ${user?.firstName ?? "there"}`}
        description="Track your children's fees, results, and school activity."
      />

      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}><Skeleton className="h-48" /></Card>
          ))}
        </div>
      ) : children.length === 0 ? (
        <Card>
          <EmptyState
            icon={Users}
            title="No children linked"
            description="Contact the school registrar to link your children to this account."
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {children.map((c) => (
            <Card key={c.studentProfileId} className="hover:border-emerald-200 hover:shadow-md transition-all">
              <div className="flex items-start gap-4">
                <Avatar name={c.fullName} src={c.profilePictureUrl} size="lg" />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <h3 className="text-[15px] font-semibold text-slate-900 truncate">
                      {c.fullName}
                    </h3>
                    {c.isPrimaryContact && <Badge variant="emerald">Primary</Badge>}
                  </div>
                  <p className="text-[12px] text-slate-400 font-mono mt-0.5">
                    {c.admissionNumber}
                  </p>
                  <p className="text-[12px] text-slate-500 mt-1 inline-flex items-center gap-1.5">
                    <GraduationCap className="w-3.5 h-3.5" />
                    {c.className}
                  </p>
                </div>
              </div>

              <div className="mt-5 grid grid-cols-2 gap-3">
                <div className="p-3 rounded-xl bg-slate-50">
                  <p className="text-[10px] uppercase tracking-wider text-slate-400 font-medium">
                    Balance
                  </p>
                  <p className={`text-[14px] font-semibold mt-1 tabular-nums ${c.outstandingBalance > 0 ? "text-rose-600" : "text-emerald-600"}`}>
                    {c.outstandingBalance > 0 ? formatNaira(c.outstandingBalance) : "Fully paid"}
                  </p>
                </div>
                <div className="p-3 rounded-xl bg-slate-50">
                  <p className="text-[10px] uppercase tracking-wider text-slate-400 font-medium">
                    Avg score
                  </p>
                  <p className="text-[14px] font-semibold mt-1 tabular-nums text-slate-900">
                    {c.latestAverageScore ? `${c.latestAverageScore.toFixed(1)}%` : "—"}
                  </p>
                </div>
              </div>

              <div className="mt-5 flex gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  fullWidth
                  onClick={() => navigate(`/parent/children/${c.studentProfileId}`)}
                >
                  Details
                </Button>
                <Button
                  size="sm"
                  fullWidth
                  iconRight={ArrowUpRight}
                  onClick={() => navigate(`/parent/children/${c.studentProfileId}/results`)}
                >
                  Results
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Mail, Phone, Calendar, GraduationCap, Users,
  Wallet, TrendingUp, Pencil, Trash2, Receipt, FileText, Plus,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Avatar, Badge, Skeleton, EmptyState, StatCard,
} from "../../components/ui";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";
import type { StudentDetail } from "../../types/student";

interface Payment {
  id: string;
  referenceNumber: string;
  amountPaid: number;
  paymentDate: string;
  gateway: string;
  status: string;
  termName: string;
  sessionName: string;
}

const STREAM_TONE: Record<string, "sky" | "violet" | "amber" | "rose"> = {
  Science: "sky",
  Arts: "violet",
  Commercial: "amber",
  General: "rose",
};

export default function StudentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [student, setStudent] = useState<StudentDetail | null>(null);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    (async () => {
      try {
        const [{ data: s }, { data: p }] = await Promise.all([
          api.get<ApiResponse<StudentDetail>>(`/student/${id}`),
          api.get<ApiResponse<Payment[]>>(`/payment/student/${id}`),
        ]);
        if (s.success) setStudent(s.data);
        if (p.success) setPayments((p.data ?? []).slice(0, 8));
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <Card><Skeleton className="h-24" /></Card>
      </div>
    );
  }

  if (!student) {
    return (
      <Card>
        <EmptyState
          icon={Users}
          title="Student not found"
          action={<Button onClick={() => navigate("/admin/students")}>Back</Button>}
        />
      </Card>
    );
  }

  const parentLinks = student.parents ?? [];

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/students")}
        className="inline-flex items-center gap-1.5 text-[12px] text-slate-500 hover:text-slate-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All students
      </button>

      <Card>
        <div className="flex flex-col sm:flex-row sm:items-center gap-5">
          <Avatar
            name={`${student.firstName} ${student.lastName}`}
            src={student.profilePictureUrl}
            size="xl"
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h1 className="text-2xl font-semibold text-slate-900">
                {student.firstName} {student.lastName}
              </h1>
              {student.isActive ? (
                <Badge variant="emerald">Active</Badge>
              ) : (
                <Badge variant="neutral">Inactive</Badge>
              )}
              {student.stream && (
                <Badge variant={STREAM_TONE[student.stream] ?? "sky"}>
                  {student.stream}
                </Badge>
              )}
            </div>
            <p className="mt-1 text-[13px] text-slate-500 font-mono">
              {student.admissionNumber}
            </p>
            <div className="mt-3 flex items-center gap-4 flex-wrap text-[12px] text-slate-500">
              <span className="inline-flex items-center gap-1.5">
                <GraduationCap className="w-3.5 h-3.5" />
                {student.className}
                {student.stream && ` · ${student.stream}`}
              </span>
              {student.email && (
                <span className="inline-flex items-center gap-1.5">
                  <Mail className="w-3.5 h-3.5" />
                  {student.email}
                </span>
              )}
              {student.phoneNumber && (
                <span className="inline-flex items-center gap-1.5">
                  <Phone className="w-3.5 h-3.5" />
                  {student.phoneNumber}
                </span>
              )}
              <span className="inline-flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5" />
                Enrolled {formatDate(student.createdAt)}
              </span>
            </div>
          </div>
          <div className="flex gap-2">
            <Button variant="secondary" icon={Pencil}>Edit</Button>
            <Button
              variant="ghost"
              icon={Trash2}
              className="text-rose-600 hover:bg-rose-50"
            >
              Delete
            </Button>
          </div>
        </div>
      </Card>

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Outstanding balance"
          value={formatNaira(student.outstandingBalance)}
          hint={student.isFullyPaid ? "Fully paid" : "Fees unpaid"}
          icon={Wallet}
          tone={student.isFullyPaid ? "emerald" : "rose"}
        />
        <StatCard
          label="Latest average"
          value={student.latestAverageScore ? `${student.latestAverageScore.toFixed(1)}%` : "—"}
          hint={student.latestTermName ?? "No results yet"}
          icon={TrendingUp}
          tone="sky"
        />
        <StatCard
          label="Class position"
          value={student.latestClassPosition ? `#${student.latestClassPosition}` : "—"}
          hint="Most recent term"
          icon={GraduationCap}
          tone="violet"
        />
        <StatCard
          label="Guardians linked"
          value={parentLinks.length}
          hint="Family contacts"
          icon={Users}
          tone="amber"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <h3 className="text-[15px] font-semibold text-slate-900">
              Guardians & contacts
            </h3>
            <Button variant="ghost" size="sm" icon={Plus}>Link</Button>
          </div>

          {parentLinks.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={Users}
                title="No guardians linked"
                description="Add a parent or guardian to enable billing and report notifications."
              />
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {parentLinks.map((p) => (
                <div
                  key={p.parentUserId}
                  className="px-6 py-4 flex items-center gap-3"
                >
                  <Avatar name={p.fullName} src={p.profilePictureUrl} size="md" />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-[13px] font-medium text-slate-800">
                        {p.fullName}
                      </p>
                      {p.isPrimaryContact && <Badge variant="emerald">Primary</Badge>}
                    </div>
                    <p className="text-[11px] text-slate-400 mt-0.5">
                      {p.relationship}
                      {p.phoneNumber && ` · ${p.phoneNumber}`}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <h3 className="text-[15px] font-semibold text-slate-900">
              Recent payments
            </h3>
            <Button variant="ghost" size="sm" iconRight={Receipt}>All</Button>
          </div>

          {payments.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={Receipt}
                title="No payments yet"
                description="Payments made by or for this student will appear here."
              />
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {payments.map((p) => (
                <div key={p.id} className="px-6 py-3.5 flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-emerald-50 grid place-items-center shrink-0">
                    <Receipt className="w-4 h-4 text-emerald-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-slate-800 font-mono">
                      {p.referenceNumber}
                    </p>
                    <p className="text-[11px] text-slate-400 mt-0.5">
                      {p.termName} · {p.gateway}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-[13px] font-semibold text-slate-900 tabular-nums">
                      {formatNaira(p.amountPaid)}
                    </p>
                    <p className="text-[11px] text-slate-400">
                      {formatDate(p.paymentDate)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      <Card className="bg-gradient-to-br from-violet-50 to-violet-100/30 border-violet-100">
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-white grid place-items-center shadow-sm">
            <FileText className="w-5 h-5 text-violet-600" />
          </div>
          <div className="flex-1">
            <h3 className="text-[14px] font-semibold text-slate-900">
              Report cards
            </h3>
            <p className="text-[12px] text-slate-600 mt-0.5">
              View or download any term report for this student.
            </p>
          </div>
          <Button variant="secondary">Open results</Button>
        </div>
      </Card>
    </div>
  );
}
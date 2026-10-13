import { useEffect, useState } from "react";
import { CheckCircle2, ClipboardCheck } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, Skeleton, Select, Badge } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface PendingResult {
  termResultId: string;
  studentId: string;
  studentName: string;
  admissionNumber: string;
  className: string;
  averageScore: number;
  overallGrade: string;
  subjectCount: number;
}

interface Session { id: string; name: string; isCurrentSession: boolean; terms: { id: string; name: string; isCurrentTerm: boolean }[]; }

export default function ResultsApprovalPage() {
  const { toast } = useToast();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [rows, setRows] = useState<PendingResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);

  const load = async () => {
    if (!sessionId || !termId) return;
    setLoading(true);
    try {
      const { data } = await api.get<ApiResponse<{ classes: any[] }>>(
        `/termsubjectresult/school?academicSessionId=${sessionId}&termId=${termId}`
      );
      if (data.success) {
        const flat: PendingResult[] = [];
        data.data.classes.forEach((c: any) =>
          c.streams.forEach((st: any) =>
            st.students.filter((s: any) => !s.isPublished).forEach((s: any) => {
              flat.push({
                termResultId: s.termResultId,
                studentId: s.studentId,
                studentName: s.studentName,
                admissionNumber: s.admissionNumber,
                className: c.className,
                averageScore: s.averageScore,
                overallGrade: s.overallGrade,
                subjectCount: s.subjectCount ?? 0,
              });
            })
          )
        );
        setRows(flat);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<{ items: Session[] }>>(
        "/academicsession/all?pageNumber=1&pageSize=20"
      );
      if (data.success) {
        setSessions(data.data.items);
        const current = data.data.items.find((s) => s.isCurrentSession);
        if (current) {
          setSessionId(current.id);
          const t = current.terms.find((x) => x.isCurrentTerm) ?? current.terms[0];
          if (t) setTermId(t.id);
        }
      }
    })();
  }, []);

  useEffect(() => { load(); /* eslint-disable-next-line */ }, [sessionId, termId]);

  const publish = async (termResultId: string) => {
    setBusy(termResultId);
    try {
      const { data } = await api.post<ApiResponse<boolean>>(
        `/termsubjectresult/publish/${termResultId}`
      );
      if (data.success) {
        toast({ variant: "success", title: "Result published" });
        setRows((prev) => prev.filter((r) => r.termResultId !== termResultId));
      } else {
        toast({ variant: "error", title: data.message ?? "Failed" });
      }
    } catch {
      toast({ variant: "error", title: "Could not publish - endpoint may not exist yet" });
    } finally {
      setBusy(null);
    }
  };

  const session = sessions.find((s) => s.id === sessionId);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Results"
        title="Approve & publish results"
        description="Review pending term results and publish them to students and parents."
      />

      <Card padding="sm">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Select
            value={sessionId}
            onChange={(e) => {
              setSessionId(e.target.value);
              const s = sessions.find((x) => x.id === e.target.value);
              if (s?.terms[0]) setTermId(s.terms[0].id);
            }}
            options={sessions.map((s) => ({ value: s.id, label: s.name }))}
          />
          <Select
            value={termId}
            onChange={(e) => setTermId(e.target.value)}
            options={session?.terms.map((t) => ({ value: t.id, label: t.name })) ?? []}
          />
        </div>
      </Card>

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={CheckCircle2}
            title="All results are published"
            description="No pending results for this term."
          />
        ) : (
          <div className="divide-y divide-surface-100">
            {rows.map((r) => (
              <div key={r.termResultId} className="px-6 py-4 flex items-center gap-4">
                <div className="w-10 h-10 rounded-xl bg-flame-50 grid place-items-center shrink-0">
                  <ClipboardCheck className="w-4 h-4 text-flame-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-[13px] font-semibold text-surface-900">{r.studentName}</p>
                  <p className="text-[11px] text-surface-400 font-mono">{r.admissionNumber} - {r.className}</p>
                </div>
                <div className="text-right hidden sm:block">
                  <p className="text-[13px] font-semibold text-surface-900 tabular-nums">
                    {r.averageScore.toFixed(1)}%
                  </p>
                  <p className="text-[11px] text-surface-400">Grade {r.overallGrade}</p>
                </div>
                <Badge variant="amber">Pending</Badge>
                <Button
                  size="sm"
                  loading={busy === r.termResultId}
                  onClick={() => publish(r.termResultId)}
                >
                  Publish
                </Button>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
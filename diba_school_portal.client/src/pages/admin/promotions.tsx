import { useEffect, useMemo, useState } from "react";
import { GraduationCap, AlertCircle, CheckCircle2, Users, RefreshCw } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, Badge, Skeleton, EmptyState } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

type StreamNum = 0 | 1 | 2;
const STREAM_OPTIONS: { value: StreamNum; label: string }[] = [
  { value: 0, label: "Science" },
  { value: 1, label: "Arts" },
  { value: 2, label: "Commercial" },
];

interface Preview {
  sessionId: string;
  sessionName: string;
  thirdTermEndDate: string | null;
  thirdTermEnded: boolean;
  isReady: boolean;
  totalStudents: number;
  totalGraduating: number;
  totalRequiresStream: number;
  classes: {
    classId: string;
    className: string;
    academicSection: number;
    studentCount: number;
    destinationClassName: string;
    isGraduating: boolean;
    requiresStreamSelection: boolean;
    students: {
      id: string;
      fullName: string;
      admissionNumber: string;
      currentStream: StreamNum | null;
      needsStream: boolean;
    }[];
  }[];
}

export default function PromotionPage() {
  const { toast } = useToast();
  const [data, setData] = useState<Preview | null>(null);
  const [loading, setLoading] = useState(true);
  const [executing, setExecuting] = useState(false);

  // studentId → chosen stream (for JSS3 → SS1)
  const [streamPicks, setStreamPicks] = useState<Record<string, StreamNum>>({});

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get<ApiResponse<Preview>>("/promotion/preview");
      if (data.success) setData(data.data);
      else toast({ variant: "error", title: data.message ?? "Could not load preview." });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); /* eslint-disable-next-line */ }, []);

  const missingStreams = useMemo(() => {
    if (!data) return 0;
    let n = 0;
    data.classes.forEach((c) => {
      if (!c.requiresStreamSelection) return;
      c.students.forEach((s) => {
        if (!s.currentStream && streamPicks[s.id] === undefined) n++;
      });
    });
    return n;
  }, [data, streamPicks]);

  const execute = async () => {
    if (!data || !data.isReady) return;
    if (missingStreams > 0) {
      toast({ variant: "error", title: `Pick streams for ${missingStreams} student(s) first.` });
      return;
    }
    if (!window.confirm(
      `Promote ${data.totalStudents} students?\n${data.totalGraduating} will graduate.\n\nThis cannot be undone easily.`
    )) return;

    setExecuting(true);
    try {
      const overrides = Object.entries(streamPicks).map(([studentId, stream]) => ({
        studentId,
        stream,
      }));
      const { data: res } = await api.post<ApiResponse<any>>(
        `/promotion/${data.sessionId}/execute`,
        overrides
      );
      if (!res.success) {
        toast({ variant: "error", title: res.message ?? "Promotion failed." });
        return;
      }
      toast({
        variant: "success",
        title: `Promoted ${res.data.promotedCount} · Graduated ${res.data.graduatedCount}`,
      });
      await load();
    } catch (err: any) {
      toast({
        variant: "error",
        title: err?.response?.data?.message ?? "Promotion failed.",
      });
    } finally {
      setExecuting(false);
    }
  };

  if (loading) return <Card><Skeleton className="h-64" /></Card>;

  if (!data) return (
    <Card><EmptyState icon={Users} title="No active session found" /></Card>
  );

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="End-of-session promotion"
        description={`Session ${data.sessionName}`}
      />

      <Card className={data.isReady ? "border-emerald-200 bg-emerald-50/30" : "border-amber-200 bg-amber-50/30"}>
        <div className="flex items-start gap-3">
          <div className={`w-10 h-10 rounded-xl grid place-items-center shrink-0 ${
            data.isReady ? "bg-emerald-100" : "bg-amber-100"
          }`}>
            {data.isReady
              ? <CheckCircle2 className="w-5 h-5 text-emerald-600" />
              : <AlertCircle className="w-5 h-5 text-amber-600" />}
          </div>
          <div className="flex-1">
            <p className="text-[14px] font-semibold text-surface-900">
              {data.isReady ? "Ready to promote" : "Waiting for session to end"}
            </p>
            <p className="text-[12px] text-surface-600 mt-0.5">
              {data.isReady
                ? `Third term ended ${formatDate(data.thirdTermEndDate!)}. You can promote students now.`
                : `Third term ${data.thirdTermEndDate ? `ends ${formatDate(data.thirdTermEndDate)}` : "has not been configured"}. Promotion is locked until then.`}
            </p>
          </div>
          <Button variant="secondary" icon={RefreshCw} onClick={load}>Refresh</Button>
        </div>
      </Card>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card><p className="text-[12px] text-surface-500">Total students</p><p className="text-2xl font-semibold">{data.totalStudents}</p></Card>
        <Card><p className="text-[12px] text-surface-500">Graduating (SS 3)</p><p className="text-2xl font-semibold text-violet-600">{data.totalGraduating}</p></Card>
        <Card><p className="text-[12px] text-surface-500">Need stream selection</p><p className="text-2xl font-semibold text-amber-600">{missingStreams}</p></Card>
      </div>

      {data.classes.map((c) => (
        <Card key={c.classId} padding="none">
          <div className="p-4 flex items-center justify-between border-b border-surface-100">
            <div className="flex items-center gap-3">
              <Badge variant="sky">{c.className}</Badge>
              <span className="text-[12px] text-surface-500">
                {c.studentCount} student{c.studentCount !== 1 ? "s" : ""}
              </span>
            </div>
            <div className="flex items-center gap-2">
              {c.isGraduating ? (
                <Badge variant="violet"><GraduationCap className="w-3 h-3" /> Graduating</Badge>
              ) : (
                <span className="text-[12px] text-surface-600">
                  → <strong>{c.destinationClassName}</strong>
                </span>
              )}
            </div>
          </div>

          {c.requiresStreamSelection && (
            <div className="px-4 py-3 bg-amber-50/50 border-b border-amber-100">
              <p className="text-[12px] text-amber-800">
                Choose a stream for each student before promoting.
              </p>
            </div>
          )}

          <div className="divide-y divide-surface-100 max-h-80 overflow-y-auto">
            {c.students.map((s) => {
              const chosen = streamPicks[s.id];
              const effectiveStream = chosen ?? s.currentStream;
              return (
                <div key={s.id} className="px-4 py-2.5 flex items-center gap-3">
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-surface-800 truncate">{s.fullName}</p>
                    <p className="text-[11px] text-surface-400 font-mono">{s.admissionNumber}</p>
                  </div>
                  {c.requiresStreamSelection && (
                    <select
                      value={effectiveStream === null || effectiveStream === undefined ? "" : String(effectiveStream)}
                      onChange={(e) => setStreamPicks((p) => ({
                        ...p,
                        [s.id]: e.target.value === "" ? undefined! : (Number(e.target.value) as StreamNum),
                      }))}
                      className="text-[12px] rounded-lg border border-surface-200 px-2 py-1 bg-white"
                    >
                      <option value="">Pick stream…</option>
                      {STREAM_OPTIONS.map((o) => (
                        <option key={o.value} value={o.value}>{o.label}</option>
                      ))}
                    </select>
                  )}
                  {c.isGraduating && <Badge variant="violet">Graduates</Badge>}
                </div>
              );
            })}
          </div>
        </Card>
      ))}

      <div className="flex justify-end">
        <Button
          icon={GraduationCap}
          loading={executing}
          disabled={!data.isReady || missingStreams > 0}
          onClick={execute}
        >
          Execute promotion
        </Button>
      </div>
    </div>
  );
}
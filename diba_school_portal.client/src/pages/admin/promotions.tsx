import { useEffect, useMemo, useState } from "react";
import {
  GraduationCap, AlertCircle, CheckCircle2, Users, RefreshCw, X,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, Badge, Skeleton, EmptyState } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

/* =========================================================
   Types
   ========================================================= */
type StreamNum = 0 | 1 | 2;
const STREAM_OPTIONS: { value: StreamNum; label: string }[] = [
  { value: 0, label: "Science" },
  { value: 1, label: "Arts" },
  { value: 2, label: "Commercial" },
];

interface StudentPreview {
  id: string;
  fullName: string;
  admissionNumber: string;
  currentStream: StreamNum | null;
  needsStream: boolean;
}

interface ClassPreview {
  classId: string;
  className: string;
  academicSection: number;
  studentCount: number;
  destinationClassName: string;
  isGraduating: boolean;
  requiresStreamSelection: boolean;
  students: StudentPreview[];
}

interface Preview {
  sessionId: string;
  sessionName: string;
  thirdTermEndDate: string | null;
  thirdTermEnded: boolean;
  isReady: boolean;
  totalStudents: number;
  totalGraduating: number;
  totalRequiresStream: number;
  classes: ClassPreview[];
}

/* =========================================================
   Page
   ========================================================= */
export default function PromotionPage() {
  const { toast } = useToast();

  const [data, setData] = useState<Preview | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [executing, setExecuting] = useState(false);

  /* studentId → chosen stream (for JSS3 → SS1) */
  const [streamPicks, setStreamPicks] = useState<Record<string, StreamNum>>({});

  /* ---------------- Load ---------------- */
  const load = async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const { data: res } = await api.get<ApiResponse<Preview>>(
        "/promotion/preview"
      );

      if (!res.success || !res.data) {
        setLoadError(res.message ?? "Could not load preview.");
        setData(null);
        return;
      }

      setData(res.data);

      /* Reset picks when the underlying list changes — otherwise stale
         picks from a previous session linger in state. */
      setStreamPicks({});
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      const message =
        e2?.response?.data?.message ??
        e2?.message ??
        (status === 401
          ? "Session expired — please log in again."
          : status === 403
          ? "You don't have permission to run promotions."
          : status === 500
          ? "Server error while loading preview."
          : "Could not load promotion preview.");

      setLoadError(message);
      setData(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /* ---------------- Derived: how many still need a pick ---------------- */
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

  /* ---------------- Execute ---------------- */
  const execute = async () => {
    if (!data || !data.isReady) return;

    if (missingStreams > 0) {
      toast({
        variant: "error",
        title: `Pick streams for ${missingStreams} student(s) first.`,
      });
      return;
    }

    if (
      !window.confirm(
        `Promote ${data.totalStudents} student(s)?\n` +
          `${data.totalGraduating} will graduate.\n\n` +
          `This cannot be undone easily.`
      )
    ) {
      return;
    }

    setExecuting(true);
    try {
      const overrides = Object.entries(streamPicks).map(
        ([studentId, stream]) => ({ studentId, stream })
      );

      const { data: res } = await api.post<ApiResponse<any>>(
        `/promotion/${data.sessionId}/execute`,
        overrides
      );

      if (!res.success) {
        toast({
          variant: "error",
          title: res.message ?? "Promotion failed.",
        });
        return;
      }

      toast({
        variant: "success",
        title: `Promoted ${res.data.promotedCount} · Graduated ${res.data.graduatedCount}`,
      });

      await load();
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      toast({
        variant: "error",
        title:
          e2?.response?.data?.message ??
          e2?.message ??
          "Promotion failed.",
      });
    } finally {
      setExecuting(false);
    }
  };

  /* ---------------- Stream helper ---------------- */
  const setStream = (studentId: string, value: string) => {
    setStreamPicks((prev) => {
      const next = { ...prev };
      if (value === "") {
        delete next[studentId];
      } else {
        next[studentId] = Number(value) as StreamNum;
      }
      return next;
    });
  };

  /* ---------------- Render guards ---------------- */

  /* 1. Loading — keep the header so the layout doesn't jump */
  if (loading) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Academics"
          title="End-of-session promotion"
          description="Loading…"
        />
        <Card><Skeleton className="h-64" /></Card>
      </div>
    );
  }

  /* 2. Load error — show the REAL error, not the misleading "no session" */
  if (loadError) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Academics"
          title="End-of-session promotion"
        />
        <Card>
          <div className="p-6 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">
                Couldn't load promotions
              </p>
              <p className="text-[13px] text-red-800/80 mt-1">{loadError}</p>
            </div>
            <Button variant="secondary" icon={RefreshCw} onClick={load}>
              Retry
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  /* 3. No data after a successful request → genuinely no session */
  if (!data) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Academics"
          title="End-of-session promotion"
        />
        <Card>
          <EmptyState
            icon={Users}
            title="No active session found"
            description="Set an academic session as current from Sessions & Terms, then come back."
            action={
              <Button variant="secondary" icon={RefreshCw} onClick={load}>
                Refresh
              </Button>
            }
          />
        </Card>
      </div>
    );
  }

  /* ---------------- Main render ---------------- */
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="End-of-session promotion"
        description={`Session ${data.sessionName}`}
      />

      {/* Status banner */}
      <Card
        className={
          data.isReady
            ? "border-emerald-200 bg-emerald-50/30"
            : "border-amber-200 bg-amber-50/30"
        }
      >
        <div className="flex items-start gap-3">
          <div
            className={`w-10 h-10 rounded-xl grid place-items-center shrink-0 ${
              data.isReady ? "bg-emerald-100" : "bg-amber-100"
            }`}
          >
            {data.isReady ? (
              <CheckCircle2 className="w-5 h-5 text-emerald-600" />
            ) : (
              <AlertCircle className="w-5 h-5 text-amber-600" />
            )}
          </div>
          <div className="flex-1">
            <p className="text-[14px] font-semibold text-surface-900">
              {data.isReady ? "Ready to promote" : "Waiting for session to end"}
            </p>
            <p className="text-[12px] text-surface-600 mt-0.5">
              {data.isReady
                ? data.thirdTermEndDate
                  ? `Third term ended ${formatDate(data.thirdTermEndDate)}. You can promote students now.`
                  : "Third term end date was not set, but promotion is unlocked."
                : data.thirdTermEndDate
                ? `Third term ends ${formatDate(data.thirdTermEndDate)}. Promotion is locked until then.`
                : "Third term has not been configured for this session."}
            </p>
          </div>
          <Button variant="secondary" icon={RefreshCw} onClick={load}>
            Refresh
          </Button>
        </div>
      </Card>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <p className="text-[12px] text-surface-500">Total students</p>
          <p className="text-2xl font-semibold">{data.totalStudents}</p>
        </Card>
        <Card>
          <p className="text-[12px] text-surface-500">Graduating</p>
          <p className="text-2xl font-semibold text-violet-600">
            {data.totalGraduating}
          </p>
        </Card>
        <Card>
          <p className="text-[12px] text-surface-500">Need stream selection</p>
          <p className="text-2xl font-semibold text-amber-600">
            {missingStreams}
          </p>
        </Card>
      </div>

      {/* Empty list — service returned success but no classes had students */}
      {data.classes.length === 0 && (
        <Card>
          <EmptyState
            icon={Users}
            title="No students to promote"
            description="No class has active (non-graduated) students. Enrol students first, then come back."
          />
        </Card>
      )}

      {/* Class cards */}
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
                <Badge variant="violet">
                  <GraduationCap className="w-3 h-3" /> Graduating
                </Badge>
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
              const effectiveStream: StreamNum | null =
                chosen !== undefined ? chosen : s.currentStream;

              return (
                <div
                  key={s.id}
                  className="px-4 py-2.5 flex items-center gap-3"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-surface-800 truncate">
                      {s.fullName}
                    </p>
                    <p className="text-[11px] text-surface-400 font-mono">
                      {s.admissionNumber}
                    </p>
                  </div>

                  {c.requiresStreamSelection && (
                    <div className="flex items-center gap-1">
                      <select
                        value={
                          effectiveStream === null ? "" : String(effectiveStream)
                        }
                        onChange={(e) => setStream(s.id, e.target.value)}
                        className="text-[12px] rounded-lg border border-surface-200 px-2 py-1 bg-white"
                      >
                        <option value="">Pick stream…</option>
                        {STREAM_OPTIONS.map((o) => (
                          <option key={o.value} value={o.value}>
                            {o.label}
                          </option>
                        ))}
                      </select>

                      {/* Clear button so a pick can be undone */}
                      {chosen !== undefined && (
                        <button
                          type="button"
                          onClick={() => setStream(s.id, "")}
                          title="Clear selection"
                          className="p-1 rounded-md text-surface-400 hover:bg-surface-100"
                        >
                          <X className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </div>
                  )}

                  {c.isGraduating && <Badge variant="violet">Graduates</Badge>}
                </div>
              );
            })}
          </div>
        </Card>
      ))}

      {/* Execute bar */}
      {data.classes.length > 0 && (
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
      )}
    </div>
  );
}
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  BookOpen,
  GraduationCap,
  Users,
  CheckCircle2,
  TrendingUp,
  Trophy,
  FileText,
  ArrowRight,
  Download,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card,
  Button,
  Badge,
  Skeleton,
  EmptyState,
  Select,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { downloadPdf } from "../../lib/downloadPdf";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface Assignment {
  classId: string;
  className: string;
  section: number;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  studentCount: number;
  scoredCount: number;
  submissionStatus: string;   // "NotStarted" | "Draft" | "Submitted"
}

interface Session {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

const SECTION_LABEL: Record<number, string> = {
  0: "Pre-Nursery",
  1: "Nursery",
  2: "Primary",
  3: "Junior Secondary",
  4: "Senior Secondary",
};

export default function SubmittedResultsPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [loading, setLoading] = useState(true);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  // ---- Load sessions ----
  useEffect(() => {
    (async () => {
      const { data: res } = await api.get<ApiResponse<{ items: Session[] }>>(
        "/academicsession/all?pageNumber=1&pageSize=20"
      );
      if (res.success) {
        setSessions(res.data.items);
        const current =
          res.data.items.find((s) => s.isCurrentSession) ?? res.data.items[0];
        if (current) {
          setSessionId(current.id);
          const t =
            current.terms.find((x) => x.isCurrentTerm) ?? current.terms[0];
          if (t) setTermId(t.id);
        }
      }
    })();
  }, []);

  // ---- Load assignments ----
  useEffect(() => {
    if (!sessionId || !termId) return;
    setLoading(true);
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<Assignment[]>>(
          `/termsubjectresult/my-assignments?academicSessionId=${sessionId}&termId=${termId}`
        );
        if (res.success) setAssignments(res.data ?? []);
      } finally {
        setLoading(false);
      }
    })();
  }, [sessionId, termId]);

  const session = sessions.find((s) => s.id === sessionId);

  // ---- Only show Submitted (or at least Draft, if you want to preview) ----
  const submitted = useMemo(
    () => assignments.filter((a) => a.submissionStatus === "Submitted"),
    [assignments]
  );

  // ---- Group by class ----
  const grouped = useMemo(() => {
    const map = new Map<
      string,
      { className: string; section: number; items: Assignment[] }
    >();
    for (const a of submitted) {
      if (!map.has(a.classId)) {
        map.set(a.classId, { className: a.className, section: a.section, items: [] });
      }
      map.get(a.classId)!.items.push(a);
    }
    return Array.from(map.entries())
      .map(([classId, g]) => ({ classId, ...g }))
      .sort((a, b) => a.className.localeCompare(b.className));
  }, [submitted]);

  const totals = useMemo(() => {
    const classes = new Set(submitted.map((a) => a.classId));
    const students = new Set<string>(); // can't dedupe across subjects w/o row data
    return {
      subjects: submitted.length,
      classes: classes.size,
      entries: submitted.reduce((s, a) => s + a.scoredCount, 0),
    };
  }, [submitted]);

  // ---- Download the class sheet PDF for a submitted subject ----
  const downloadClassSheet = async (a: Assignment) => {
    setDownloadingId(a.subjectId);
    try {
      // Fetch the sheet data first (structured DTO), then POST to PDF service
      const { data: sheetRes } = await api.get<ApiResponse<any>>(
        `/termsubjectresult/class-sheet?classId=${a.classId}&subjectId=${a.subjectId}` +
          `&termId=${termId}&academicSessionId=${sessionId}`
      );
      if (!sheetRes.success || !sheetRes.data) {
        toast({ variant: "error", title: sheetRes.message ?? "Sheet not available" });
        return;
      }

      await downloadPdf(
        "/pdfreport/teacher-class-sheet",
        sheetRes.data,
        `ClassSheet_${a.className}_${a.subjectName}.pdf`
      );
    } catch (err: any) {
      toast({
        variant: "error",
        title: err?.response?.data?.message ?? "Could not download PDF",
      });
    } finally {
      setDownloadingId(null);
    }
  };

  const openSheet = (a: Assignment) => {
    navigate(
      `/admin/score-entry?classId=${a.classId}&subjectId=${a.subjectId}` +
        `&termId=${termId}&academicSessionId=${sessionId}`
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Results"
        title="My submitted results"
        description="Everything you've submitted this term — read-only."
      />

      {/* Session / term picker */}
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
            placeholder="Academic session"
          />
          <Select
            value={termId}
            onChange={(e) => setTermId(e.target.value)}
            options={
              session?.terms.map((t) => ({ value: t.id, label: t.name })) ?? []
            }
            placeholder="Term"
            disabled={!sessionId}
          />
        </div>
      </Card>

      {loading ? (
        <Card>
          <Skeleton className="h-64" />
        </Card>
      ) : submitted.length === 0 ? (
        <Card>
          <EmptyState
            icon={FileText}
            title="Nothing submitted yet"
            description="Once you submit scores from My Assignments, they'll show up here."
            action={
              <Button onClick={() => navigate("/admin/my-assignments")}>
                Go to My Assignments
              </Button>
            }
          />
        </Card>
      ) : (
        <>
          {/* KPI strip */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Subjects submitted
              </p>
              <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
                {totals.subjects}
              </p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Classes
              </p>
              <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
                {totals.classes}
              </p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Total entries
              </p>
              <p className="text-2xl font-semibold text-brand-600 tabular-nums mt-1">
                {totals.entries}
              </p>
            </Card>
          </div>

          {/* Class groups */}
          {grouped.map((group) => (
            <Card key={group.classId} padding="none">
              <div className="p-6 pb-4 flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-brand-50 grid place-items-center">
                  <GraduationCap className="w-5 h-5 text-brand-600" />
                </div>
                <div>
                  <h3 className="text-[15px] font-semibold text-surface-900">
                    {group.className}
                  </h3>
                  <p className="text-[11px] text-surface-400 mt-0.5">
                    {SECTION_LABEL[group.section] ?? "Class"} ·{" "}
                    {group.items.length} subject
                    {group.items.length === 1 ? "" : "s"} submitted
                  </p>
                </div>
              </div>

              <div className="divide-y divide-surface-100 border-t border-surface-100">
                {group.items.map((a) => {
                  const pct =
                    a.studentCount > 0
                      ? Math.round((a.scoredCount / a.studentCount) * 100)
                      : 0;

                  return (
                    <div
                      key={a.subjectId}
                      className="px-6 py-4 flex items-center gap-4 hover:bg-surface-50/40 transition-colors"
                    >
                      {/* Icon */}
                      <div className="w-9 h-9 rounded-xl bg-emerald-50 grid place-items-center shrink-0">
                        <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                      </div>

                      {/* Info */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <p className="text-[13px] font-semibold text-surface-900">
                            {a.subjectName}
                          </p>
                          <span className="text-[10px] font-mono text-surface-400">
                            {a.subjectCode}
                          </span>
                          <Badge variant="emerald">Submitted</Badge>
                        </div>
                        <div className="flex items-center gap-3 mt-1 text-[11px] text-surface-500">
                          <span className="inline-flex items-center gap-1">
                            <Users className="w-3 h-3" />
                            {a.studentCount} student
                            {a.studentCount === 1 ? "" : "s"}
                          </span>
                          <span className="tabular-nums">
                            {a.scoredCount}/{a.studentCount} scored
                          </span>
                        </div>
                      </div>

                      {/* Buttons */}
                      <div className="flex gap-2 shrink-0">
                        <Button
                          size="sm"
                          variant="ghost"
                          icon={FileText}
                          onClick={() => openSheet(a)}
                        >
                          View
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          icon={Download}
                          loading={downloadingId === a.subjectId}
                          onClick={() => downloadClassSheet(a)}
                        >
                          PDF
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </Card>
          ))}
        </>
      )}
    </div>
  );
}
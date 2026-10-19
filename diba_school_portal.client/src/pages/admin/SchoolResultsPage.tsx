import { useEffect, useMemo, useState } from "react";
import {
  BarChart3,
  Trophy,
  AlertCircle,
  CheckCircle2,
  Clock,
  Download,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card,
  EmptyState,
  Skeleton,
  Select,
  Avatar,
  Badge,
  Button,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { downloadPdf } from "../../lib/downloadPdf";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface StreamGroup {
  stream: number | null;
  streamName: string;
  studentCount: number;
  streamAverage: number;
  students: {
    termResultId: string;
    studentId: string;
    studentName: string;
    admissionNumber: string;
    profilePictureUrl: string | null;
    averageScore: number;
    position: number;
    overallGrade: string;
    subjectCount: number;
    submittedSubjectCount: number;
    totalSubjectCount: number;
    isComplete: boolean;
  }[];
}

interface ClassGroup {
  classId: string;
  className: string;
  section: number;
  studentCount: number;
  classAverage: number;
  highestAverage: number;
  lowestAverage: number;
  completeCount: number;
  pendingCount: number;
  streams: StreamGroup[];
}

interface SchoolResult {
  academicSessionId: string;
  academicSessionName: string;
  termId: string;
  termName: string;
  totalStudents: number;
  totalClasses: number;
  totalComplete: number;
  totalPending: number;
  overallAverage: number;
  classes: ClassGroup[];
}

interface OutstandingSubmission {
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  teacherName: string;
  studentCount: number;
  scoredCount: number;
}

interface Session {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

/* ==========================================================
   Class name ordering helper
   Nursery → Pre-Nursery → Primary → JSS → SS
   ========================================================== */
const SECTION_ORDER_PREFIX: Record<string, number> = {
  "Pre-Nursery": 0,
  "Pre Nursery": 0,
  "Nursery": 1,
  "Primary": 2,
  "JSS": 3,
  "Junior Secondary": 3,
  "SS": 4,
  "Senior Secondary": 4,
};

function classSortKey(className: string): [number, number, string] {
  const trimmed = (className ?? "").trim();
  const parts = trimmed.split(/\s+/);

  // Handle "Pre-Nursery 1" / "Pre Nursery 2" (two-word prefix)
  let prefix = parts[0] ?? "";
  let rest = parts.slice(1);
  if (
    (prefix === "Pre-Nursery" || prefix === "Pre" || prefix === "Junior" || prefix === "Senior") &&
    parts[1]
  ) {
    prefix = `${parts[0]} ${parts[1]}`;
    rest = parts.slice(2);
  }

  const num = parseInt(rest.join(" "), 10);
  const numeric = Number.isFinite(num) ? num : 0;
  const sectionRank = SECTION_ORDER_PREFIX[prefix] ?? 99;

  return [sectionRank, numeric, trimmed];
}

export default function SchoolResultsPage() {
  const { toast } = useToast();

  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [data, setData] = useState<SchoolResult | null>(null);
  const [outstanding, setOutstanding] = useState<OutstandingSubmission[]>([]);
  const [loading, setLoading] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [showOutstanding, setShowOutstanding] = useState(true);

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

  useEffect(() => {
    if (!sessionId || !termId) return;
    setLoading(true);
    (async () => {
      try {
        const [schoolRes, outstandingRes] = await Promise.allSettled([
          api.get<ApiResponse<SchoolResult>>(
            `/termsubjectresult/school?academicSessionId=${sessionId}&termId=${termId}`
          ),
          api.get<ApiResponse<OutstandingSubmission[]>>(
            `/termsubjectresult/outstanding?academicSessionId=${sessionId}&termId=${termId}`
          ),
        ]);

        if (schoolRes.status === "fulfilled" && schoolRes.value.data.success) {
          setData(schoolRes.value.data.data);
        } else {
          setData(null);
        }

        if (
          outstandingRes.status === "fulfilled" &&
          outstandingRes.value.data.success
        ) {
          setOutstanding(outstandingRes.value.data.data ?? []);
        } else {
          setOutstanding([]);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [sessionId, termId]);

  const session = sessions.find((s) => s.id === sessionId);

  const outstandingByClass = useMemo(() => {
    const map = new Map<string, OutstandingSubmission[]>();
    for (const o of outstanding) {
      if (!map.has(o.className)) map.set(o.className, []);
      map.get(o.className)!.push(o);
    }
    return Array.from(map.entries()).sort((a, b) => a[0].localeCompare(b[0]));
  }, [outstanding]);

  // 👇 Sort classes: Nursery → Primary → JSS → SS, numeric within each
  const sortedClasses = useMemo(() => {
    if (!data?.classes) return [];
    return [...data.classes].sort((a, b) => {
      const [sa, na, ta] = classSortKey(a.className);
      const [sb, nb, tb] = classSortKey(b.className);
      if (sa !== sb) return sa - sb;
      if (na !== nb) return na - nb;
      return ta.localeCompare(tb);
    });
  }, [data]);

  const downloadSchoolResultsPdf = async () => {
    if (!data) return;
    setDownloading(true);
    try {
      const payload = {
        academicSessionName: data.academicSessionName,
        termName: data.termName,
        totalStudents: data.totalStudents,
        totalClasses: data.totalClasses,
        totalComplete: data.totalComplete,
        totalPending: data.totalPending,
        overallAverage: data.overallAverage,
        classes: sortedClasses.map((c) => ({
          className: c.className,
          studentCount: c.studentCount,
          classAverage: c.classAverage,
          highestAverage: c.highestAverage,
          lowestAverage: c.lowestAverage,
          completeCount: c.completeCount,
          pendingCount: c.pendingCount,
          students: c.streams.flatMap((s) =>
            s.students.map((st) => ({
              position: st.position,
              studentName: st.studentName,
              admissionNumber: st.admissionNumber,
              averageScore: st.averageScore,
              overallGrade: st.overallGrade,
              submittedSubjectCount: st.submittedSubjectCount,
              totalSubjectCount: st.totalSubjectCount,
              isComplete: st.isComplete,
            }))
          ),
        })),
      };

      await downloadPdf(
        "/pdfreport/school-results",
        payload,
        `SchoolResults_${data.termName}.pdf`
      );
    } catch (err: any) {
      toast({
        variant: "error",
        title: err?.response?.data?.message ?? "Could not generate PDF",
      });
    } finally {
      setDownloading(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="School results"
        description="Live view — results appear as soon as each teacher submits their subject."
        actions={
          <Button
            icon={Download}
            loading={downloading}
            disabled={!data}
            onClick={downloadSchoolResultsPdf}
          >
            Download PDF
          </Button>
        }
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
            options={
              session?.terms.map((t) => ({ value: t.id, label: t.name })) ?? []
            }
          />
        </div>
      </Card>

      {!loading && outstanding.length > 0 && (
        <Card padding="none">
          <button
            type="button"
            onClick={() => setShowOutstanding((v) => !v)}
            className="w-full px-6 py-4 flex items-center justify-between text-left hover:bg-surface-50"
          >
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-xl bg-amber-50 grid place-items-center">
                <Clock className="w-4 h-4 text-amber-600" />
              </div>
              <div>
                <h3 className="text-[14px] font-semibold text-surface-900">
                  Waiting on teachers
                </h3>
                <p className="text-[12px] text-surface-500 mt-0.5">
                  {outstanding.length} subject
                  {outstanding.length === 1 ? "" : "s"} not yet submitted
                </p>
              </div>
            </div>
            <span className="text-[12px] text-surface-500">
              {showOutstanding ? "Hide" : "Show"}
            </span>
          </button>

          {showOutstanding && (
            <div className="border-t border-surface-100 divide-y divide-surface-100">
              {outstandingByClass.map(([className, items]) => (
                <div key={className} className="px-6 py-3">
                  <p className="text-[12px] font-semibold text-surface-700 mb-2">
                    {className}
                  </p>
                  <div className="space-y-1.5">
                    {items.map((o) => (
                      <div
                        key={`${o.classId}-${o.subjectId}`}
                        className="flex items-center justify-between gap-3 text-[12px]"
                      >
                        <div className="flex items-center gap-2 min-w-0">
                          <AlertCircle className="w-3.5 h-3.5 text-amber-500 shrink-0" />
                          <span className="text-surface-800 truncate">
                            {o.subjectName}
                          </span>
                          <span className="text-surface-400">·</span>
                          <span className="text-surface-500 truncate">
                            {o.teacherName}
                          </span>
                        </div>
                        <span className="text-[11px] text-surface-500 tabular-nums shrink-0">
                          {o.scoredCount > 0
                            ? `${o.scoredCount}/${o.studentCount} saved`
                            : "not started"}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {loading ? (
        <Card>
          <Skeleton className="h-64" />
        </Card>
      ) : !data ? (
        <Card>
          <EmptyState
            icon={BarChart3}
            title="No results available"
            description="Once teachers start submitting scores, they will appear here."
          />
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Students
              </p>
              <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
                {data.totalStudents}
              </p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Classes
              </p>
              <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
                {data.totalClasses}
              </p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Complete
              </p>
              <p className="text-2xl font-semibold text-brand-600 tabular-nums mt-1">
                {data.totalComplete}
              </p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
                Waiting on teachers
              </p>
              <p className="text-2xl font-semibold text-flame-600 tabular-nums mt-1">
                {data.totalPending}
              </p>
            </Card>
          </div>

          {/* 👇 Iterate over sortedClasses instead of data.classes */}
          {sortedClasses.map((cls) => (
            <Card key={cls.classId} padding="none">
              <div className="p-6 pb-4 flex items-center justify-between gap-4 flex-wrap">
                <div>
                  <h3 className="text-[15px] font-semibold text-surface-900">
                    {cls.className}
                  </h3>
                  <p className="text-[12px] text-surface-400 mt-0.5">
                    {cls.studentCount} students
                    {cls.classAverage > 0 && (
                      <> - avg {cls.classAverage.toFixed(1)}%</>
                    )}
                    {" · "}
                    {cls.completeCount} complete, {cls.pendingCount} waiting
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  {cls.highestAverage > 0 && (
                    <Badge variant="emerald">
                      High {cls.highestAverage.toFixed(1)}%
                    </Badge>
                  )}
                  {cls.lowestAverage > 0 && (
                    <Badge variant="neutral">
                      Low {cls.lowestAverage.toFixed(1)}%
                    </Badge>
                  )}
                </div>
              </div>

              {cls.streams.map((stream, si) => (
                <div key={si} className="border-t border-surface-100">
                  <div className="px-6 py-2 bg-surface-50/50 flex items-center justify-between">
                    <p className="text-[11px] font-semibold uppercase tracking-wider text-surface-500">
                      {stream.streamName} - {stream.studentCount} students
                    </p>
                    {stream.streamAverage > 0 && (
                      <span className="text-[11px] text-surface-500 tabular-nums">
                        Avg {stream.streamAverage.toFixed(1)}%
                      </span>
                    )}
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead>
                        <tr className="bg-white">
                          <th className="text-left text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-2 w-16">
                            Pos
                          </th>
                          <th className="text-left text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2">
                            Student
                          </th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2 w-32">
                            Subjects
                          </th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2 w-24">
                            Average
                          </th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2 w-20">
                            Grade
                          </th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-2 w-32">
                            Status
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-surface-100">
                        {stream.students.map((s) => {
                          const hasData = s.subjectCount > 0;
                          return (
                            <tr
                              key={s.termResultId}
                              className="hover:bg-surface-50/50"
                            >
                              <td className="px-6 py-2.5 text-[12px] text-surface-500 tabular-nums">
                                {!hasData ? (
                                  <span className="text-surface-300">-</span>
                                ) : s.position <= 3 ? (
                                  <Trophy className="w-3.5 h-3.5 text-flame-500" />
                                ) : (
                                  `#${s.position}`
                                )}
                              </td>
                              <td className="px-4 py-2.5">
                                <div className="flex items-center gap-2">
                                  <Avatar
                                    name={s.studentName}
                                    src={s.profilePictureUrl}
                                    size="sm"
                                  />
                                  <div>
                                    <p className="text-[12px] font-medium text-surface-800">
                                      {s.studentName}
                                    </p>
                                    <p className="text-[10px] text-surface-400 font-mono">
                                      {s.admissionNumber}
                                    </p>
                                  </div>
                                </div>
                              </td>
                              <td className="px-4 py-2.5 text-right">
                                {s.totalSubjectCount > 0 ? (
                                  <span
                                    className={
                                      "text-[12px] tabular-nums " +
                                      (s.isComplete
                                        ? "text-emerald-600 font-semibold"
                                        : "text-surface-700")
                                    }
                                  >
                                    {s.submittedSubjectCount}/
                                    {s.totalSubjectCount}
                                  </span>
                                ) : hasData ? (
                                  <span className="text-[12px] text-surface-700 tabular-nums">
                                    {s.subjectCount}
                                  </span>
                                ) : (
                                  <span className="text-[11px] text-flame-500 inline-flex items-center gap-1">
                                    <AlertCircle className="w-3 h-3" />
                                    none
                                  </span>
                                )}
                              </td>
                              <td className="px-4 py-2.5 text-right text-[13px] font-semibold text-surface-900 tabular-nums">
                                {hasData ? `${s.averageScore.toFixed(1)}%` : "-"}
                              </td>
                              <td className="px-4 py-2.5 text-right">
                                {hasData ? (
                                  <Badge
                                    variant={
                                      s.overallGrade === "A"
                                        ? "emerald"
                                        : s.overallGrade === "F"
                                        ? "rose"
                                        : "sky"
                                    }
                                  >
                                    {s.overallGrade}
                                  </Badge>
                                ) : (
                                  <span className="text-surface-300">-</span>
                                )}
                              </td>
                              <td className="px-6 py-2.5 text-right">
                                {s.isComplete ? (
                                  <Badge variant="emerald">
                                    <CheckCircle2 className="w-3 h-3 mr-1 inline" />
                                    Complete
                                  </Badge>
                                ) : hasData ? (
                                  <Badge variant="amber">Waiting</Badge>
                                ) : (
                                  <Badge variant="neutral">No scores</Badge>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              ))}
            </Card>
          ))}
        </>
      )}
    </div>
  );
}
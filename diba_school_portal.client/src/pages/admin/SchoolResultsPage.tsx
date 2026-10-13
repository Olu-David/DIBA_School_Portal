import { useEffect, useState } from "react";
import { BarChart3, Trophy } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Skeleton, Select, Avatar, Badge } from "../../components/ui";
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
    isPublished: boolean;
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
  publishedCount: number;
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
  totalPublished: number;
  totalPending: number;
  overallAverage: number;
  classes: ClassGroup[];
}

interface Session { id: string; name: string; isCurrentSession: boolean; terms: { id: string; name: string; isCurrentTerm: boolean }[]; }

export default function SchoolResultsPage() {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [data, setData] = useState<SchoolResult | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    (async () => {
      const { data: res } = await api.get<ApiResponse<{ items: Session[] }>>(
        "/academicsession/all?pageNumber=1&pageSize=20"
      );
      if (res.success) {
        setSessions(res.data.items);
        const current = res.data.items.find((s) => s.isCurrentSession) ?? res.data.items[0];
        if (current) {
          setSessionId(current.id);
          const t = current.terms.find((x) => x.isCurrentTerm) ?? current.terms[0];
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
        const { data: res } = await api.get<ApiResponse<SchoolResult>>(
          `/termsubjectresult/school?academicSessionId=${sessionId}&termId=${termId}`
        );
        setData(res.success ? res.data : null);
      } catch {
        setData(null);
      } finally {
        setLoading(false);
      }
    })();
  }, [sessionId, termId]);

  const session = sessions.find((s) => s.id === sessionId);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="School results"
        description="Overall performance across every class and stream."
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

      {loading ? (
        <Card><Skeleton className="h-64" /></Card>
      ) : !data ? (
        <Card>
          <EmptyState
            icon={BarChart3}
            title="No results available"
            description="Once teachers submit scores, they will appear here."
          />
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">Students</p>
              <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">{data.totalStudents}</p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">Classes</p>
              <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">{data.totalClasses}</p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">Published</p>
              <p className="text-2xl font-semibold text-brand-600 tabular-nums mt-1">{data.totalPublished}</p>
            </Card>
            <Card padding="sm">
              <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">Pending</p>
              <p className="text-2xl font-semibold text-flame-600 tabular-nums mt-1">{data.totalPending}</p>
            </Card>
          </div>

          {data.classes.map((cls) => (
            <Card key={cls.classId} padding="none">
              <div className="p-6 pb-4 flex items-center justify-between gap-4 flex-wrap">
                <div>
                  <h3 className="text-[15px] font-semibold text-surface-900">{cls.className}</h3>
                  <p className="text-[12px] text-surface-400 mt-0.5">
                    {cls.studentCount} students - avg {cls.classAverage.toFixed(1)}%
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant="emerald">High {cls.highestAverage.toFixed(1)}%</Badge>
                  <Badge variant="neutral">Low {cls.lowestAverage.toFixed(1)}%</Badge>
                </div>
              </div>

              {cls.streams.map((stream, si) => (
                <div key={si} className="border-t border-surface-100">
                  <div className="px-6 py-2 bg-surface-50/50 flex items-center justify-between">
                    <p className="text-[11px] font-semibold uppercase tracking-wider text-surface-500">
                      {stream.streamName} - {stream.studentCount} students
                    </p>
                    <span className="text-[11px] text-surface-500 tabular-nums">
                      Avg {stream.streamAverage.toFixed(1)}%
                    </span>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead>
                        <tr className="bg-white">
                          <th className="text-left text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-2 w-16">Pos</th>
                          <th className="text-left text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2">Student</th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2 w-24">Average</th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-2 w-20">Grade</th>
                          <th className="text-right text-[10px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-2 w-28">Status</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-surface-100">
                        {stream.students.map((s) => (
                          <tr key={s.termResultId} className="hover:bg-surface-50/50">
                            <td className="px-6 py-2.5 text-[12px] text-surface-500 tabular-nums">
                              {s.position <= 3 ? <Trophy className="w-3.5 h-3.5 text-flame-500" /> : `#${s.position}`}
                            </td>
                            <td className="px-4 py-2.5">
                              <div className="flex items-center gap-2">
                                <Avatar name={s.studentName} src={s.profilePictureUrl} size="sm" />
                                <div>
                                  <p className="text-[12px] font-medium text-surface-800">{s.studentName}</p>
                                  <p className="text-[10px] text-surface-400 font-mono">{s.admissionNumber}</p>
                                </div>
                              </div>
                            </td>
                            <td className="px-4 py-2.5 text-right text-[13px] font-semibold text-surface-900 tabular-nums">
                              {s.averageScore.toFixed(1)}%
                            </td>
                            <td className="px-4 py-2.5 text-right">
                              <Badge variant={s.overallGrade === "A" ? "emerald" : s.overallGrade === "F" ? "rose" : "sky"}>
                                {s.overallGrade}
                              </Badge>
                            </td>
                            <td className="px-6 py-2.5 text-right">
                              {s.isPublished ? <Badge variant="emerald">Published</Badge> : <Badge variant="amber">Pending</Badge>}
                            </td>
                          </tr>
                        ))}
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
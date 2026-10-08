import { useEffect, useState } from "react";
import { FileText, Download, Trophy, TrendingUp } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, Skeleton, Badge, Select } from "../../components/ui";
import { useAuth } from "../../hooks/useAuth";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface TermResult {
  averageScore: number;
  classPosition: number;
  overallGrade: string | null;
  subjectResults: { id: string; subjectName: string; test1Score: number; test2Score: number; examScore: number; totalScore: number; subjectPosition: number; grade: string; remark: string }[];
}
interface Session { id: string; name: string; isCurrentSession: boolean; terms: { id: string; name: string; isCurrentTerm: boolean }[]; }

export default function MyResultsPage() {
  const { user } = useAuth();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [report, setReport] = useState<TermResult | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<{ items: Session[] }>>("/academicsession/all?pageNumber=1&pageSize=20");
      if (data.success) {
        setSessions(data.data.items);
        const c = data.data.items.find((s) => s.isCurrentSession);
        if (c) {
          setSessionId(c.id);
          const t = c.terms.find((x) => x.isCurrentTerm) ?? c.terms[0];
          if (t) setTermId(t.id);
        }
      }
    })();
  }, []);

  useEffect(() => {
    if (!sessionId || !termId || !user?.id) return;
    setLoading(true);
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<TermResult>>(
          `/termsubjectresult/report-card/${user.id}?termId=${termId}&academicSessionId=${sessionId}`
        );
        setReport(data.success ? data.data : null);
      } catch { setReport(null); }
      finally { setLoading(false); }
    })();
  }, [sessionId, termId, user?.id]);

  const session = sessions.find((s) => s.id === sessionId);

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Academics" title="My results" description="View your term report card and subject breakdown." />

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
        <Card><Skeleton className="h-96" /></Card>
      ) : !report ? (
        <Card>
          <EmptyState icon={FileText} title="No results available" description="Your report card has not been published for this term yet." />
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-3 gap-4">
            <Card padding="sm" className="text-center">
              <div className="w-10 h-10 rounded-xl bg-sky-50 grid place-items-center mx-auto mb-2">
                <TrendingUp className="w-4 h-4 text-sky-600" />
              </div>
              <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">Average</p>
              <p className="text-xl font-semibold text-slate-900 tabular-nums">{report.averageScore.toFixed(1)}%</p>
            </Card>
            <Card padding="sm" className="text-center">
              <div className="w-10 h-10 rounded-xl bg-amber-50 grid place-items-center mx-auto mb-2">
                <Trophy className="w-4 h-4 text-amber-600" />
              </div>
              <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">Position</p>
              <p className="text-xl font-semibold text-slate-900 tabular-nums">#{report.classPosition}</p>
            </Card>
            <Card padding="sm" className="text-center">
              <div className="w-10 h-10 rounded-xl bg-emerald-50 grid place-items-center mx-auto mb-2">
                <FileText className="w-4 h-4 text-emerald-600" />
              </div>
              <p className="text-[10px] font-medium uppercase tracking-wider text-slate-400">Grade</p>
              <p className="text-xl font-semibold text-slate-900">{report.overallGrade ?? "-"}</p>
            </Card>
          </div>

          <Card padding="none">
            <div className="p-6 pb-4 flex items-center justify-between">
              <h3 className="text-[15px] font-semibold text-slate-900">Subject breakdown</h3>
              <Button variant="secondary" size="sm" icon={Download}>Download PDF</Button>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/50">
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Subject</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Test 1</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Test 2</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Exam</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Total</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Grade</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Pos</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3">Remark</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {report.subjectResults.map((s) => (
                    <tr key={s.id} className="hover:bg-slate-50/50">
                      <td className="px-5 py-3 text-[13px] font-medium text-slate-800">{s.subjectName}</td>
                      <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">{s.test1Score}</td>
                      <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">{s.test2Score}</td>
                      <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">{s.examScore}</td>
                      <td className="px-5 py-3 text-right tabular-nums text-[13px] font-semibold text-slate-900">{s.totalScore}</td>
                      <td className="px-5 py-3 text-right">
                        <Badge variant="sky">{s.grade}</Badge>
                      </td>
                      <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">#{s.subjectPosition}</td>
                      <td className="px-5 py-3 text-[12px] text-slate-500">{s.remark}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </>
      )}
    </div>
  );
}

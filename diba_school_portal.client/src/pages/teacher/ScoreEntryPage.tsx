import { useEffect, useState } from "react";
import { Save, Send, AlertCircle, ClipboardList } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Input, Select, Skeleton, Avatar,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

interface ScoreRow {
  studentId: string;
  studentName: string;
  test1Score: number;
  test2Score: number;
  examScore: number;
}

export default function ScoreEntryPage() {
  const { toast } = useToast();
  const [sessions, setSessions] = useState<any[]>([]);
  const [classes, setClasses] = useState<any[]>([]);
  const [subjects, setSubjects] = useState<any[]>([]);

  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [classId, setClassId] = useState("");
  const [subjectId, setSubjectId] = useState("");

  const [rows, setRows] = useState<ScoreRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    (async () => {
      const [s, c, sub] = await Promise.all([
        api.get<ApiResponse<{ items: any[] }>>("/academicsession/all?pageNumber=1&pageSize=20"),
        api.get<ApiResponse<any[]>>("/schoolclass/all"),
        api.get<ApiResponse<any[]>>("/subject/all"),
      ]);
      if (s.data.success) {
        setSessions(s.data.data.items);
        const current = s.data.data.items.find((x) => x.isCurrentSession);
        if (current) {
          setSessionId(current.id);
          const t = current.terms.find((x: any) => x.isCurrentTerm) ?? current.terms[0];
          if (t) setTermId(t.id);
        }
      }
      if (c.data.success) setClasses(c.data.data);
      if (sub.data.success) setSubjects(sub.data.data);
    })();
  }, []);

  useEffect(() => {
    if (!classId || !subjectId || !termId || !sessionId) return;
    setLoading(true);
    (async () => {
      try {
        const { data } = await api.get<
          ApiResponse<Paginated<ScoreRow>>
        >(
          `/termsubjectresult/score-sheet?classId=${classId}&subjectId=${subjectId}&termId=${termId}&academicSessionId=${sessionId}&pageSize=100`
        );
        if (data.success) {
          setRows(data.data.items);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [classId, subjectId, termId, sessionId]);

  const update = (studentId: string, key: keyof ScoreRow, value: number) => {
    setRows((prev) =>
      prev.map((r) => (r.studentId === studentId ? { ...r, [key]: value } : r))
    );
  };

  const submit = async (isFinished: boolean) => {
    setSaving(true);
    try {
      const { data } = await api.post<ApiResponse<string>>(
        "/termsubjectresult/save-scores",
        {
          classId,
          subjectId,
          termId,
          academicSessionId: sessionId,
          isFinished,
          studentScores: rows.map((r) => ({
            studentId: r.studentId,
            test1Score: r.test1Score,
            test2Score: r.test2Score,
            examScore: r.examScore,
          })),
        }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }
      toast({
        variant: "success",
        title: isFinished ? "Scores submitted" : "Draft saved",
      });
    } finally {
      setSaving(false);
    }
  };

  const session = sessions.find((s) => s.id === sessionId);
  const canSubmit = classId && subjectId && termId && sessionId && rows.length > 0;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Teaching"
        title="Score entry"
        description="Enter CA and exam scores. You can save a draft and submit later."
        actions={
          canSubmit && (
            <>
              <Button variant="secondary" icon={Save} loading={saving} onClick={() => submit(false)}>
                Save draft
              </Button>
              <Button icon={Send} loading={saving} onClick={() => submit(true)}>
                Submit final
              </Button>
            </>
          )
        }
      />

      <Card padding="sm">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
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
            options={session?.terms.map((t: any) => ({ value: t.id, label: t.name })) ?? []}
          />
          <Select
            value={classId}
            onChange={(e) => setClassId(e.target.value)}
            placeholder="Choose class"
            options={classes.map((c) => ({ value: c.id, label: c.name }))}
          />
          <Select
            value={subjectId}
            onChange={(e) => setSubjectId(e.target.value)}
            placeholder="Choose subject"
            options={subjects.map((s) => ({ value: s.id, label: s.name }))}
          />
        </div>
      </Card>

      {!classId || !subjectId ? (
        <Card>
          <EmptyState
            icon={ClipboardList}
            title="Pick a class and subject"
            description="Your score sheet will appear once both are selected."
          />
        </Card>
      ) : loading ? (
        <Card>
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
          </div>
        </Card>
      ) : rows.length === 0 ? (
        <Card>
          <EmptyState
            icon={ClipboardList}
            title="No students enrolled"
            description="Students appear once they're enrolled in this class and registered for the subject."
          />
        </Card>
      ) : (
        <Card padding="none">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-surface-100 bg-surface-50/50">
                  <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                    Student
                  </th>
                  <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                    Test 1 <span className="text-surface-400">/20</span>
                  </th>
                  <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                    Test 2 <span className="text-surface-400">/20</span>
                  </th>
                  <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                    Exam <span className="text-surface-400">/60</span>
                  </th>
                  <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                    Total
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-surface-100">
                {rows.map((r) => {
                  const total = r.test1Score + r.test2Score + r.examScore;
                  const invalid =
                    r.test1Score < 0 || r.test1Score > 20 ||
                    r.test2Score < 0 || r.test2Score > 20 ||
                    r.examScore < 0 || r.examScore > 60;

                  return (
                    <tr key={r.studentId} className={invalid ? "bg-flame-50/30" : ""}>
                      <td className="px-6 py-3">
                        <div className="flex items-center gap-3">
                          <Avatar name={r.studentName} size="sm" />
                          <p className="text-[13px] font-medium text-surface-800">{r.studentName}</p>
                          {invalid && (
                            <span title="Score out of range" className="text-flame-500">
                              <AlertCircle className="w-4 h-4" />
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Input
                          type="number"
                          value={String(r.test1Score)}
                          onChange={(e) => update(r.studentId, "test1Score", Number(e.target.value))}
                          className="max-w-20 ml-auto text-right"
                        />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Input
                          type="number"
                          value={String(r.test2Score)}
                          onChange={(e) => update(r.studentId, "test2Score", Number(e.target.value))}
                          className="max-w-20 ml-auto text-right"
                        />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Input
                          type="number"
                          value={String(r.examScore)}
                          onChange={(e) => update(r.studentId, "examScore", Number(e.target.value))}
                          className="max-w-20 ml-auto text-right"
                        />
                      </td>
                      <td className="px-6 py-3 text-right">
                        <span className={`text-[13px] font-semibold tabular-nums ${invalid ? "text-flame-600" : "text-surface-900"}`}>
                          {total}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </div>
  );
}
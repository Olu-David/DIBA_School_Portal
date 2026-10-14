import { useEffect, useState } from "react";
import { useParams, useSearchParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  Printer,
  Users,
  TrendingUp,
  AlertCircle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, Button, Badge, Skeleton, EmptyState } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { downloadPdf } from "../../lib/downloadPdf";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface SheetRow {
  position: number;
  studentName: string;
  admissionNumber: string;
  test1Score: number;
  test2Score: number;
  examScore: number;
  totalScore: number;
  grade: string;
  remark: string;
}

interface ClassSheet {
  className: string;
  subjectName: string;
  subjectCode: string;
  teacherName: string;
  termName: string;
  academicSessionName: string;
  isSubmitted: boolean;
  classAverage: number;
  highestScore: number;
  lowestScore: number;
  passCount: number;
  failCount: number;
  students: SheetRow[];
}

export default function TeacherClassSheetPage() {
  const { classId, subjectId } = useParams<{
    classId: string;
    subjectId: string;
  }>();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { toast } = useToast();

  const termId = params.get("termId") ?? "";
  const sessionId = params.get("sessionId") ?? "";

  const [data, setData] = useState<ClassSheet | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    if (!classId || !subjectId || !termId || !sessionId) {
      setLoading(false);
      return;
    }
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<ClassSheet>>(
          `/termsubjectresult/class-sheet?classId=${classId}&subjectId=${subjectId}` +
            `&termId=${termId}&academicSessionId=${sessionId}`
        );
        if (res.success) setData(res.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [classId, subjectId, termId, sessionId]);

  const downloadPdfFile = async () => {
    if (!data) return;
    setDownloading(true);
    try {
      await downloadPdf(
        "/pdfreport/teacher-class-sheet",
        data,
        `ClassSheet_${data.className}_${data.subjectName}.pdf`
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

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <Card>
          <Skeleton className="h-96" />
        </Card>
      </div>
    );
  }

  if (!data) {
    return (
      <Card>
        <EmptyState
          icon={Users}
          title="Sheet not available"
          description="No scores have been entered for this class and subject."
          action={
            <Button onClick={() => navigate(-1)}>Back</Button>
          }
        />
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate(-1)}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        Back
      </button>

      <PageHeader
        eyebrow="Results"
        title={`${data.subjectName} — ${data.className}`}
        description={`${data.termName} · ${data.academicSessionName} · ${data.teacherName}`}
        actions={
          <div className="flex gap-2">
            <Button
              variant="secondary"
              icon={Printer}
              onClick={() => window.print()}
            >
              Print
            </Button>
            <Button
              icon={Download}
              loading={downloading}
              onClick={downloadPdfFile}
            >
              Download PDF
            </Button>
          </div>
        }
      />

      {/* Status strip */}
      <Card>
        <div className="flex flex-wrap items-center gap-4">
          <Badge variant={data.isSubmitted ? "emerald" : "amber"}>
            {data.isSubmitted ? "Submitted" : "Draft"}
          </Badge>
          <span className="text-[12px] text-surface-500">
            {data.students.length} students
          </span>
        </div>
      </Card>

      {/* KPIs */}
      <div className="grid grid-cols-2 sm:grid-cols-5 gap-4">
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Students
          </p>
          <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
            {data.students.length}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Class avg
          </p>
          <p className="text-2xl font-semibold text-brand-600 tabular-nums mt-1">
            {data.classAverage.toFixed(1)}%
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Highest
          </p>
          <p className="text-2xl font-semibold text-emerald-600 tabular-nums mt-1">
            {data.highestScore.toFixed(1)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Lowest
          </p>
          <p className="text-2xl font-semibold text-flame-600 tabular-nums mt-1">
            {data.lowestScore.toFixed(1)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Pass / Fail
          </p>
          <p className="text-2xl font-semibold tabular-nums mt-1">
            <span className="text-emerald-600">{data.passCount}</span>
            <span className="text-surface-300"> / </span>
            <span className="text-flame-600">{data.failCount}</span>
          </p>
        </Card>
      </div>

      {/* Students table */}
      <Card padding="none">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-surface-100 bg-surface-50/50">
                <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3 w-14">
                  Pos
                </th>
                <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Student
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  T1
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  T2
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Exam
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Total
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Grade
                </th>
                <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                  Remark
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-100">
              {data.students.map((s) => {
                const hasScore = s.totalScore > 0;
                return (
                  <tr key={s.admissionNumber} className="hover:bg-surface-50/50">
                    <td className="px-6 py-3 text-[12px] text-surface-500 tabular-nums">
                      {s.position || "-"}
                    </td>
                    <td className="px-4 py-3">
                      <p className="text-[13px] font-medium text-surface-900">
                        {s.studentName}
                      </p>
                      <p className="text-[10px] text-surface-400 font-mono">
                        {s.admissionNumber}
                      </p>
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] tabular-nums">
                      {s.test1Score.toFixed(1)}
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] tabular-nums">
                      {s.test2Score.toFixed(1)}
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] tabular-nums">
                      {s.examScore.toFixed(1)}
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] font-semibold tabular-nums">
                      {hasScore ? s.totalScore.toFixed(1) : "—"}
                    </td>
                    <td className="px-4 py-3 text-right">
                      {hasScore ? (
                        <Badge
                          variant={
                            s.grade === "A"
                              ? "emerald"
                              : s.grade === "F"
                              ? "rose"
                              : "sky"
                          }
                        >
                          {s.grade}
                        </Badge>
                      ) : (
                        <span className="text-[11px] text-flame-500 inline-flex items-center gap-1">
                          <AlertCircle className="w-3 h-3" />
                          none
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-3 text-[12px] text-surface-600">
                      {hasScore ? s.remark : "—"}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
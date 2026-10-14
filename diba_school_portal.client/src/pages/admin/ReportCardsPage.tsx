import { useEffect, useState } from "react";
import { useParams, useSearchParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  Printer,
  Trophy,
  GraduationCap,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card,
  Button,
  Avatar,
  Badge,
  Skeleton,
  EmptyState,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { downloadPdf } from "../../lib/downloadPdf";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface SubjectScore {
  id: string;
  subjectId: string;
  subjectName: string;
  test1Score: number;
  test2Score: number;
  examScore: number;
  totalScore: number;
  subjectPosition: number;
  grade: string;
  remark: string;
}

interface ReportCard {
  termResultId: string;
  studentId: string;
  studentName: string;
  admissionNumber: string | null;
  profilePictureUrl: string | null;
  classId: string;
  className: string;
  stream: number | null;
  termId: string;
  termName: string;
  academicSessionId: string;
  sessionName: string;
  averageScore: number;
  classPosition: number;
  overallGrade: string;
  subjectResults: SubjectScore[];
}

const STREAM_LABEL: Record<number, string> = {
  0: "Science",
  1: "Arts",
  2: "Commercial",
  3: "General",
};

export default function ReportCardPage() {
  const { studentId } = useParams<{ studentId: string }>();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { toast } = useToast();

  const termId = params.get("termId") ?? "";
  const sessionId = params.get("sessionId") ?? "";

  const [data, setData] = useState<ReportCard | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    if (!studentId || !termId || !sessionId) {
      setLoading(false);
      return;
    }
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<ReportCard>>(
          `/termsubjectresult/report-card/${studentId}` +
            `?termId=${termId}&academicSessionId=${sessionId}`
        );
        if (res.success) setData(res.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [studentId, termId, sessionId]);

  const downloadPdfFile = async () => {
    if (!data) return;
    setDownloading(true);
    try {
      await downloadPdf(
        "/pdfreport/report-card",
        data,
        `ReportCard_${data.studentName}_${data.termName}.pdf`
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
          icon={GraduationCap}
          title="Report card not available"
          description="No results found for this student in the selected term."
          action={
            <Button onClick={() => navigate("/admin/report-cards")}>Back</Button>
          }
        />
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/report-cards")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All report cards
      </button>

      <PageHeader
        eyebrow="Results"
        title="Report card"
        description={`${data.termName} · ${data.sessionName}`}
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

      {/* Student hero */}
      <Card>
        <div className="flex flex-col sm:flex-row sm:items-center gap-5">
          <Avatar name={data.studentName} src={data.profilePictureUrl} size="xl" />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h1 className="text-2xl font-semibold text-surface-900">
                {data.studentName}
              </h1>
              <Badge
                variant={
                  data.overallGrade === "A"
                    ? "emerald"
                    : data.overallGrade === "F"
                    ? "rose"
                    : "sky"
                }
              >
                {data.overallGrade}
              </Badge>
              {data.stream !== null && (
                <Badge variant="violet">
                  {STREAM_LABEL[data.stream] ?? "Stream"}
                </Badge>
              )}
            </div>
            <p className="mt-1 text-[13px] text-surface-500 font-mono">
              {data.admissionNumber ?? "—"}
            </p>
            <p className="mt-1 text-[12px] text-surface-500">
              {data.className} · {data.sessionName} · {data.termName}
            </p>
          </div>
        </div>
      </Card>

      {/* KPIs */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Average score
          </p>
          <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
            {data.averageScore.toFixed(2)}%
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Class position
          </p>
          <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1 inline-flex items-center gap-2">
            {data.classPosition <= 3 && data.classPosition > 0 && (
              <Trophy className="w-5 h-5 text-flame-500" />
            )}
            {data.classPosition > 0 ? `#${data.classPosition}` : "-"}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Subjects taken
          </p>
          <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
            {data.subjectResults.length}
          </p>
        </Card>
      </div>

      {/* Subjects table */}
      <Card padding="none">
        <div className="p-6 pb-4">
          <h3 className="text-[15px] font-semibold text-surface-900">
            Subject performance
          </h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-surface-100 bg-surface-50/50">
                <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                  Subject
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Test 1
                </th>
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Test 2
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
                <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-4 py-3">
                  Pos
                </th>
                <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                  Remark
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-100">
              {data.subjectResults.map((s) => (
                <tr key={s.id} className="hover:bg-surface-50/50">
                  <td className="px-6 py-3 text-[13px] font-medium text-surface-900">
                    {s.subjectName}
                  </td>
                  <td className="px-4 py-3 text-right text-[13px] text-surface-700 tabular-nums">
                    {s.test1Score.toFixed(1)}
                  </td>
                  <td className="px-4 py-3 text-right text-[13px] text-surface-700 tabular-nums">
                    {s.test2Score.toFixed(1)}
                  </td>
                  <td className="px-4 py-3 text-right text-[13px] text-surface-700 tabular-nums">
                    {s.examScore.toFixed(1)}
                  </td>
                  <td className="px-4 py-3 text-right text-[13px] font-semibold text-surface-900 tabular-nums">
                    {s.totalScore.toFixed(1)}
                  </td>
                  <td className="px-4 py-3 text-right">
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
                  </td>
                  <td className="px-4 py-3 text-right text-[12px] text-surface-500 tabular-nums">
                    {s.subjectPosition}
                  </td>
                  <td className="px-6 py-3 text-[12px] text-surface-600">
                    {s.remark}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
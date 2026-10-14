import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  Printer,
  BookOpen,
  GraduationCap,
  Users,
  TrendingUp,
  Trophy,
  CheckCircle2,
  Edit3,
  Clock,
  AlertCircle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card,
  Button,
  Badge,
  Skeleton,
  EmptyState,
  Avatar,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { downloadPdf } from "../../lib/downloadPdf";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface ResultRow {
  studentId: string;
  admissionNumber: string;
  studentName: string;
  profilePictureUrl: string | null;
  test1Score: number;
  test2Score: number;
  examScore: number;
  totalScore: number;
  grade: string;
  remark: string;
  subjectPosition: number;
  isDraft: boolean;
}

interface ClassSubjectResult {
  classId: string;
  className: string;
  section: number;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  termId: string;
  termName: string;
  academicSessionId: string;
  academicSessionName: string;
  teacherName: string;
  submissionStatus: string;
  studentCount: number;
  scoredCount: number;
  classAverage: number;
  highestScore: number;
  lowestScore: number;
  passCount: number;
  failCount: number;
  students: ResultRow[];
}

export default function ClassSubjectResultsPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { toast } = useToast();

  const classId = params.get("classId") ?? "";
  const subjectId = params.get("subjectId") ?? "";
  const termId = params.get("termId") ?? "";
  const sessionId = params.get("academicSessionId") ?? "";

  const [data, setData] = useState<ClassSubjectResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    if (!classId || !subjectId || !termId || !sessionId) {
      setLoading(false);
      return;
    }
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<ClassSubjectResult>>(
          `/termsubjectresult/class-subject-results?classId=${classId}` +
            `&subjectId=${subjectId}&termId=${termId}&academicSessionId=${sessionId}`
        );
        if (res.success) setData(res.data);
      } catch (err: any) {
        toast({
          variant: "error",
          title: err?.response?.data?.message ?? "Could not load results",
        });
      } finally {
        setLoading(false);
      }
    })();
  }, [classId, subjectId, termId, sessionId, toast]);

  const downloadPdfFile = async () => {
    if (!data) return;
    setDownloading(true);
    try {
      await downloadPdf(
        "/pdfreport/teacher-class-sheet",
        {
          className: data.className,
          subjectName: data.subjectName,
          subjectCode: data.subjectCode,
          teacherName: data.teacherName,
          termName: data.termName,
          academicSessionName: data.academicSessionName,
          isSubmitted: data.submissionStatus === "Submitted",
          classAverage: data.classAverage,
          highestScore: data.highestScore,
          lowestScore: data.lowestScore,
          passCount: data.passCount,
          failCount: data.failCount,
          students: data.students.map((s) => ({
            position: s.subjectPosition,
            studentName: s.studentName,
            admissionNumber: s.admissionNumber,
            test1Score: s.test1Score,
            test2Score: s.test2Score,
            examScore: s.examScore,
            totalScore: s.totalScore,
            grade: s.grade,
            remark: s.remark,
          })),
        },
        `ClassSheet_${data.className}_${data.subjectName}.pdf`
      );
    } catch (err: any) {
      toast({
        variant: "error",
        title: err?.response?.data?.message ?? "Could not download PDF",
      });
    } finally {
      setDownloading(false);
    }
  };

  const openScoreEntry = () => {
    navigate(
      `/admin/score-entry?classId=${classId}&subjectId=${subjectId}` +
        `&termId=${termId}&academicSessionId=${sessionId}`
    );
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
          icon={BookOpen}
          title="Results not available"
          description="No scores have been entered for this class and subject yet."
          action={<Button onClick={() => navigate(-1)}>Back</Button>}
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
            <Button variant="secondary" icon={Printer} onClick={() => window.print()}>
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
          {data.submissionStatus === "Submitted" ? (
            <Badge variant="emerald">
              <CheckCircle2 className="w-3 h-3 mr-1 inline" />
              Submitted
            </Badge>
          ) : data.submissionStatus === "Draft" ? (
            <Badge variant="amber">
              <Edit3 className="w-3 h-3 mr-1 inline" />
              Draft
            </Badge>
          ) : (
            <Badge variant="neutral">
              <Clock className="w-3 h-3 mr-1 inline" />
              Not started
            </Badge>
          )}
          <span className="text-[12px] text-surface-500">
            {data.scoredCount} of {data.studentCount} scored
          </span>
          <div className="flex-1" />
          <Button variant="secondary" size="sm" onClick={openScoreEntry}>
            Edit scores
          </Button>
        </div>
      </Card>

      {/* KPIs */}
      <div className="grid grid-cols-2 sm:grid-cols-5 gap-4">
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Students
          </p>
          <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
            {data.studentCount}
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

      {/* Table */}
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
                  <tr key={s.studentId} className="hover:bg-surface-50/50">
                    <td className="px-6 py-3 text-[12px] text-surface-500 tabular-nums">
                      {!hasScore ? (
                        <span className="text-surface-300">-</span>
                      ) : s.subjectPosition <= 3 ? (
                        <Trophy className="w-3.5 h-3.5 text-flame-500" />
                      ) : (
                        `#${s.subjectPosition}`
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <Avatar
                          name={s.studentName}
                          src={s.profilePictureUrl}
                          size="sm"
                        />
                        <div className="min-w-0">
                          <p className="text-[12px] font-medium text-surface-800 truncate">
                            {s.studentName}
                          </p>
                          <p className="text-[10px] text-surface-400 font-mono">
                            {s.admissionNumber}
                          </p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] tabular-nums">
                      {hasScore ? s.test1Score.toFixed(1) : "—"}
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] tabular-nums">
                      {hasScore ? s.test2Score.toFixed(1) : "—"}
                    </td>
                    <td className="px-4 py-3 text-right text-[13px] tabular-nums">
                      {hasScore ? s.examScore.toFixed(1) : "—"}
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
import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, Download, FileText } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, Skeleton, Badge } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function ReportCardViewer() {
  const { studentId, termId, sessionId } = useParams<{
    studentId: string; termId: string; sessionId: string;
  }>();
  const navigate = useNavigate();
  const [report, setReport] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [pdfLoading, setPdfLoading] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<any>>(
          `/termsubjectresult/report-card/${studentId}?termId=${termId}&academicSessionId=${sessionId}`
        );
        if (data.success) setReport(data.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [studentId, termId, sessionId]);

  const downloadPdf = async () => {
    if (!report) return;
    setPdfLoading(true);
    try {
      const res = await api.post(
        "/pdfreport/report-card",
        report,
        { responseType: "blob" }
      );
      const url = URL.createObjectURL(new Blob([res.data], { type: "application/pdf" }));
      const a = document.createElement("a");
      a.href = url;
      a.download = `ReportCard_${report.studentName}_${report.termName}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      setPdfLoading(false);
    }
  };

  if (loading) return <Card><Skeleton className="h-96" /></Card>;

  if (!report) {
    return (
      <Card>
        <EmptyState
          icon={FileText}
          title="Report card not found"
          description="It may not have been published yet."
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
        className="inline-flex items-center gap-1.5 text-[12px] text-slate-500 hover:text-slate-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        Back
      </button>

      <PageHeader
        eyebrow={`${report.sessionName} · ${report.termName}`}
        title={report.studentName}
        description={`Class: ${report.className}`}
        actions={
          <Button icon={Download} loading={pdfLoading} onClick={downloadPdf}>
            Download PDF
          </Button>
        }
      />

      <div className="grid grid-cols-3 gap-4">
        <Card padding="sm" className="text-center">
          <p className="text-[10px] uppercase tracking-wider text-slate-400 font-medium">Average</p>
          <p className="text-2xl font-semibold text-slate-900 tabular-nums mt-1">
            {report.averageScore?.toFixed(2)}%
          </p>
        </Card>
        <Card padding="sm" className="text-center">
          <p className="text-[10px] uppercase tracking-wider text-slate-400 font-medium">Position</p>
          <p className="text-2xl font-semibold text-slate-900 tabular-nums mt-1">
            #{report.classPosition}
          </p>
        </Card>
        <Card padding="sm" className="text-center">
          <p className="text-[10px] uppercase tracking-wider text-slate-400 font-medium">Grade</p>
          <p className="text-2xl font-semibold text-slate-900 mt-1">{report.overallGrade ?? "—"}</p>
        </Card>
      </div>

      <Card padding="none">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-slate-100 bg-slate-50/50">
                {["Subject", "Test 1", "Test 2", "Exam", "Total", "Grade", "Pos", "Remark"].map((h) => (
                  <th key={h} className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-5 py-3 whitespace-nowrap">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {report.subjectResults.map((s: any) => (
                <tr key={s.id} className="hover:bg-slate-50/50">
                  <td className="px-5 py-3 text-[13px] font-medium text-slate-800">{s.subjectName}</td>
                  <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">{s.test1Score}</td>
                  <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">{s.test2Score}</td>
                  <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">{s.examScore}</td>
                  <td className="px-5 py-3 text-right tabular-nums text-[13px] font-semibold text-slate-900">{s.totalScore}</td>
                  <td className="px-5 py-3 text-right">
                    <Badge variant={s.grade === "A" ? "emerald" : s.grade === "F" ? "rose" : "sky"}>{s.grade}</Badge>
                  </td>
                  <td className="px-5 py-3 text-right tabular-nums text-[13px] text-slate-600">#{s.subjectPosition}</td>
                  <td className="px-5 py-3 text-[12px] text-slate-500">{s.remark}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { FileText, Calendar, ArrowRight, GraduationCap, Download } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Skeleton, Badge, Button } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import { useAuth } from "../../hooks/useAuth";
import type { ApiResponse } from "../../types/common";

interface Session {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

export default function MyResultsPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { toast } = useToast();

  const [sessions, setSessions] = useState<Session[]>([]);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState<string | null>(null); // "termId"

  const studentId = user?.id ?? "";

  useEffect(() => {
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<{ items: Session[] }>>(
          "/academicsession/all?pageNumber=1&pageSize=20"
        );
        if (res.success) setSessions(res.data.items ?? []);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const openReportCard = (termId: string, sessionId: string) => {
    if (!studentId) return;
    navigate(`/student/results/${studentId}/${sessionId}/${termId}`);
  };

  const downloadPdf = async (termId: string, sessionId: string) => {
    if (!studentId) return;
    setDownloading(termId);
    try {
      const res = await api.get(
        `/termsubjectresult/report-card/${studentId}/pdf` +
          `?termId=${termId}&academicSessionId=${sessionId}`,
        { responseType: "blob" }
      );

      const disposition = (res.headers?.["content-disposition"] as string) ?? "";
      const match = disposition.match(/filename="?([^";]+)"?/i);
      const filename = match?.[1] ?? `ReportCard_${termId}.pdf`;

      const blob = new Blob([res.data], { type: "application/pdf" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err: any) {
      if (err?.response?.data instanceof Blob) {
        try {
          const text = await err.response.data.text();
          const json = JSON.parse(text);
          toast({ variant: "error", title: json?.message ?? "Download failed" });
          return;
        } catch {
          /* fall through */
        }
      }
      toast({
        variant: "error",
        title: err?.response?.data?.message ?? "Could not download PDF",
      });
    } finally {
      setDownloading(null);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Results"
        title="My results"
        description="View or download your term report cards."
      />

      {loading ? (
        <Card>
          <Skeleton className="h-40" />
        </Card>
      ) : sessions.length === 0 ? (
        <Card>
          <EmptyState
            icon={FileText}
            title="No results yet"
            description="Report cards appear once your teachers finish submitting scores."
          />
        </Card>
      ) : (
        <div className="space-y-4">
          {sessions.map((s) => (
            <Card key={s.id} padding="none">
              <div className="p-6 pb-3 flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-brand-50 grid place-items-center">
                  <Calendar className="w-5 h-5 text-brand-600" />
                </div>
                <div className="flex-1">
                  <h3 className="text-[15px] font-semibold text-surface-900">
                    {s.name}
                  </h3>
                  {s.isCurrentSession && (
                    <Badge variant="emerald">Current session</Badge>
                  )}
                </div>
              </div>

              <div className="divide-y divide-surface-100 border-t border-surface-100">
                {s.terms.map((t) => (
                  <div
                    key={t.id}
                    className="px-6 py-4 flex items-center gap-3"
                  >
                    <GraduationCap className="w-4 h-4 text-surface-400" />
                    <div className="flex-1 min-w-0">
                      <p className="text-[13px] font-medium text-surface-900">
                        {t.name}
                      </p>
                      {t.isCurrentTerm && (
                        <p className="text-[11px] text-emerald-600 mt-0.5">
                          Current term
                        </p>
                      )}
                    </div>

                    <Button
                      size="sm"
                      variant="ghost"
                      icon={Download}
                      loading={downloading === t.id}
                      onClick={() => downloadPdf(t.id, s.id)}
                    >
                      PDF
                    </Button>

                    <Button
                      size="sm"
                      variant="secondary"
                      iconRight={ArrowRight}
                      onClick={() => openReportCard(t.id, s.id)}
                    >
                      View
                    </Button>
                  </div>
                ))}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
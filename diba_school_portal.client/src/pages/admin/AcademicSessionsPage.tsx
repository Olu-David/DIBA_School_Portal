import { useEffect, useState } from "react";
import {
  CalendarDays, Plus, CheckCircle2, ChevronRight, BookMarked,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, Badge, EmptyState, Spinner } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";
import type { AcademicSession } from "../../types/academics";

export default function AcademicSessionsPage() {
  const { toast } = useToast();
  const [sessions, setSessions] = useState<AcademicSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [settingId, setSettingId] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      // Backend returns PaginatedViewModel<AcademicSessionDto> → items live on .data.items
      const { data } = await api.get<
        ApiResponse<Paginated<AcademicSession>>
      >("/academicsession/all?pageNumber=1&pageSize=50");

      if (data.success) {
        setSessions(data.data.items ?? []);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const setCurrent = async (sessionId: string, termId: string) => {
    setSettingId(sessionId);
    try {
      await api.post(
        `/academicsession/set-current?sessionId=${sessionId}&termId=${termId}`
      );
      toast({ variant: "success", title: "Current session updated" });
      await load();
    } catch {
      toast({ variant: "error", title: "Could not set current session" });
    } finally {
      setSettingId(null);
    }
  };

  // ... rest of the component body is unchanged from what I sent before
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academic calendar"
        title="Sessions & terms"
        description="Manage every academic year and its three terms."
        actions={<Button icon={Plus}>New session</Button>}
      />

      {loading ? (
        <Card>
          <div className="py-16 flex justify-center">
            <Spinner size="lg" label="Loading sessions…" />
          </div>
        </Card>
      ) : sessions.length === 0 ? (
        <Card>
          <EmptyState
            icon={CalendarDays}
            title="No academic sessions"
            description="Create your first session to unlock terms, fees, and results."
            action={<Button icon={Plus}>New session</Button>}
          />
        </Card>
      ) : (
        <div className="space-y-4">
          {sessions.map((s) => {
            const currentTerm = s.terms.find((t) => t.isCurrentTerm);
            return (
              <Card
                key={s.id}
                className={
                  s.isCurrentSession
                    ? "ring-2 ring-emerald-100 border-emerald-200"
                    : ""
                }
              >
                <div className="flex flex-col lg:flex-row lg:items-center gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 mb-2">
                      <div
                        className={`w-11 h-11 rounded-xl grid place-items-center ${
                          s.isCurrentSession
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-slate-100 text-slate-500"
                        }`}
                      >
                        <CalendarDays className="w-5 h-5" />
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <h3 className="text-[16px] font-semibold text-slate-900">
                            {s.name}
                          </h3>
                          {s.isCurrentSession && (
                            <Badge variant="emerald">
                              <CheckCircle2 className="w-3 h-3" />
                              Current
                            </Badge>
                          )}
                          {!s.isActive && <Badge variant="neutral">Closed</Badge>}
                        </div>
                        <p className="text-[12px] text-slate-400 mt-0.5">
                          {formatDate(s.startDate)} → {formatDate(s.endDate)}
                        </p>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-3 gap-2 lg:w-[420px]">
                    {s.terms.map((t) => {
                      const isCurrent = t.isCurrentTerm;
                      return (
                        <button
                          key={t.id}
                          type="button"
                          disabled={settingId === s.id}
                          onClick={() => !isCurrent && setCurrent(s.id, t.id)}
                          className={`text-left p-3 rounded-xl border transition-all ${
                            isCurrent
                              ? "border-emerald-200 bg-emerald-50"
                              : "border-slate-100 hover:border-slate-200 hover:bg-slate-50"
                          } disabled:cursor-wait`}
                        >
                          <div className="flex items-center gap-1.5 mb-1">
                            <BookMarked
                              className={`w-3 h-3 ${
                                isCurrent ? "text-emerald-600" : "text-slate-400"
                              }`}
                            />
                            <p
                              className={`text-[11px] font-medium ${
                                isCurrent ? "text-emerald-700" : "text-slate-500"
                              }`}
                            >
                              {t.name.replace(" Term", "")}
                            </p>
                          </div>
                          <p className="text-[10px] text-slate-400">
                            {formatDate(t.startDate).slice(0, 6)}
                          </p>
                        </button>
                      );
                    })}
                  </div>

                  <div className="lg:w-32 flex lg:justify-end">
                    <button
                      type="button"
                      className="inline-flex items-center gap-1.5 text-[12px] font-medium text-emerald-600 hover:text-emerald-700"
                    >
                      Manage
                      <ChevronRight className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                {s.isCurrentSession && currentTerm && (
                  <div className="mt-5 pt-4 border-t border-emerald-100 flex items-center gap-2 text-[12px] text-emerald-700">
                    <CheckCircle2 className="w-3.5 h-3.5" />
                    Running:{" "}
                    <strong className="font-semibold">{currentTerm.name}</strong>
                    <span className="text-emerald-500">
                      ({formatDate(currentTerm.startDate)} –{" "}
                      {formatDate(currentTerm.endDate)})
                    </span>
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
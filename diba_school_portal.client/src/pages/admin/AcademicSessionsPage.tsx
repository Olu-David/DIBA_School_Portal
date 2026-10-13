import { useEffect, useState, type FormEvent } from "react";
import {
  CalendarDays, Plus, CheckCircle2, BookMarked, X, AlertCircle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Badge, EmptyState, Spinner, FormField, Input,
} from "../../components/ui";
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

  // ---- Create dialog state ----
  const [createOpen, setCreateOpen] = useState(false);
  const [createForm, setCreateForm] = useState({
    name: "",
    startDate: "",
    endDate: "",
  });
  const [creating, setCreating] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
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

  // ---- Submit new session ----
  const submitCreate = async (e: FormEvent) => {
    e.preventDefault();

    if (!createForm.name.trim() || !createForm.startDate) {
      toast({ variant: "error", title: "Name and start date are required" });
      return;
    }

    setCreating(true);
    try {
      // Build a DTO that matches AcademicSessionDto on the backend
      const payload = {
        name: createForm.name.trim(),
        startDate: createForm.startDate,
        endDate: createForm.endDate || null,
        isCurrentSession: false,
        isActive: true,
      };

      const { data } = await api.post<ApiResponse<boolean>>(
        "/academicsession/create",
        payload
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to create" });
        return;
      }

      toast({
        variant: "success",
        title: "Session created with 3 terms",
      });
      setCreateOpen(false);
      setCreateForm({ name: "", startDate: "", endDate: "" });
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not create session",
      });
    } finally {
      setCreating(false);
    }
  };

  // ---- Has a session ended? ----
  const hasEnded = (endDate: string) => {
    if (!endDate) return false;
    return new Date(endDate) < new Date();
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academic calendar"
        title="Sessions & terms"
        description="Manage every academic year and its three terms."
        actions={
          <Button icon={Plus} onClick={() => setCreateOpen(true)}>
            New session
          </Button>
        }
      />

      {loading ? (
        <Card>
          <div className="py-16 flex justify-center">
            <Spinner size="lg" label="Loading sessions..." />
          </div>
        </Card>
      ) : sessions.length === 0 ? (
        <Card>
          <EmptyState
            icon={CalendarDays}
            title="No academic sessions"
            description="Create your first session to unlock terms, fees, and results."
            action={
              <Button icon={Plus} onClick={() => setCreateOpen(true)}>
                New session
              </Button>
            }
          />
        </Card>
      ) : (
        <div className="space-y-4">
          {sessions.map((s) => {
            const currentTerm = s.terms.find((t) => t.isCurrentTerm);
            const ended = hasEnded(s.endDate);
            const closed = !s.isActive || ended;

            return (
              <Card
                key={s.id}
                className={
                  s.isCurrentSession
                    ? "ring-2 ring-brand-100 border-brand-200"
                    : ""
                }
              >
                <div className="flex flex-col lg:flex-row lg:items-center gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-3 mb-2">
                      <div
                        className={`w-11 h-11 rounded-xl grid place-items-center ${
                          s.isCurrentSession
                            ? "bg-brand-100 text-brand-700"
                            : "bg-surface-100 text-surface-500"
                        }`}
                      >
                        <CalendarDays className="w-5 h-5" />
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <h3 className="text-[16px] font-semibold text-surface-900">
                            {s.name}
                          </h3>
                          {s.isCurrentSession && (
                            <Badge variant="emerald">
                              <CheckCircle2 className="w-3 h-3" />
                              Current
                            </Badge>
                          )}
                          {closed && !s.isCurrentSession && (
                            <Badge variant="neutral">Closed</Badge>
                          )}
                        </div>
                        <p className="text-[12px] text-surface-400 mt-0.5">
                          {formatDate(s.startDate)} to {formatDate(s.endDate)}
                        </p>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-3 gap-2 lg:w-[420px]">
                    {s.terms.map((t) => {
                      const isCurrent = t.isCurrentTerm;
                      const termEnded = hasEnded(t.endDate);
                      const termClosed = termEnded || !s.isActive;

                      return (
                        <button
                          key={t.id}
                          type="button"
                          disabled={
                            settingId === s.id || termClosed || isCurrent
                          }
                          onClick={() =>
                            !isCurrent && !termClosed && setCurrent(s.id, t.id)
                          }
                          className={`text-left p-3 rounded-xl border transition-all ${
                            isCurrent
                              ? "border-brand-200 bg-brand-50"
                              : termClosed
                              ? "border-surface-100 bg-surface-50/50 opacity-60 cursor-not-allowed"
                              : "border-surface-100 hover:border-surface-200 hover:bg-surface-50"
                          } disabled:cursor-wait`}
                        >
                          <div className="flex items-center gap-1.5 mb-1">
                            <BookMarked
                              className={`w-3 h-3 ${
                                isCurrent
                                  ? "text-brand-600"
                                  : termClosed
                                  ? "text-surface-300"
                                  : "text-surface-400"
                              }`}
                            />
                            <p
                              className={`text-[11px] font-medium ${
                                isCurrent
                                  ? "text-brand-700"
                                  : termClosed
                                  ? "text-surface-400"
                                  : "text-surface-500"
                              }`}
                            >
                              {t.name.replace(" Term", "")}
                            </p>
                          </div>
                          <p className="text-[10px] text-surface-400">
                            {formatDate(t.startDate).slice(0, 6)}
                          </p>
                          {termClosed && (
                            <p className="text-[9px] text-surface-400 mt-0.5">
                              Ended
                            </p>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </div>

                {s.isCurrentSession && currentTerm && (
                  <div className="mt-5 pt-4 border-t border-brand-100 flex items-center gap-2 text-[12px] text-brand-700">
                    <CheckCircle2 className="w-3.5 h-3.5" />
                    Running:{" "}
                    <strong className="font-semibold">{currentTerm.name}</strong>
                    <span className="text-brand-500">
                      ({formatDate(currentTerm.startDate)} to{" "}
                      {formatDate(currentTerm.endDate)})
                    </span>
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}

      {/* ==========================================================
          CREATE SESSION DIALOG
         ========================================================== */}
      {createOpen && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !creating && setCreateOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
              <div className="p-5 flex items-center justify-between border-b border-surface-100">
                <h2 className="text-[15px] font-semibold text-surface-900">
                  New academic session
                </h2>
                <button
                  type="button"
                  onClick={() => !creating && setCreateOpen(false)}
                  className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <form onSubmit={submitCreate} className="p-5 space-y-4">
                <FormField
                  label="Session name"
                  required
                  hint="e.g. 2025/2026"
                >
                  <Input
                    value={createForm.name}
                    onChange={(e) =>
                      setCreateForm((f) => ({ ...f, name: e.target.value }))
                    }
                    placeholder="2025/2026"
                  />
                </FormField>

                <FormField label="Start date" required>
                  <Input
                    type="date"
                    value={createForm.startDate}
                    onChange={(e) =>
                      setCreateForm((f) => ({ ...f, startDate: e.target.value }))
                    }
                  />
                </FormField>

                <FormField
                  label="End date"
                  hint="Optional. Defaults to 1 year after start."
                >
                  <Input
                    type="date"
                    value={createForm.endDate}
                    onChange={(e) =>
                      setCreateForm((f) => ({ ...f, endDate: e.target.value }))
                    }
                  />
                </FormField>

                <div className="flex items-start gap-3 p-3 rounded-xl bg-brand-50 border border-brand-100 text-[12px] text-brand-800">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>
                    Three terms (First, Second, Third) will be created
                    automatically, evenly spaced across the year.
                  </span>
                </div>

                <div className="pt-2 flex justify-end gap-3">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => setCreateOpen(false)}
                    disabled={creating}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" icon={Plus} loading={creating}>
                    Create session
                  </Button>
                </div>
              </form>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
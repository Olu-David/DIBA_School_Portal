import { useEffect, useState, type FormEvent } from "react";
import {
  AlertCircle, MessageCircle, Download, Phone, X, Send,
  CheckCircle2,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Skeleton, Select, Avatar, Badge,
  FormField, Textarea,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface ArrearsRow {
  studentProfileId: string;
  studentName: string;
  admissionNumber: string;
  className: string;
  guardianPhone: string;
  expected: number;
  paid: number;
  outstanding: number;
  isFullyPaid: boolean;
}

interface Session {
  id: string;
  name: string;
  isCurrentSession: boolean;
  terms: { id: string; name: string; isCurrentTerm: boolean }[];
}

// Rough cost per SMS segment. Termii is ~₦4 for a 160-char page.
const SMS_COST_PER_SEGMENT = 4;
const SMS_CHARS_PER_SEGMENT = 160;

export default function ArrearsPage() {
  const { toast } = useToast();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [rows, setRows] = useState<ArrearsRow[]>([]);
  const [loading, setLoading] = useState(true);

  // ---- SMS compose modal ----
  const [modalOpen, setModalOpen] = useState(false);
  const [message, setMessage] = useState(
    "DIBA: Dear parent, your child's term fees are still outstanding. Please settle at the bursary or via the parent portal. Thank you."
  );
  const [sending, setSending] = useState(false);

  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<{ items: Session[] }>>(
        "/academicsession/all?pageNumber=1&pageSize=20"
      );
      if (data.success) {
        setSessions(data.data.items);
        const current = data.data.items.find((s) => s.isCurrentSession);
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
        const { data } = await api.get<ApiResponse<ArrearsRow[]>>(
          `/account/arrears?academicSessionId=${sessionId}&termId=${termId}`
        );
        if (data.success) setRows(data.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [sessionId, termId]);

  const session = sessions.find((s) => s.id === sessionId);
  const totalOwed = rows.reduce((s, r) => s + r.outstanding, 0);

  const recipientsWithPhone = rows.filter((r) => r.guardianPhone?.trim());

  // Estimate cost: one SMS per recipient, multiplied by segments
  const segments = Math.max(
    1,
    Math.ceil(message.length / SMS_CHARS_PER_SEGMENT)
  );
  const estimatedCost = recipientsWithPhone.length * segments * SMS_COST_PER_SEGMENT;

  const sendReminders = async (e: FormEvent) => {
    e.preventDefault();

    if (!message.trim()) {
      toast({ variant: "error", title: "Message cannot be empty" });
      return;
    }
    if (recipientsWithPhone.length === 0) {
      toast({ variant: "error", title: "No parents with a phone number to notify" });
      return;
    }

    setSending(true);
    try {
      const { data } = await api.post<ApiResponse<{ recipientCount: number; queueId: string }>>(
        "/notificationdispatch/sms",
        {
          target: "Arrears",
          message: message.trim(),
          academicSessionId: sessionId,
          termId,
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Could not queue SMS" });
        return;
      }

      toast({
        variant: "success",
        title: `Queued ${data.data.recipientCount} SMS`,
        description: "Sending in the background. Check the log for delivery.",
      });
      setModalOpen(false);
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not send reminders",
      });
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Arrears"
        description="Students with unpaid or partially paid fees for the selected term."
        actions={
          <>
            <Button variant="secondary" icon={Download}>Export</Button>
            <Button
              variant="secondary"
              icon={MessageCircle}
              onClick={() => setModalOpen(true)}
              disabled={loading || rows.length === 0}
            >
              Send reminders
            </Button>
          </>
        }
      />

      {/* Session / term selector */}
      <Card padding="sm">
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="flex-1">
            <Select
              value={sessionId}
              onChange={(e) => {
                setSessionId(e.target.value);
                const s = sessions.find((x) => x.id === e.target.value);
                if (s?.terms[0])
                  setTermId(s.terms.find((t) => t.isCurrentTerm)?.id ?? s.terms[0].id);
              }}
              options={sessions.map((s) => ({ value: s.id, label: s.name }))}
            />
          </div>
          <div className="flex-1">
            <Select
              value={termId}
              onChange={(e) => setTermId(e.target.value)}
              options={session?.terms.map((t) => ({ value: t.id, label: t.name })) ?? []}
            />
          </div>
        </div>
      </Card>

      {/* Alert banner */}
      {!loading && rows.length > 0 && (
        <div className="bg-gradient-to-br from-flame-50 to-flame-100/40 border border-flame-200 rounded-2xl p-5 flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-white grid place-items-center shadow-sm shrink-0">
            <AlertCircle className="w-5 h-5 text-flame-600" />
          </div>
          <div className="flex-1">
            <p className="text-[14px] font-semibold text-surface-900">
              {rows.length} student{rows.length !== 1 ? "s" : ""} owing
            </p>
            <p className="text-[12px] text-surface-600 mt-0.5">
              Total outstanding: <strong className="text-flame-600">{formatNaira(totalOwed)}</strong>
              {" · "}
              {recipientsWithPhone.length} of them have a guardian phone on file
            </p>
          </div>
        </div>
      )}

      {/* Table */}
      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-12" />)}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={AlertCircle}
            title="No arrears"
            description="Every student has paid their fees for this term."
          />
        ) : (
          <>
            <div className="hidden lg:block overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50/50">
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Student</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Class</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Guardian</th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Expected</th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Paid</th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Outstanding</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-100">
                  {rows.map((r) => (
                    <tr key={r.studentProfileId} className="hover:bg-surface-50/50">
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          <Avatar name={r.studentName} size="sm" />
                          <div>
                            <p className="text-[13px] font-medium text-surface-800">{r.studentName}</p>
                            <p className="text-[11px] text-surface-400 font-mono">{r.admissionNumber}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4"><Badge variant="sky">{r.className}</Badge></td>
                      <td className="px-6 py-4">
                        {r.guardianPhone ? (
                          <a
                            href={`tel:${r.guardianPhone}`}
                            className="inline-flex items-center gap-1.5 text-[12px] text-surface-600 hover:text-brand-700"
                          >
                            <Phone className="w-3 h-3" />
                            {r.guardianPhone}
                          </a>
                        ) : (
                          <span className="text-[12px] text-surface-400">No phone</span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right text-[13px] text-surface-600 tabular-nums">{formatNaira(r.expected)}</td>
                      <td className="px-6 py-4 text-right text-[13px] text-surface-600 tabular-nums">{formatNaira(r.paid)}</td>
                      <td className="px-6 py-4 text-right">
                        <span className="text-[13px] font-semibold text-flame-600 tabular-nums">
                          {formatNaira(r.outstanding)}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="lg:hidden divide-y divide-surface-100">
              {rows.map((r) => (
                <div key={r.studentProfileId} className="p-4">
                  <div className="flex items-center gap-3">
                    <Avatar name={r.studentName} size="md" />
                    <div className="flex-1 min-w-0">
                      <p className="text-[13px] font-medium text-surface-800 truncate">{r.studentName}</p>
                      <p className="text-[11px] text-surface-400 font-mono">
                        {r.admissionNumber} - {r.className}
                      </p>
                    </div>
                    <p className="text-[13px] font-semibold text-flame-600 tabular-nums">
                      {formatNaira(r.outstanding)}
                    </p>
                  </div>
                  {r.guardianPhone && (
                    <a
                      href={`tel:${r.guardianPhone}`}
                      className="mt-3 inline-flex items-center gap-1.5 text-[12px] text-brand-600"
                    >
                      <Phone className="w-3 h-3" />
                      {r.guardianPhone}
                    </a>
                  )}
                </div>
              ))}
            </div>
          </>
        )}
      </Card>

      {/* ==========================================================
          SEND REMINDERS MODAL
         ========================================================== */}
      {modalOpen && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !sending && setModalOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg overflow-hidden">
              <div className="p-5 flex items-center justify-between border-b border-surface-100">
                <div className="flex items-center gap-2">
                  <MessageCircle className="w-4 h-4 text-brand-600" />
                  <h2 className="text-[15px] font-semibold text-surface-900">
                    Send fee reminders via SMS
                  </h2>
                </div>
                <button
                  type="button"
                  onClick={() => !sending && setModalOpen(false)}
                  className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <form onSubmit={sendReminders} className="p-5 space-y-4">
                <div className="rounded-xl bg-brand-50 border border-brand-100 p-3 text-[12px] text-brand-800 leading-relaxed">
                  This will send an SMS to <strong>{recipientsWithPhone.length}</strong>{" "}
                  guardian{recipientsWithPhone.length !== 1 ? "s" : ""} of students
                  owing for <strong>{session?.name}</strong> - <strong>{session?.terms.find((t) => t.id === termId)?.name}</strong>.
                </div>

                <FormField label="Message" required hint={`${message.length} chars · ${segments} segment${segments !== 1 ? "s" : ""} per SMS`}>
                  <Textarea
                    rows={5}
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    maxLength={480}
                    className="resize-none"
                  />
                </FormField>

                <div className="flex items-center justify-between p-3 rounded-xl bg-surface-50 border border-surface-100 text-[12px]">
                  <span className="text-surface-500">Estimated cost</span>
                  <span className="font-semibold text-surface-900 tabular-nums">
                    ₦{estimatedCost.toLocaleString()}
                  </span>
                </div>

                <div className="pt-2 flex justify-end gap-3">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={() => setModalOpen(false)}
                    disabled={sending}
                  >
                    Cancel
                  </Button>
                  <Button
                    type="submit"
                    icon={Send}
                    loading={sending}
                    disabled={recipientsWithPhone.length === 0}
                  >
                    Send {recipientsWithPhone.length} SMS
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
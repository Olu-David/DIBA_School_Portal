import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { BadgeDollarSign, Search } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button,
  Card,
  FormField,
  Input,
  Select,
  Textarea,
  Avatar,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useDebounce } from "../../hooks/useDebounce";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";
import type { AcademicSession } from "../../types/academics";
import type { StudentRow } from "../../types/student";

interface CashPaymentResponse {
  referenceNumber: string;
}

export default function RecordCash() {
  const navigate = useNavigate();
  const { toast } = useToast();

  // Sessions and terms
  const [sessions, setSessions] = useState<AcademicSession[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");

  // Student search
  const [selectedStudent, setSelectedStudent] = useState<StudentRow | null>(null);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [results, setResults] = useState<StudentRow[]>([]);
  const [searching, setSearching] = useState(false);

  // Form
  const [amount, setAmount] = useState("");
  const [reference, setReference] = useState("");
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);

  // ---------- Load sessions once ----------
  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<
          ApiResponse<{ items: AcademicSession[] }>
        >("/academicsession/all?pageNumber=1&pageSize=20");
        if (!data.success) return;

        setSessions(data.data.items);
        const current = data.data.items.find((s) => s.isCurrentSession);
        if (current) {
          setSessionId(current.id);
          const t =
            current.terms.find((x) => x.isCurrentTerm) ?? current.terms[0];
          if (t) setTermId(t.id);
        }
      } catch {
        toast({ variant: "error", title: "Could not load academic sessions" });
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ---------- Search students ----------
  useEffect(() => {
    if (!debounced.trim()) {
      setResults([]);
      return;
    }
    setSearching(true);
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<Paginated<StudentRow>>>(
          `/student?searchTerm=${encodeURIComponent(debounced)}&pageNumber=1&pageSize=8`
        );
        if (data.success) setResults(data.data.items);
      } finally {
        setSearching(false);
      }
    })();
  }, [debounced]);

  // ---------- Handlers ----------
  const handleSearchChange = (e: ChangeEvent<HTMLInputElement>) =>
    setSearch(e.target.value);

  const handleSessionChange = (e: ChangeEvent<HTMLSelectElement>) => {
    const id = e.target.value;
    setSessionId(id);
    const s = sessions.find((x) => x.id === id);
    if (s && s.terms.length > 0) {
      const t = s.terms.find((x) => x.isCurrentTerm) ?? s.terms[0];
      setTermId(t.id);
    }
  };

  const handleTermChange = (e: ChangeEvent<HTMLSelectElement>) =>
    setTermId(e.target.value);

  const handleAmountChange = (e: ChangeEvent<HTMLInputElement>) =>
    setAmount(e.target.value);

  const handleReferenceChange = (e: ChangeEvent<HTMLInputElement>) =>
    setReference(e.target.value);

  const handleNoteChange = (e: ChangeEvent<HTMLTextAreaElement>) =>
    setNote(e.target.value);

  const submit = async () => {
    if (!selectedStudent) {
      toast({ variant: "error", title: "Pick a student first" });
      return;
    }
    const amt = parseFloat(amount);
    if (!amt || amt <= 0) {
      toast({ variant: "error", title: "Enter a valid amount" });
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<CashPaymentResponse>>(
        "/account/cash-payment",
        {
          studentProfileId: selectedStudent.id,
          academicSessionId: sessionId,
          termId,
          amountPaid: amt,
          externalReference: reference || undefined,
          note: note || undefined,
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }

      toast({
        variant: "success",
        title: "Cash payment recorded",
        description: `Ref ${data.data.referenceNumber} · ${formatNaira(amt)}`,
      });
      navigate("/admin/finance");
    } catch {
      toast({ variant: "error", title: "Could not record payment" });
    } finally {
      setSubmitting(false);
    }
  };

  const selectedSession = sessions.find((s) => s.id === sessionId);
  const amountNumber = parseFloat(amount) || 0;
  const newBalance = selectedStudent
    ? Math.max(0, selectedStudent.outstandingBalance - amountNumber)
    : 0;

  const isOverpaying =
    !!selectedStudent && amountNumber > selectedStudent.outstandingBalance;

  return (
    <div>
      <PageHeader
        eyebrow="Finance"
        title="Record cash payment"
        description="Log a payment made at the bursary. This immediately updates the student's balance."
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* LEFT — form */}
        <div className="lg:col-span-2 space-y-6">
          {/* Step 1 — student picker */}
          <Card>
            <div className="flex items-center gap-3 mb-5">
              <div className="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 grid place-items-center text-[13px] font-semibold">
                1
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-slate-900">
                  Select student
                </h2>
                <p className="text-[12px] text-slate-400">
                  Search by name or admission number
                </p>
              </div>
            </div>

            {selectedStudent ? (
              <div className="flex items-center justify-between p-4 rounded-xl border border-emerald-200 bg-emerald-50/40">
                <div className="flex items-center gap-3 min-w-0">
                  <Avatar
                    name={selectedStudent.fullName}
                    src={selectedStudent.profilePictureUrl}
                    size="lg"
                  />
                  <div className="min-w-0">
                    <p className="text-[14px] font-semibold text-slate-900 truncate">
                      {selectedStudent.fullName}
                    </p>
                    <p className="text-[12px] text-slate-500">
                      {selectedStudent.admissionNumber} · {selectedStudent.className}
                    </p>
                    {selectedStudent.outstandingBalance > 0 && (
                      <p className="text-[12px] mt-1 text-rose-600 font-medium">
                        Outstanding: {formatNaira(selectedStudent.outstandingBalance)}
                      </p>
                    )}
                  </div>
                </div>
                <Button
                  variant="ghost"
                  onClick={() => {
                    setSelectedStudent(null);
                    setSearch("");
                  }}
                >
                  Change
                </Button>
              </div>
            ) : (
              <>
                <Input
                  icon={Search}
                  placeholder="Type a name or admission number…"
                  value={search}
                  onChange={handleSearchChange}
                />

                {searching && (
                  <p className="text-[12px] text-slate-400 mt-3">Searching…</p>
                )}

                {!searching && results.length > 0 && (
                  <div className="mt-3 border border-slate-100 rounded-xl overflow-hidden divide-y divide-slate-100">
                    {results.map((s) => (
                      <button
                        key={s.id}
                        type="button"
                        onClick={() => {
                          setSelectedStudent(s);
                          setResults([]);
                        }}
                        className="w-full flex items-center gap-3 p-3 hover:bg-slate-50 text-left transition-colors"
                      >
                        <Avatar name={s.fullName} src={s.profilePictureUrl} size="sm" />
                        <div className="flex-1 min-w-0">
                          <p className="text-[13px] font-medium text-slate-800 truncate">
                            {s.fullName}
                          </p>
                          <p className="text-[11px] text-slate-400">
                            {s.admissionNumber} · {s.className}
                          </p>
                        </div>
                      </button>
                    ))}
                  </div>
                )}

                {!searching && debounced && results.length === 0 && (
                  <div className="mt-3 py-8 text-center text-[13px] text-slate-400">
                    No students found for &quot;{debounced}&quot;
                  </div>
                )}
              </>
            )}
          </Card>

          {/* Step 2 — payment details */}
          <Card>
            <div className="flex items-center gap-3 mb-5">
              <div className="w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 grid place-items-center text-[13px] font-semibold">
                2
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-slate-900">
                  Payment details
                </h2>
                <p className="text-[12px] text-slate-400">
                  Amount and reference information
                </p>
              </div>
            </div>

            <div className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField label="Session" required>
                  <Select
                    value={sessionId}
                    onChange={handleSessionChange}
                    options={sessions.map((s) => ({
                      value: s.id,
                      label: s.name,
                    }))}
                  />
                </FormField>
                <FormField label="Term" required>
                  <Select
                    value={termId}
                    onChange={handleTermChange}
                    options={
                      selectedSession?.terms.map((t) => ({
                        value: t.id,
                        label: t.name,
                      })) ?? []
                    }
                  />
                </FormField>
              </div>

              <FormField
                label="Amount (₦)"
                required
                hint="Enter the amount received in cash"
              >
                <Input
                  type="number"
                  inputMode="decimal"
                  placeholder="50000"
                  value={amount}
                  onChange={handleAmountChange}
                />
              </FormField>

              <FormField
                label="External reference"
                hint="Optional teller slip or receipt book number. Auto-generated if empty."
              >
                <Input
                  placeholder="e.g. TELLER-0045"
                  value={reference}
                  onChange={handleReferenceChange}
                />
              </FormField>

              <FormField label="Note">
                <Textarea
                  rows={3}
                  placeholder="e.g. Father paid in person at bursary"
                  value={note}
                  onChange={handleNoteChange}
                />
              </FormField>
            </div>
          </Card>
        </div>

        {/* RIGHT — summary */}
        <div className="space-y-6">
          <Card className="sticky top-24">
            <h3 className="text-[15px] font-semibold text-slate-900 mb-4">
              Summary
            </h3>

            <div className="space-y-3">
              <SummaryRow
                label="Student"
                value={selectedStudent?.fullName ?? "—"}
              />
              <SummaryRow
                label="Admission number"
                value={selectedStudent?.admissionNumber ?? "—"}
              />
              <SummaryRow
                label="Class"
                value={selectedStudent?.className ?? "—"}
              />
              <div className="pt-3 border-t border-slate-100" />
              <SummaryRow
                label="Current balance"
                value={
                  selectedStudent
                    ? formatNaira(selectedStudent.outstandingBalance)
                    : "—"
                }
              />
              <SummaryRow
                label="Paying now"
                value={amountNumber > 0 ? formatNaira(amountNumber) : "—"}
                highlight
              />
              <SummaryRow
                label="New balance"
                value={selectedStudent ? formatNaira(newBalance) : "—"}
              />
            </div>

            {isOverpaying && (
              <p className="mt-4 text-[11px] text-amber-600 bg-amber-50 border border-amber-100 rounded-lg p-2.5">
                Amount exceeds outstanding balance — overpayment will be credited.
              </p>
            )}

            <Button
              fullWidth
              size="lg"
              icon={BadgeDollarSign}
              className="mt-6"
              loading={submitting}
              onClick={submit}
              disabled={!selectedStudent || !amount}
            >
              Record payment
            </Button>

            <p className="mt-3 text-[11px] text-slate-400 text-center">
              This action is logged and cannot be undone.
            </p>
          </Card>
        </div>
      </div>
    </div>
  );
}

interface SummaryRowProps {
  label: string;
  value: string;
  highlight?: boolean;
}

function SummaryRow({ label, value, highlight = false }: SummaryRowProps) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span className="text-[12px] text-slate-500">{label}</span>
      <span
        className={
          highlight
            ? "text-[14px] font-semibold text-emerald-600"
            : "text-[13px] font-medium text-slate-800 truncate max-w-[60%] text-right"
        }
      >
        {value}
      </span>
    </div>
  );
}
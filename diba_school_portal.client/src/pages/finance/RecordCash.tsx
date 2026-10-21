import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { BadgeDollarSign, Search, Coins, User, Receipt, Wallet } from "lucide-react";
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

interface MiscIncomeResponse {
  referenceNumber: string;
}

type PaymentKind = "student" | "other";

const OTHER_CATEGORIES = [
  "Uniform",
  "Textbook",
  "ID Card",
  "Fine",
  "PTA",
  "Excursion",
  "Exam Fee",
  "Donation",
  "Other",
];

export default function RecordCash() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [kind, setKind] = useState<PaymentKind>("student");

  const [sessions, setSessions] = useState<AcademicSession[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");

  // ---------- Student payment ----------
  const [selectedStudent, setSelectedStudent] = useState<StudentRow | null>(null);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [results, setResults] = useState<StudentRow[]>([]);
  const [searching, setSearching] = useState(false);
  const [amount, setAmount] = useState("");
  const [reference, setReference] = useState("");
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);

  // 👇 NEW — set outstanding state
  const [outstandingAmount, setOutstandingAmount] = useState("");
  const [savingOutstanding, setSavingOutstanding] = useState(false);

  // ---------- Other income ----------
  const [category, setCategory] = useState("");
  const [description, setDescription] = useState("");
  const [otherAmount, setOtherAmount] = useState("");
  const [payerName, setPayerName] = useState("");
  const [payerPhone, setPayerPhone] = useState("");
  const [otherReference, setOtherReference] = useState("");
  const [otherNote, setOtherNote] = useState("");
  const [submittingOther, setSubmittingOther] = useState(false);

  // ---------- Load sessions ----------
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
    if (kind !== "student") return;
    if (!debounced.trim()) {
      setResults([]);
      return;
    }
    setSearching(true);
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<Paginated<StudentRow>>>(
          `/student?searchTerm=${encodeURIComponent(
            debounced
          )}&pageNumber=1&pageSize=8`
        );
        if (data.success) setResults(data.data.items);
      } finally {
        setSearching(false);
      }
    })();
  }, [debounced, kind]);

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

  // ---------- SUBMIT — Student payment ----------
  const submitStudent = async () => {
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

  // 👇 NEW — save outstanding
  const onSaveOutstanding = async () => {
    if (!selectedStudent) return;

    const amt = parseFloat(outstandingAmount);
    if (isNaN(amt) || amt < 0) {
      toast({ variant: "error", title: "Enter a valid amount (0 or more)." });
      return;
    }

    setSavingOutstanding(true);
    try {
      const { data } = await api.post<ApiResponse<number>>(
        `/account/student/${selectedStudent.id}/outstanding`,
        {
          academicSessionId: sessionId,
          termId,
          amount: amt,
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to save." });
        return;
      }

      toast({
        variant: "success",
        title: "Outstanding set",
        description: `New balance: ${formatNaira(data.data ?? amt)}`,
      });

      // Refresh the selected student's balance
      const { data: sd } = await api.get<ApiResponse<Paginated<StudentRow>>>(
        `/student?searchTerm=${encodeURIComponent(
          selectedStudent.admissionNumber
        )}&pageNumber=1&pageSize=1`
      );
      if (sd.success && sd.data.items[0]) {
        setSelectedStudent(sd.data.items[0]);
      }

      setOutstandingAmount("");
    } catch {
      toast({ variant: "error", title: "Could not save outstanding." });
    } finally {
      setSavingOutstanding(false);
    }
  };

  // ---------- SUBMIT — Other income ----------
  const submitOther = async () => {
    const amt = parseFloat(otherAmount);
    if (!category.trim()) {
      toast({ variant: "error", title: "Pick a category" });
      return;
    }
    if (!description.trim()) {
      toast({ variant: "error", title: "Description is required" });
      return;
    }
    if (!amt || amt <= 0) {
      toast({ variant: "error", title: "Enter a valid amount" });
      return;
    }

    setSubmittingOther(true);
    try {
      const { data } = await api.post<ApiResponse<MiscIncomeResponse>>(
        "/account/misc-income",
        {
          category: category.trim(),
          description: description.trim(),
          amount: amt,
          payerName: payerName.trim() || undefined,
          payerPhone: payerPhone.trim() || undefined,
          academicSessionId: sessionId,
          termId,
          gateway: "Cash",
          externalReference: otherReference || undefined,
          note: otherNote || undefined,
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }

      toast({
        variant: "success",
        title: "Income recorded",
        description: `Ref ${data.data.referenceNumber} · ${formatNaira(amt)}`,
      });
      navigate("/admin/finance");
    } catch {
      toast({ variant: "error", title: "Could not record income" });
    } finally {
      setSubmittingOther(false);
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
        title="Record cash"
        description="Log money received at the bursary — either a fee payment for a student, or other income."
      />

      {/* ---------- KIND TOGGLE ---------- */}
      <div className="mb-6">
        <div className="inline-flex rounded-xl border border-slate-200 bg-white p-1">
          <button
            type="button"
            onClick={() => setKind("student")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-[13px] font-medium transition-colors ${
              kind === "student"
                ? "bg-emerald-600 text-white shadow-sm"
                : "text-slate-600 hover:text-slate-900"
            }`}
          >
            <User className="w-4 h-4" />
            Student fee payment
          </button>
          <button
            type="button"
            onClick={() => setKind("other")}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-[13px] font-medium transition-colors ${
              kind === "other"
                ? "bg-amber-500 text-white shadow-sm"
                : "text-slate-600 hover:text-slate-900"
            }`}
          >
            <Coins className="w-4 h-4" />
            Other income
          </button>
        </div>
        <p className="mt-2 text-[12px] text-slate-500">
          {kind === "student"
            ? "Records a fee payment for a specific student — updates their enrollment balance."
            : "Records non-fee income like uniforms, fines, donations — does not affect any student's balance."}
        </p>
      </div>

      {kind === "student" ? (
        <StudentPaymentForm
          sessions={sessions}
          sessionId={sessionId}
          termId={termId}
          onSessionChange={handleSessionChange}
          onTermChange={handleTermChange}
          selectedStudent={selectedStudent}
          setSelectedStudent={setSelectedStudent}
          search={search}
          onSearchChange={handleSearchChange}
          debounced={debounced}
          results={results}
          setResults={setResults}
          searching={searching}
          amount={amount}
          setAmount={setAmount}
          reference={reference}
          setReference={setReference}
          note={note}
          setNote={setNote}
          submitting={submitting}
          onSubmit={submitStudent}
          selectedSession={selectedSession}
          amountNumber={amountNumber}
          newBalance={newBalance}
          isOverpaying={isOverpaying}
          // 👇 NEW
          outstandingAmount={outstandingAmount}
          setOutstandingAmount={setOutstandingAmount}
          savingOutstanding={savingOutstanding}
          onSaveOutstanding={onSaveOutstanding}
        />
      ) : (
        <OtherIncomeForm
          sessions={sessions}
          sessionId={sessionId}
          termId={termId}
          onSessionChange={handleSessionChange}
          onTermChange={handleTermChange}
          selectedSession={selectedSession}
          category={category}
          setCategory={setCategory}
          description={description}
          setDescription={setDescription}
          otherAmount={otherAmount}
          setOtherAmount={setOtherAmount}
          payerName={payerName}
          setPayerName={setPayerName}
          payerPhone={payerPhone}
          setPayerPhone={setPayerPhone}
          otherReference={otherReference}
          setOtherReference={setOtherReference}
          otherNote={otherNote}
          setOtherNote={setOtherNote}
          submitting={submittingOther}
          onSubmit={submitOther}
        />
      )}
    </div>
  );
}

// ==========================================================
// STUDENT PAYMENT FORM
// ==========================================================
interface StudentPaymentFormProps {
  sessions: AcademicSession[];
  sessionId: string;
  termId: string;
  onSessionChange: (e: ChangeEvent<HTMLSelectElement>) => void;
  onTermChange: (e: ChangeEvent<HTMLSelectElement>) => void;
  selectedStudent: StudentRow | null;
  setSelectedStudent: (s: StudentRow | null) => void;
  search: string;
  onSearchChange: (e: ChangeEvent<HTMLInputElement>) => void;
  debounced: string;
  results: StudentRow[];
  setResults: (r: StudentRow[]) => void;
  searching: boolean;
  amount: string;
  setAmount: (v: string) => void;
  reference: string;
  setReference: (v: string) => void;
  note: string;
  setNote: (v: string) => void;
  submitting: boolean;
  onSubmit: () => void;
  selectedSession: AcademicSession | undefined;
  amountNumber: number;
  newBalance: number;
  isOverpaying: boolean;
  // 👇 NEW
  outstandingAmount: string;
  setOutstandingAmount: (v: string) => void;
  savingOutstanding: boolean;
  onSaveOutstanding: () => void;
}

function StudentPaymentForm(p: StudentPaymentFormProps) {
  return (
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

          {p.selectedStudent ? (
            <div className="flex items-center justify-between p-4 rounded-xl border border-emerald-200 bg-emerald-50/40">
              <div className="flex items-center gap-3 min-w-0">
                <Avatar
                  name={p.selectedStudent.fullName}
                  src={p.selectedStudent.profilePictureUrl}
                  size="lg"
                />
                <div className="min-w-0">
                  <p className="text-[14px] font-semibold text-slate-900 truncate">
                    {p.selectedStudent.fullName}
                  </p>
                  <p className="text-[12px] text-slate-500">
                    {p.selectedStudent.admissionNumber} ·{" "}
                    {p.selectedStudent.className}
                  </p>
                  {p.selectedStudent.outstandingBalance > 0 && (
                    <p className="text-[12px] mt-1 text-rose-600 font-medium">
                      Outstanding:{" "}
                      {formatNaira(p.selectedStudent.outstandingBalance)}
                    </p>
                  )}
                </div>
              </div>
              <Button
                variant="ghost"
                onClick={() => {
                  p.setSelectedStudent(null);
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
                value={p.search}
                onChange={p.onSearchChange}
              />

              {p.searching && (
                <p className="text-[12px] text-slate-400 mt-3">Searching…</p>
              )}

              {!p.searching && p.results.length > 0 && (
                <div className="mt-3 border border-slate-100 rounded-xl overflow-hidden divide-y divide-slate-100">
                  {p.results.map((s) => (
                    <button
                      key={s.id}
                      type="button"
                      onClick={() => {
                        p.setSelectedStudent(s);
                        p.setResults([]);
                      }}
                      className="w-full flex items-center gap-3 p-3 hover:bg-slate-50 text-left transition-colors"
                    >
                      <Avatar
                        name={s.fullName}
                        src={s.profilePictureUrl}
                        size="sm"
                      />
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

              {!p.searching && p.debounced && p.results.length === 0 && (
                <div className="mt-3 py-8 text-center text-[13px] text-slate-400">
                  No students found for &quot;{p.debounced}&quot;
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
                  value={p.sessionId}
                  onChange={p.onSessionChange}
                  options={p.sessions.map((s) => ({
                    value: s.id,
                    label: s.name,
                  }))}
                />
              </FormField>
              <FormField label="Term" required>
                <Select
                  value={p.termId}
                  onChange={p.onTermChange}
                  options={
                    p.selectedSession?.terms.map((t) => ({
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
                value={p.amount}
                onChange={(e) => p.setAmount(e.target.value)}
              />
            </FormField>

            <FormField
              label="External reference"
              hint="Optional teller slip or receipt book number. Auto-generated if empty."
            >
              <Input
                placeholder="e.g. TELLER-0045"
                value={p.reference}
                onChange={(e) => p.setReference(e.target.value)}
              />
            </FormField>

            <FormField label="Note">
              <Textarea
                rows={3}
                placeholder="e.g. Father paid in person at bursary"
                value={p.note}
                onChange={(e) => p.setNote(e.target.value)}
              />
            </FormField>
          </div>
        </Card>

        {/* 👇 NEW Step 3 — set outstanding */}
        {p.selectedStudent && (
          <Card>
            <div className="flex items-center gap-3 mb-5">
              <div className="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 grid place-items-center text-[13px] font-semibold">
                <Wallet className="w-4 h-4" />
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-slate-900">
                  Set outstanding{" "}
                  <span className="text-[11px] font-normal text-slate-400">
                    (optional)
                  </span>
                </h2>
                <p className="text-[12px] text-slate-400">
                  Use this to set the student's true balance — including any
                  amount owed from before the system.
                </p>
              </div>
            </div>

            <div className="flex items-end gap-3">
              <div className="flex-1">
                <FormField
                  label="Outstanding amount (₦)"
                  hint="This overwrites the current balance. Payments after this reduce it."
                >
                  <Input
                    type="number"
                    inputMode="decimal"
                    placeholder="0"
                    value={p.outstandingAmount}
                    onChange={(e) => p.setOutstandingAmount(e.target.value)}
                  />
                </FormField>
              </div>
              <Button
                variant="secondary"
                onClick={p.onSaveOutstanding}
                loading={p.savingOutstanding}
                disabled={p.outstandingAmount.trim() === ""}
              >
                Save
              </Button>
            </div>

            <p className="mt-3 text-[11px] text-slate-400">
              Current outstanding:{" "}
              <span className="font-semibold text-slate-700">
                {formatNaira(p.selectedStudent.outstandingBalance)}
              </span>
            </p>
          </Card>
        )}
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
              value={p.selectedStudent?.fullName ?? "—"}
            />
            <SummaryRow
              label="Admission number"
              value={p.selectedStudent?.admissionNumber ?? "—"}
            />
            <SummaryRow
              label="Class"
              value={p.selectedStudent?.className ?? "—"}
            />
            <div className="pt-3 border-t border-slate-100" />
            <SummaryRow
              label="Current balance"
              value={
                p.selectedStudent
                  ? formatNaira(p.selectedStudent.outstandingBalance)
                  : "—"
              }
            />
            <SummaryRow
              label="Paying now"
              value={p.amountNumber > 0 ? formatNaira(p.amountNumber) : "—"}
              highlight
            />
            <SummaryRow
              label="New balance"
              value={p.selectedStudent ? formatNaira(p.newBalance) : "—"}
            />
          </div>

          {p.isOverpaying && (
            <p className="mt-4 text-[11px] text-amber-600 bg-amber-50 border border-amber-100 rounded-lg p-2.5">
              Amount exceeds outstanding balance — overpayment will be credited.
            </p>
          )}

          <Button
            fullWidth
            size="lg"
            icon={BadgeDollarSign}
            className="mt-6"
            loading={p.submitting}
            onClick={p.onSubmit}
            disabled={!p.selectedStudent || !p.amount}
          >
            Record payment
          </Button>

          <p className="mt-3 text-[11px] text-slate-400 text-center">
            This action is logged and cannot be undone.
          </p>
        </Card>
      </div>
    </div>
  );
}

// ==========================================================
// OTHER INCOME FORM  (unchanged)
// ==========================================================
interface OtherIncomeFormProps {
  sessions: AcademicSession[];
  sessionId: string;
  termId: string;
  onSessionChange: (e: ChangeEvent<HTMLSelectElement>) => void;
  onTermChange: (e: ChangeEvent<HTMLSelectElement>) => void;
  selectedSession: AcademicSession | undefined;
  category: string;
  setCategory: (v: string) => void;
  description: string;
  setDescription: (v: string) => void;
  otherAmount: string;
  setOtherAmount: (v: string) => void;
  payerName: string;
  setPayerName: (v: string) => void;
  payerPhone: string;
  setPayerPhone: (v: string) => void;
  otherReference: string;
  setOtherReference: (v: string) => void;
  otherNote: string;
  setOtherNote: (v: string) => void;
  submitting: boolean;
  onSubmit: () => void;
}

function OtherIncomeForm(p: OtherIncomeFormProps) {
  const amountNumber = parseFloat(p.otherAmount) || 0;

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div className="lg:col-span-2 space-y-6">
        <Card>
          <div className="flex items-center gap-3 mb-5">
            <div className="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 grid place-items-center">
              <Receipt className="w-4 h-4" />
            </div>
            <div>
              <h2 className="text-[15px] font-semibold text-slate-900">
                What kind of income?
              </h2>
              <p className="text-[12px] text-slate-400">
                Pick a category and describe what was sold or collected
              </p>
            </div>
          </div>

          <div className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Category" required>
                <Select
                  value={p.category}
                  onChange={(e) => p.setCategory(e.target.value)}
                  options={OTHER_CATEGORIES.map((c) => ({
                    value: c,
                    label: c,
                  }))}
                />
              </FormField>
              <FormField label="Amount (₦)" required hint="Amount received">
                <Input
                  type="number"
                  inputMode="decimal"
                  placeholder="5000"
                  value={p.otherAmount}
                  onChange={(e) => p.setOtherAmount(e.target.value)}
                />
              </FormField>
            </div>

            <FormField
              label="Description"
              required
              hint="Briefly say what this money was for"
            >
              <Input
                placeholder="e.g. 2 sports jerseys (medium)"
                value={p.description}
                onChange={(e) => p.setDescription(e.target.value)}
              />
            </FormField>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Payer name" hint="Optional">
                <Input
                  placeholder="e.g. Mr Okonkwo"
                  value={p.payerName}
                  onChange={(e) => p.setPayerName(e.target.value)}
                />
              </FormField>
              <FormField label="Payer phone" hint="Optional">
                <Input
                  placeholder="e.g. 0803…"
                  value={p.payerPhone}
                  onChange={(e) => p.setPayerPhone(e.target.value)}
                />
              </FormField>
            </div>
          </div>
        </Card>

        <Card>
          <div className="flex items-center gap-3 mb-5">
            <div className="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 grid place-items-center text-[13px] font-semibold">
              ℹ
            </div>
            <div>
              <h2 className="text-[15px] font-semibold text-slate-900">
                Accounting context
              </h2>
              <p className="text-[12px] text-slate-400">
                Session, term and reference — the accountant will use these
              </p>
            </div>
          </div>

          <div className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField label="Session" required>
                <Select
                  value={p.sessionId}
                  onChange={p.onSessionChange}
                  options={p.sessions.map((s) => ({
                    value: s.id,
                    label: s.name,
                  }))}
                />
              </FormField>
              <FormField label="Term" required>
                <Select
                  value={p.termId}
                  onChange={p.onTermChange}
                  options={
                    p.selectedSession?.terms.map((t) => ({
                      value: t.id,
                      label: t.name,
                    })) ?? []
                  }
                />
              </FormField>
            </div>

            <FormField
              label="External reference"
              hint="Optional receipt book number. Auto-generated if empty."
            >
              <Input
                placeholder="e.g. RECEIPT-0050"
                value={p.otherReference}
                onChange={(e) => p.setOtherReference(e.target.value)}
              />
            </FormField>

            <FormField label="Note">
              <Textarea
                rows={3}
                placeholder="e.g. Paid in person by the parent"
                value={p.otherNote}
                onChange={(e) => p.setOtherNote(e.target.value)}
              />
            </FormField>
          </div>
        </Card>
      </div>

      <div className="space-y-6">
        <Card className="sticky top-24">
          <h3 className="text-[15px] font-semibold text-slate-900 mb-4">
            Summary
          </h3>

          <div className="space-y-3">
            <SummaryRow label="Type" value="Other income" />
            <SummaryRow label="Category" value={p.category || "—"} />
            <SummaryRow label="Description" value={p.description || "—"} />
            <SummaryRow label="Payer" value={p.payerName || "—"} />
            <div className="pt-3 border-t border-slate-100" />
            <SummaryRow
              label="Amount"
              value={amountNumber > 0 ? formatNaira(amountNumber) : "—"}
              highlight
            />
          </div>

          <p className="mt-4 text-[11px] text-slate-500 bg-slate-50 border border-slate-100 rounded-lg p-2.5">
            This will NOT affect any student&apos;s fee balance.
          </p>

          <Button
            fullWidth
            size="lg"
            icon={Coins}
            className="mt-6"
            loading={p.submitting}
            onClick={p.onSubmit}
            disabled={!p.category || !p.description || !p.otherAmount}
          >
            Record income
          </Button>

          <p className="mt-3 text-[11px] text-slate-400 text-center">
            This action is logged and cannot be undone.
          </p>
        </Card>
      </div>
    </div>
  );
}

// ==========================================================
// Shared
// ==========================================================
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
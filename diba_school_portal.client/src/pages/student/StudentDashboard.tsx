import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  BookOpen, Wallet, TrendingUp, Trophy, Calendar, FileText,
  Receipt, AlertCircle, CreditCard, X, Loader2, ShieldCheck,
  CheckCircle2,
} from "lucide-react";
import { useAuth } from "../../hooks/useAuth";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, StatCard, Badge, Avatar, Skeleton, FormField, Input,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface Bill {
  studentProfileId: string;
  academicSessionId: string;
  termId: string;
  termName: string;
  totalBilledAmount?: number;
  totalPaidSoFar?: number;
  totalBalanceRemaining?: number;
  minimumAmountRequired?: number;
  feeItems: {
    feeId: string;
    feeName: string;
    amount: number;
    minimumAmountToPay: number;
    amountPaidTowardThis: number;
    isFullyPaid: boolean;
  }[];
  paymentHistory: {
    paymentId: string;
    referenceNumber: string;
    amountPaid: number;
    paymentDate: string | null;
  }[];
}

interface InitiateResponse {
  paymentId: string;
  referenceNumber: string;
  checkoutUrl: string;
  amount: number;
  currency: string;
}

export default function StudentDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { toast } = useToast();

  const greeting = (() => {
    const h = new Date().getHours();
    if (h < 12) return "Good morning";
    if (h < 17) return "Good afternoon";
    return "Good evening";
  })();

  // ---------- Bill ----------
  const [bill, setBill] = useState<Bill | null>(null);
  const [billLoading, setBillLoading] = useState(true);
  const [billError, setBillError] = useState<string | null>(null);

  // ---------- Subject count ----------
  const [subjectCount, setSubjectCount] = useState<number | null>(null);

  // ---------- Payment modal ----------
  const [payOpen, setPayOpen] = useState(false);
  const [amount, setAmount] = useState("");
  const [email, setEmail] = useState("");
  const [paying, setPaying] = useState(false);

  const loadBill = async () => {
    setBillLoading(true);
    setBillError(null);
    try {
      const { data } = await api.get<ApiResponse<Bill>>("/student/me/bill");
      if (!data.success || !data.data) {
        setBillError(data.message ?? "Could not load your bill.");
        return;
      }
      setBill(data.data);
    } catch (err: unknown) {
      const e2 = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setBillError(
        e2?.response?.data?.message ?? e2?.message ?? "Could not load your bill."
      );
    } finally {
      setBillLoading(false);
    }
  };

  const loadSubjectCount = async () => {
    try {
      const { data } = await api.get<ApiResponse<number>>(
        "/student/me/subjects/count"
      );
      if (data.success) setSubjectCount(data.data ?? 0);
    } catch {
      /* leave null → shows "-" */
    }
  };

  useEffect(() => {
    loadBill();
    loadSubjectCount();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const balance = bill?.totalBalanceRemaining ?? 0;
  const paid = bill?.totalPaidSoFar ?? 0;
  const billed = bill?.totalBilledAmount ?? 0;
  const minimum = bill?.minimumAmountRequired ?? 0;

  const openPay = () => {
    if (!bill) return;
    if (balance <= 0) {
      toast({ variant: "success", title: "You have nothing to pay." });
      return;
    }
    const defaultAmount = minimum > 0 ? Math.min(minimum, balance) : balance;
    setAmount(defaultAmount.toFixed(2));
    setEmail("");
    setPayOpen(true);
  };

  const submitPayment = async () => {
    if (!bill) return;

    const amt = parseFloat(amount);
    if (!amt || amt <= 0) {
      toast({ variant: "error", title: "Enter a valid amount." });
      return;
    }
    if (amt > balance + 0.01) {
      toast({ variant: "error", title: `Amount exceeds balance of ${formatNaira(balance)}.` });
      return;
    }
    if (minimum > 0 && amt < minimum) {
      toast({ variant: "error", title: `Minimum payment is ${formatNaira(minimum)}.` });
      return;
    }

    setPaying(true);
    try {
      const { data } = await api.post<ApiResponse<InitiateResponse>>(
        "/payment/flutterwave/initiate",
        {
          studentProfileId: bill.studentProfileId,
          academicSessionId: bill.academicSessionId,
          termId: bill.termId,
          amount: amt,
          customerEmail: email.trim() || undefined,
        }
      );

      if (!data.success || !data.data) {
        toast({ variant: "error", title: data.message ?? "Could not start payment." });
        setPaying(false);
        return;
      }

      sessionStorage.setItem("flw_pending_ref", data.data.referenceNumber);
      window.location.href = data.data.checkoutUrl;
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      const message =
        e2?.response?.data?.message ??
        e2?.message ??
        (status === 404
          ? "Payment endpoint not found."
          : status === 500
          ? "Server error — check the API terminal."
          : "Payment failed to start.");
      toast({ variant: "error", title: message });
      setPaying(false);
    }
  };

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow={user?.className ?? "Student"}
        title={`${greeting}, ${user?.firstName ?? "there"}`}
        description="Your fees, results, and class updates in one place."
        actions={
          <Button
            variant="secondary"
            icon={Calendar}
            onClick={() => navigate("/student/bill")}
          >
            Full bill breakdown
          </Button>
        }
      />

      {/* BILL SUMMARY */}
      {billLoading ? (
        <Card><Skeleton className="h-40" /></Card>
      ) : billError ? (
        <Card>
          <div className="flex items-start gap-3 py-4">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">Couldn't load your bill</p>
              <p className="text-[13px] text-red-800/80 mt-1">{billError}</p>
              <Button variant="secondary" className="mt-3" onClick={loadBill}>Retry</Button>
            </div>
          </div>
        </Card>
      ) : bill ? (
        <Card>
          <div className="flex flex-col lg:flex-row lg:items-center gap-6">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <Wallet className="w-4 h-4 text-surface-500" />
                <p className="text-[12px] font-medium uppercase tracking-wider text-surface-500">
                  Outstanding — {bill.termName}
                </p>
              </div>
              <p className={`text-3xl font-semibold tabular-nums ${balance > 0 ? "text-rose-600" : "text-emerald-600"}`}>
                {formatNaira(balance)}
              </p>
              <p className="text-[12px] text-surface-500 mt-2">
                {formatNaira(paid)} paid of {formatNaira(billed)} billed
              </p>
            </div>

            {balance > 0 && minimum > 0 && (
              <>
                <div className="hidden lg:block w-px h-16 bg-surface-100" />
                <div>
                  <p className="text-[12px] font-medium uppercase tracking-wider text-surface-500 mb-1">
                    Minimum to pay
                  </p>
                  <p className="text-xl font-semibold text-surface-900 tabular-nums">
                    {formatNaira(minimum)}
                  </p>
                  <p className="text-[11px] text-surface-400 mt-1">
                    Required to activate enrollment
                  </p>
                </div>
              </>
            )}

            {balance > 0 ? (
              <div className="lg:ml-auto">
                <Button size="lg" icon={CreditCard} onClick={openPay} className="w-full lg:w-auto">
                  Pay now
                </Button>
              </div>
            ) : (
              <div className="lg:ml-auto flex items-center gap-2 text-emerald-700">
                <CheckCircle2 className="w-5 h-5" />
                <span className="text-[13px] font-medium">Fully paid</span>
              </div>
            )}
          </div>
        </Card>
      ) : null}

      {/* STAT STRIP */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Outstanding fees"
          value={bill ? formatNaira(balance) : "-"}
          hint={bill ? bill.termName : "Current term"}
          icon={Wallet}
          tone={bill && balance > 0 ? "rose" : "emerald"}
        />
        <StatCard label="Latest average" value="-" hint="No results yet" icon={TrendingUp} tone="sky" />
        <StatCard label="Class position" value="-" hint="Most recent term" icon={Trophy} tone="amber" />
        <StatCard
          label="Subjects"
          value={subjectCount !== null ? String(subjectCount) : "-"}
          hint="Enrolled this term"
          icon={BookOpen}
          tone="violet"
        />
      </div>

      {/* QUICK ACCESS + PROFILE */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <h3 className="text-[15px] font-semibold text-surface-900 mb-4">Quick access</h3>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
            <button
              type="button"
              onClick={() => navigate("/student/results")}
              className="p-4 rounded-xl border border-surface-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-surface-50 text-surface-600">
                <FileText className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-surface-800">My results</p>
            </button>
            <button
              type="button"
              onClick={() => navigate("/student/bill")}
              className="p-4 rounded-xl border border-surface-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-surface-50 text-surface-600">
                <Receipt className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-surface-800">My payments</p>
            </button>
            <button
              type="button"
              onClick={() => navigate("/student/bill")}
              className="p-4 rounded-xl border border-surface-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-surface-50 text-surface-600">
                <Wallet className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-surface-800">My bill</p>
            </button>
            <button
              type="button"
              onClick={() => navigate("/student/subjects")}
              className="p-4 rounded-xl border border-surface-100 hover:border-emerald-200 hover:bg-emerald-50/30 transition-all text-left"
            >
              <div className="w-9 h-9 rounded-lg grid place-items-center mb-3 bg-surface-50 text-surface-600">
                <BookOpen className="w-4 h-4" />
              </div>
              <p className="text-[13px] font-medium text-surface-800">My subjects</p>
            </button>
          </div>
        </Card>

        <Card>
          <div className="text-center">
            <Avatar
              name={`${user?.firstName} ${user?.lastName}`}
              src={user?.profilePictureUrl}
              size="xl"
            />
            <p className="mt-4 text-[14px] font-semibold text-surface-900">
              {user?.firstName} {user?.lastName}
            </p>
            <p className="text-[12px] text-surface-400 font-mono mt-0.5">
              {user?.admissionNumber ?? "-"}
            </p>
            <div className="mt-2">
              <Badge variant="emerald">{user?.className ?? "Student"}</Badge>
            </div>
          </div>
        </Card>
      </div>

      {/* PAYMENT MODAL */}
      {payOpen && bill && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !paying && setPayOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
              <div className="flex items-start justify-between mb-4">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-emerald-50 grid place-items-center">
                    <CreditCard className="w-5 h-5 text-emerald-600" />
                  </div>
                  <div>
                    <h2 className="text-[16px] font-semibold text-surface-900">Pay fees online</h2>
                    <p className="text-[12px] text-surface-500">Secure checkout via Flutterwave</p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => !paying && setPayOpen(false)}
                  className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <div className="space-y-4 mt-4">
                <FormField
                  label="Amount (₦)"
                  required
                  hint={
                    minimum > 0
                      ? `Minimum: ${formatNaira(minimum)} · Balance: ${formatNaira(balance)}`
                      : `Balance: ${formatNaira(balance)}`
                  }
                >
                  <Input
                    type="number"
                    inputMode="decimal"
                    value={amount}
                    onChange={(e: ChangeEvent<HTMLInputElement>) => setAmount(e.target.value)}
                  />
                </FormField>

                <div className="grid grid-cols-2 gap-2">
                  {minimum > 0 && (
                    <button
                      type="button"
                      onClick={() => setAmount(minimum.toFixed(2))}
                      className="text-[11px] px-3 py-2 rounded-lg border border-surface-200 text-surface-700 hover:bg-surface-50"
                    >
                      Pay minimum
                    </button>
                  )}
                  <button
                    type="button"
                    onClick={() => setAmount(balance.toFixed(2))}
                    className="text-[11px] px-3 py-2 rounded-lg border border-surface-200 text-surface-700 hover:bg-surface-50"
                  >
                    Pay full balance
                  </button>
                </div>

                <FormField label="Email for receipt" hint="Optional — we'll use your profile email if empty">
                  <Input
                    type="email"
                    value={email}
                    onChange={(e: ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
                    placeholder="you@example.com"
                  />
                </FormField>

                <div className="flex items-start gap-2 text-[11px] text-surface-500 bg-surface-50 border border-surface-100 rounded-lg p-3">
                  <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
                  <p>You'll be redirected to Flutterwave to complete payment. Your card details never touch our servers.</p>
                </div>
              </div>

              <div className="mt-6 flex justify-end gap-3">
                <Button variant="secondary" onClick={() => setPayOpen(false)} disabled={paying}>
                  Cancel
                </Button>
                <Button onClick={submitPayment} disabled={paying}>
                  {paying ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin mr-2" />
                      Redirecting…
                    </>
                  ) : (
                    `Pay ${formatNaira(parseFloat(amount) || 0)}`
                  )}
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
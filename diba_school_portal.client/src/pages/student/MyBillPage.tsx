import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  Wallet, CreditCard, AlertCircle, X, Loader2, ShieldCheck, RefreshCw,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card, Skeleton, Button, EmptyState, FormField, Input,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

// ==========================================================
// Types — match StudentService.GetMyBillAsync response
// ==========================================================
interface Bill {
  studentProfileId: string;
  academicSessionId: string;
  termId: string;
  termName: string;
  totalBilledAmount: number;
  totalPaidSoFar: number;
  totalBalanceRemaining: number;
  minimumAmountRequired: number;
  feeItems: {
    feeId: string;
    feeName: string;
    description?: string;
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

export default function MyBillPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  // ---------- Bill state ----------
  const [data, setData] = useState<Bill | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // ---------- Payment modal ----------
  const [payOpen, setPayOpen] = useState(false);
  const [amount, setAmount] = useState("");
  const [email, setEmail] = useState("");
  const [paying, setPaying] = useState(false);

  // ==========================================================
  // Load bill
  // ==========================================================
  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const { data: res } = await api.get<ApiResponse<Bill>>("/student/me/bill");
      if (!res.success || !res.data) {
        setError(res.message ?? "Could not load your bill.");
        return;
      }
      setData(res.data);
    } catch (err: unknown) {
      const e2 = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setError(
        e2?.response?.data?.message ?? e2?.message ?? "Could not load your bill."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ==========================================================
  // Open payment modal
  // ==========================================================
  const openPay = () => {
    if (!data) return;
    if (data.totalBalanceRemaining <= 0) {
      toast({ variant: "success", title: "You have nothing to pay." });
      return;
    }
    const defaultAmount =
      data.minimumAmountRequired > 0
        ? Math.min(data.minimumAmountRequired, data.totalBalanceRemaining)
        : data.totalBalanceRemaining;
    setAmount(defaultAmount.toFixed(2));
    setEmail("");
    setPayOpen(true);
  };

  // ==========================================================
  // Submit payment — triggers Flutterwave
  // ==========================================================
  const submitPayment = async () => {
    if (!data) return;

    const amt = parseFloat(amount);
    if (!amt || amt <= 0) {
      toast({ variant: "error", title: "Enter a valid amount." });
      return;
    }
    if (amt > data.totalBalanceRemaining + 0.01) {
      toast({
        variant: "error",
        title: `Amount exceeds balance of ${formatNaira(
          data.totalBalanceRemaining
        )}.`,
      });
      return;
    }
    if (data.minimumAmountRequired > 0 && amt < data.minimumAmountRequired) {
      toast({
        variant: "error",
        title: `Minimum payment is ${formatNaira(data.minimumAmountRequired)}.`,
      });
      return;
    }

    setPaying(true);
    try {
      const { data: res } = await api.post<ApiResponse<InitiateResponse>>(
        "/payment/flutterwave/initiate",
        {
          studentProfileId: data.studentProfileId,
          academicSessionId: data.academicSessionId,
          termId: data.termId,
          amount: amt,
          customerEmail: email.trim() || undefined,
        }
      );

      if (!res.success || !res.data) {
        toast({ variant: "error", title: res.message ?? "Could not start payment." });
        setPaying(false);
        return;
      }

      sessionStorage.setItem("flw_pending_ref", res.data.referenceNumber);
      window.location.href = res.data.checkoutUrl;
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
          ? "Payment endpoint not found — backend not running or route missing."
          : status === 500
          ? "Server error — check the API terminal."
          : "Payment failed to start.");
      toast({ variant: "error", title: message });
      setPaying(false);
    }
  };

  // ==========================================================
  // Loading
  // ==========================================================
  if (loading) {
    return (
      <div className="space-y-6">
        <PageHeader eyebrow="Finance" title="My bill" />
        <Card><Skeleton className="h-64" /></Card>
      </div>
    );
  }

  // ==========================================================
  // Error
  // ==========================================================
  if (error) {
    return (
      <div className="space-y-6">
        <PageHeader eyebrow="Finance" title="My bill" />
        <Card>
          <div className="flex items-start gap-3 py-6">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">
                Couldn't load your bill
              </p>
              <p className="text-[13px] text-red-800/80 mt-1">{error}</p>
              <Button
                variant="secondary"
                icon={RefreshCw}
                className="mt-3"
                onClick={load}
              >
                Retry
              </Button>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  // ==========================================================
  // Empty
  // ==========================================================
  if (!data) {
    return (
      <div className="space-y-6">
        <PageHeader eyebrow="Finance" title="My bill" />
        <Card>
          <EmptyState
            icon={Wallet}
            title="No bill available"
            description="Your bill for the current term has not been published yet."
          />
        </Card>
      </div>
    );
  }

  // ==========================================================
  // Main render
  // ==========================================================
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={data.termName}
        title="My bill"
        description="Detailed breakdown of what you owe for the current term."
      />

      {/* ---------- Summary strip ---------- */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">
            Total billed
          </p>
          <p className="mt-1 text-xl font-semibold text-slate-900 tabular-nums">
            {formatNaira(data.totalBilledAmount)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">
            Paid so far
          </p>
          <p className="mt-1 text-xl font-semibold text-emerald-600 tabular-nums">
            {formatNaira(data.totalPaidSoFar)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[11px] font-medium uppercase tracking-wider text-slate-400">
            Balance
          </p>
          <p className="mt-1 text-xl font-semibold tabular-nums text-rose-600">
            {formatNaira(data.totalBalanceRemaining)}
          </p>
        </Card>
      </div>

      {/* ---------- Minimum required banner ---------- */}
      {data.totalBalanceRemaining > 0 && data.minimumAmountRequired > 0 && (
        <div className="rounded-xl border border-amber-200 bg-amber-50 p-4 flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-amber-600 mt-0.5 shrink-0" />
          <div className="flex-1">
            <p className="text-[13px] font-semibold text-amber-900">
              Minimum payment required: {formatNaira(data.minimumAmountRequired)}
            </p>
            <p className="text-[12px] text-amber-800/80 mt-0.5">
              Pay at least this amount to activate your enrollment for {data.termName}.
            </p>
          </div>
        </div>
      )}

      {/* ---------- Fee items ---------- */}
      <Card padding="none">
        <div className="p-6 pb-4">
          <h3 className="text-[15px] font-semibold text-slate-900">Fee items</h3>
        </div>
        <div className="divide-y divide-slate-100">
          {data.feeItems.map((f) => {
            const minRemaining =
              f.minimumAmountToPay > f.amountPaidTowardThis
                ? f.minimumAmountToPay - f.amountPaidTowardThis
                : 0;
            return (
              <div
                key={f.feeId}
                className="px-6 py-4 flex items-center justify-between gap-4"
              >
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl grid place-items-center bg-slate-100 text-slate-500">
                    <Wallet className="w-4 h-4" />
                  </div>
                  <div>
                    <p className="text-[13px] font-medium text-slate-800">
                      {f.feeName}
                    </p>
                    {minRemaining > 0 && (
                      <p className="text-[11px] text-amber-600 mt-0.5">
                        Min required: {formatNaira(minRemaining)}
                      </p>
                    )}
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-[13px] font-semibold text-slate-900 tabular-nums">
                    {formatNaira(f.amount)}
                  </p>
                  <p className="text-[11px] text-slate-400 mt-0.5">
                    Paid {formatNaira(f.amountPaidTowardThis)}
                  </p>
                </div>
              </div>
            );
          })}
          {data.feeItems.length === 0 && (
            <div className="px-6 py-10 text-center text-[13px] text-slate-400">
              No fees configured for your class yet.
            </div>
          )}
        </div>
      </Card>

      {/* ---------- Pay now callout ---------- */}
      {data.totalBalanceRemaining > 0 && (
        <div className="bg-gradient-to-br from-emerald-50 to-emerald-100/50 rounded-2xl border border-emerald-100 p-6 flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-white grid place-items-center shadow-sm shrink-0">
            <CreditCard className="w-5 h-5 text-emerald-600" />
          </div>
          <div className="flex-1">
            <p className="text-[14px] font-semibold text-slate-900">
              Pay your fees online
            </p>
            <p className="text-[12px] text-slate-600 mt-0.5">
              Card, bank transfer, or USSD — powered by Flutterwave.
            </p>
          </div>
          <Button onClick={openPay}>Pay now</Button>
        </div>
      )}

      {/* ---------- Payment history ---------- */}
      {data.paymentHistory.length > 0 && (
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-slate-900">
              Payment history
            </h3>
          </div>
          <div className="divide-y divide-slate-100">
            {data.paymentHistory.map((p) => (
              <div
                key={p.paymentId}
                className="px-6 py-3.5 flex items-center gap-3"
              >
                <div className="w-9 h-9 rounded-xl bg-emerald-50 grid place-items-center shrink-0">
                  <Wallet className="w-4 h-4 text-emerald-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-[12px] font-mono text-slate-600">
                    {p.referenceNumber}
                  </p>
                  <p className="text-[11px] text-slate-400 mt-0.5">
                    {p.paymentDate ? formatDate(p.paymentDate) : "-"}
                  </p>
                </div>
                <p className="text-[13px] font-semibold text-slate-900 tabular-nums">
                  {formatNaira(p.amountPaid)}
                </p>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* ===============================================
          PAYMENT MODAL
          =============================================== */}
      {payOpen && (
        <>
          <div
            className="fixed inset-0 bg-slate-950/50 z-40"
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
                    <h2 className="text-[16px] font-semibold text-slate-900">
                      Pay fees online
                    </h2>
                    <p className="text-[12px] text-slate-500">
                      Secure checkout via Flutterwave
                    </p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => !paying && setPayOpen(false)}
                  className="p-2 rounded-lg text-slate-400 hover:bg-slate-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <div className="space-y-4 mt-4">
                <FormField
                  label="Amount (₦)"
                  required
                  hint={
                    data.minimumAmountRequired > 0
                      ? `Minimum: ${formatNaira(
                          data.minimumAmountRequired
                        )} · Balance: ${formatNaira(
                          data.totalBalanceRemaining
                        )}`
                      : `Balance: ${formatNaira(data.totalBalanceRemaining)}`
                  }
                >
                  <Input
                    type="number"
                    inputMode="decimal"
                    value={amount}
                    onChange={(e: ChangeEvent<HTMLInputElement>) =>
                      setAmount(e.target.value)
                    }
                  />
                </FormField>

                <div className="grid grid-cols-2 gap-2">
                  {data.minimumAmountRequired > 0 && (
                    <button
                      type="button"
                      onClick={() =>
                        setAmount(data.minimumAmountRequired.toFixed(2))
                      }
                      className="text-[11px] px-3 py-2 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-50"
                    >
                      Pay minimum
                    </button>
                  )}
                  <button
                    type="button"
                    onClick={() =>
                      setAmount(data.totalBalanceRemaining.toFixed(2))
                    }
                    className="text-[11px] px-3 py-2 rounded-lg border border-slate-200 text-slate-700 hover:bg-slate-50"
                  >
                    Pay full balance
                  </button>
                </div>

                <FormField
                  label="Email for receipt"
                  hint="Optional — we'll use your profile email if empty"
                >
                  <Input
                    type="email"
                    value={email}
                    onChange={(e: ChangeEvent<HTMLInputElement>) =>
                      setEmail(e.target.value)
                    }
                    placeholder="you@example.com"
                  />
                </FormField>

                <div className="flex items-start gap-2 text-[11px] text-slate-500 bg-slate-50 border border-slate-100 rounded-lg p-3">
                  <ShieldCheck className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />
                  <p>
                    You'll be redirected to Flutterwave to complete payment.
                    Your card details never touch our servers.
                  </p>
                </div>
              </div>

              <div className="mt-6 flex justify-end gap-3">
                <Button
                  variant="secondary"
                  onClick={() => setPayOpen(false)}
                  disabled={paying}
                >
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
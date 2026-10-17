import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { CheckCircle2, XCircle, Loader2, AlertCircle } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, Button } from "../../components/ui";
import api from "../../lib/api";
import { formatNaira } from "../../lib/format";
import type { ApiResponse } from "../../types/common";

type Status = "verifying" | "success" | "failed";

export default function PaymentCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();

  const [status, setStatus] = useState<Status>("verifying");
  const [message, setMessage] = useState("Verifying your payment…");
  const [amount, setAmount] = useState<number | null>(null);

  useEffect(() => {
    const txId = params.get("transaction_id");
    const txRef = params.get("tx_ref");
    const flwStatus = params.get("status"); // "successful" | "failed" | "cancelled"

    // If Flutterwave already told us it failed, don't bother verifying
    if (flwStatus && flwStatus !== "successful") {
      setStatus("failed");
      setMessage(
        flwStatus === "cancelled"
          ? "You cancelled the payment."
          : `Payment ${flwStatus}. You can try again from your bill.`
      );
      return;
    }

    if (!txId || !txRef) {
      setStatus("failed");
      setMessage("Missing transaction information from Flutterwave.");
      return;
    }

    (async () => {
      try {
        const { data } = await api.post<ApiResponse<boolean>>(
          `/payment/flutterwave/verify?transactionId=${encodeURIComponent(
            txId
          )}&referenceNumber=${encodeURIComponent(txRef)}`
        );

        if (!data.success) {
          setStatus("failed");
          setMessage(data.message ?? "Could not verify payment.");
          return;
        }

        try {
          const { data: bill } = await api.get<ApiResponse<{ totalPaidSoFar?: number }>>(
            "/student/me/bill"
          );
          if (bill.success) setAmount(bill.data.totalPaidSoFar ?? 0);
        } catch {
          /* non-fatal */
        }

        setStatus("success");
        setMessage("Payment confirmed. Your bill has been updated.");
      } catch (err: unknown) {
        const e2 = err as {
          response?: { data?: { message?: string } };
          message?: string;
        };
        setStatus("failed");
        setMessage(
          e2?.response?.data?.message ?? e2?.message ?? "Verification failed."
        );
      }
    })();
  }, [params]);

  return (
    <div className="max-w-md mx-auto py-12">
      <PageHeader eyebrow="Payment" title="Checkout result" />

      <Card>
        <div className="text-center py-6">
          {status === "verifying" && (
            <>
              <div className="w-14 h-14 rounded-full bg-sky-50 grid place-items-center mx-auto mb-4">
                <Loader2 className="w-7 h-7 text-sky-600 animate-spin" />
              </div>
              <p className="text-[14px] font-semibold text-slate-900">{message}</p>
              <p className="text-[12px] text-slate-500 mt-1">
                This usually takes 2–5 seconds.
              </p>
            </>
          )}

          {status === "success" && (
            <>
              <div className="w-14 h-14 rounded-full bg-emerald-50 grid place-items-center mx-auto mb-4">
                <CheckCircle2 className="w-7 h-7 text-emerald-600" />
              </div>
              <p className="text-[16px] font-semibold text-slate-900">
                Payment successful
              </p>
              <p className="text-[13px] text-slate-500 mt-1">{message}</p>
              {amount !== null && (
                <p className="text-[12px] text-slate-400 mt-2">
                  Total paid to date: {formatNaira(amount)}
                </p>
              )}
            </>
          )}

          {status === "failed" && (
            <>
              <div className="w-14 h-14 rounded-full bg-rose-50 grid place-items-center mx-auto mb-4">
                <XCircle className="w-7 h-7 text-rose-600" />
              </div>
              <p className="text-[16px] font-semibold text-slate-900">
                Payment not completed
              </p>
              <p className="text-[13px] text-slate-500 mt-1">{message}</p>
            </>
          )}

          <div className="mt-8 flex justify-center gap-3">
            <Button variant="secondary" onClick={() => navigate("/student")}>
              Back to dashboard
            </Button>
            <Button onClick={() => navigate("/student/bill")}>
              Go to bill
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
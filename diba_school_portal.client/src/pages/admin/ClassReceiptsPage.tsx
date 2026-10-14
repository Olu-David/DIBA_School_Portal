import { useEffect, useMemo, useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft,
  Download,
  Receipt,
  Wallet,
  Users,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card,
  Button,
  Badge,
  Skeleton,
  EmptyState,
  Avatar,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { downloadPdf } from "../../lib/downloadPdf";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface FeeItem {
  name: string;
  amount: number;
}

interface StudentReceipt {
  referenceNumber: string;
  studentName: string;
  className: string;
  sessionName: string;
  termName: string;
  paymentDate: string;
  profilePictureUrl: string | null;
  totalAmountPaid: number;
  feeItems: FeeItem[];
}

interface ClassReceipts {
  className: string;
  termName: string;
  academicSessionName: string;
  receipts: StudentReceipt[];
}

export default function ClassReceiptsPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { toast } = useToast();

  const classId = params.get("classId") ?? "";
  const termId = params.get("termId") ?? "";
  const sessionId = params.get("sessionId") ?? "";

  const [data, setData] = useState<ClassReceipts | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    if (!classId || !termId || !sessionId) {
      setLoading(false);
      return;
    }
    (async () => {
      try {
        const { data: res } = await api.get<ApiResponse<ClassReceipts>>(
          `/payment/class-receipts?classId=${classId}&termId=${termId}&academicSessionId=${sessionId}`
        );
        if (res.success) setData(res.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [classId, termId, sessionId]);

  const totals = useMemo(() => {
    if (!data) return { count: 0, total: 0 };
    return {
      count: data.receipts.length,
      total: data.receipts.reduce((s, r) => s + r.totalAmountPaid, 0),
    };
  }, [data]);

  const downloadPdfFile = async () => {
    if (!data || data.receipts.length === 0) {
      toast({ variant: "warning", title: "No receipts to print." });
      return;
    }
    setDownloading(true);
    try {
      await downloadPdf(
        "/pdfreport/class-receipts",
        data,
        `Receipts_${data.className}_${data.termName}.pdf`
      );
    } catch (err: any) {
      toast({
        variant: "error",
        title: err?.response?.data?.message ?? "Could not generate PDF",
      });
    } finally {
      setDownloading(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <Card>
          <Skeleton className="h-96" />
        </Card>
      </div>
    );
  }

  if (!data || data.receipts.length === 0) {
    return (
      <Card>
        <EmptyState
          icon={Receipt}
          title="No receipts to print"
          description="No successful payments recorded for this class in this term."
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
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        Back
      </button>

      <PageHeader
        eyebrow="Finance"
        title={`Receipts — ${data.className}`}
        description={`${data.termName} · ${data.academicSessionName}`}
        actions={
          <Button icon={Download} loading={downloading} onClick={downloadPdfFile}>
            Download all ({totals.count})
          </Button>
        }
      />

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Receipts
          </p>
          <p className="text-2xl font-semibold text-surface-900 tabular-nums mt-1">
            {totals.count}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Total collected
          </p>
          <p className="text-2xl font-semibold text-emerald-600 tabular-nums mt-1">
            {formatNaira(totals.total)}
          </p>
        </Card>
        <Card padding="sm">
          <p className="text-[10px] uppercase tracking-wider text-surface-400 font-medium">
            Term
          </p>
          <p className="text-2xl font-semibold text-surface-900 mt-1">
            {data.termName}
          </p>
        </Card>
      </div>

      <Card padding="none">
        <div className="divide-y divide-surface-100">
          {data.receipts.map((r) => (
            <div
              key={r.referenceNumber}
              className="px-6 py-4 flex items-center gap-4"
            >
              <Avatar name={r.studentName} src={r.profilePictureUrl} size="md" />
              <div className="flex-1 min-w-0">
                <p className="text-[13px] font-semibold text-surface-900">
                  {r.studentName}
                </p>
                <p className="text-[11px] text-surface-500 mt-0.5 font-mono">
                  {r.referenceNumber}
                </p>
              </div>
              <div className="text-right shrink-0">
                <p className="text-[13px] font-semibold text-surface-900 tabular-nums">
                  {formatNaira(r.totalAmountPaid)}
                </p>
                <p className="text-[11px] text-surface-400">
                  {formatDate(r.paymentDate)}
                </p>
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
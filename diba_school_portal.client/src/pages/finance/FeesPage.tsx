import { useEffect, useState } from "react";
import { BookOpen, Plus, Hash, Trash2 } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, FormField, Input, Select, Badge, Skeleton,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface ClassPrice { schoolClassId: string; amount: number; minimumAmountToPay: number; }
interface Fee {
  id: string;
  feeName: string;
  description: string;
  section: string;
  stream: string;
}

export default function FeesPage() {
  const { toast } = useToast();
  const [sessions, setSessions] = useState<any[]>([]);
  const [classes, setClasses] = useState<any[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [loading, setLoading] = useState(false);

  // Form
  const [feeName, setFeeName] = useState("");
  const [description, setDescription] = useState("");
  const [prices, setPrices] = useState<ClassPrice[]>([]);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<any>>("/schoolfee/metadata");
      if (data.success) {
        setSessions(data.data.sessions);
        setClasses(data.data.schoolClasses);
        const current = data.data.sessions.find((s: any) => s.isCurrentSession);
        if (current) {
          setSessionId(current.id);
          const t = current.terms.find((x: any) => x.isCurrentTerm) ?? current.terms[0];
          if (t) setTermId(t.id);
        }
      }
    })();
  }, []);

  // Prefill prices when class list changes
  useEffect(() => {
    setPrices(classes.map((c) => ({
      schoolClassId: c.id,
      amount: 0,
      minimumAmountToPay: 0,
    })));
  }, [classes]);

  const session = sessions.find((s) => s.id === sessionId);

  const updatePrice = (idx: number, key: keyof ClassPrice, value: number) => {
    setPrices((prev) => prev.map((p, i) => (i === idx ? { ...p, [key]: value } : p)));
  };

  const submit = async () => {
    const payload = {
      feeName,
      description,
      academicSessionId: sessionId,
      termId,
      schoolClassId: classes[0]?.id, // legacy field
      section: 1,                    // "Primary" — server ignores on non-required paths
      stream: 0,
      classPrices: prices.filter((p) => p.amount > 0),
    };

    if (!feeName || !termId || payload.classPrices.length === 0) {
      toast({ variant: "error", title: "Fee name, term, and at least one class price required" });
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<boolean>>("/schoolfee/create-fee", payload);
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }
      toast({ variant: "success", title: "Fee created" });
      setFeeName("");
      setDescription("");
      setPrices((p) => p.map((x) => ({ ...x, amount: 0, minimumAmountToPay: 0 })));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Fee structures"
        description="Define per-class amounts for each fee in a term."
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left — form */}
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <h3 className="text-[15px] font-semibold text-slate-900 mb-5">
              New fee
            </h3>

            <div className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField label="Session" required>
                  <Select
                    value={sessionId}
                    onChange={(e) => {
                      setSessionId(e.target.value);
                      const s = sessions.find((x) => x.id === e.target.value);
                      if (s?.terms[0]) setTermId(s.terms[0].id);
                    }}
                    options={sessions.map((s) => ({ value: s.id, label: s.name }))}
                  />
                </FormField>
                <FormField label="Term" required>
                  <Select
                    value={termId}
                    onChange={(e) => setTermId(e.target.value)}
                    options={session?.terms.map((t: any) => ({ value: t.id, label: t.name })) ?? []}
                  />
                </FormField>
              </div>

              <FormField label="Fee name" required>
                <Input
                  placeholder="e.g. Tuition Fee"
                  value={feeName}
                  onChange={(e) => setFeeName(e.target.value)}
                />
              </FormField>

              <FormField label="Description">
                <Input
                  placeholder="Optional"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </FormField>
            </div>
          </Card>

          {/* Class prices */}
          <Card padding="none">
            <div className="p-6 pb-4">
              <h3 className="text-[15px] font-semibold text-slate-900">
                Class prices
              </h3>
              <p className="text-[12px] text-slate-400 mt-0.5">
                Leave a class at ₦0 to skip it.
              </p>
            </div>

            <div className="divide-y divide-slate-100">
              {prices.map((p, idx) => {
                const cls = classes.find((c) => c.id === p.schoolClassId);
                return (
                  <div key={p.schoolClassId} className="px-6 py-3 grid grid-cols-1 sm:grid-cols-[1fr_160px_160px] gap-3 items-center">
                    <p className="text-[13px] font-medium text-slate-800">
                      {cls?.name ?? "Unknown"}
                    </p>
                    <Input
                      type="number"
                      placeholder="Amount"
                      value={p.amount || ""}
                      onChange={(e) => updatePrice(idx, "amount", Number(e.target.value))}
                    />
                    <Input
                      type="number"
                      placeholder="Min to pay"
                      value={p.minimumAmountToPay || ""}
                      onChange={(e) => updatePrice(idx, "minimumAmountToPay", Number(e.target.value))}
                    />
                  </div>
                );
              })}
            </div>
          </Card>
        </div>

        {/* Right — summary */}
        <div>
          <Card className="sticky top-24">
            <h3 className="text-[15px] font-semibold text-slate-900 mb-4">Summary</h3>
            <div className="space-y-3">
              <Row label="Fee name" value={feeName || "—"} />
              <Row label="Classes priced" value={String(prices.filter((p) => p.amount > 0).length)} />
              <div className="pt-3 border-t border-slate-100" />
              <Row label="Total catalogue" value={formatNaira(prices.reduce((s, p) => s + p.amount, 0))} highlight />
            </div>

            <Button
              fullWidth
              size="lg"
              className="mt-6"
              icon={Plus}
              loading={submitting}
              onClick={submit}
              disabled={!feeName || !termId}
            >
              Create fee
            </Button>
            <p className="mt-3 text-[11px] text-slate-400 text-center">
              Creates one Fee and one price row per class.
            </p>
          </Card>
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, highlight = false }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <span className="text-[12px] text-slate-500">{label}</span>
      <span className={highlight ? "text-[14px] font-semibold text-emerald-600" : "text-[13px] font-medium text-slate-800"}>
        {value}
      </span>
    </div>
  );
}
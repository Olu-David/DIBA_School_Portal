import { useCallback, useEffect, useMemo, useState } from "react";
import { Plus, Trash2, RotateCcw } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, FormField, Input, Select,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface SchoolClass {
  id: string;
  name: string;
  section: string;   // enum serialized as string ("Nursery", "Primary", ...)
}

interface ClassPrice {
  schoolClassId: string;
  amount: number;
  minimumAmountToPay: number;
}

interface FeeClassPriceItem {
  schoolClassId: string;
  className: string;
  amount: number;
  minimumAmountToPay: number;
}

interface FeeItem {
  id: string;
  feeName: string;
  description?: string;
  isDeleted: boolean;
  classPrices: FeeClassPriceItem[];
}

// ---- Order sections: PreNursery → Nursery → Primary → JSS → SS ----
const SECTION_ORDER: Record<string, number> = {
  PreNursery: 0,
  Nursery: 1,
  Primary: 2,
  JuniorSecondary: 3,
  SeniorSecondary: 4,
};

// Rank: section * 1000 + numeric suffix
function classRank(c: SchoolClass): number {
  const sec = SECTION_ORDER[c.section] ?? 99;
  const match = c.name.match(/(\d+)/);
  const num = match ? parseInt(match[1], 10) : 0;
  return sec * 1000 + num;
}

const errorMessage = (err: any, fallback = "Request failed") =>
  err?.response?.data?.message ?? err?.response?.data?.title ?? fallback;

export default function FeesPage() {
  const { toast } = useToast();

  const [sessions, setSessions] = useState<any[]>([]);
  const [rawClasses, setRawClasses] = useState<SchoolClass[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");

  const [feeName, setFeeName] = useState("");
  const [description, setDescription] = useState("");
  const [prices, setPrices] = useState<ClassPrice[]>([]);
  const [submitting, setSubmitting] = useState(false);

  const [fees, setFees] = useState<FeeItem[]>([]);
  const [loadingFees, setLoadingFees] = useState(false);
  const [showDeleted, setShowDeleted] = useState(false);
  const [busyFeeId, setBusyFeeId] = useState<string | null>(null);

  // ---- Sort classes: Nursery 1 → Nursery 2 → ... → SS 3 ----
  const classes = useMemo(
    () => [...rawClasses].sort((a, b) => classRank(a) - classRank(b)),
    [rawClasses]
  );

  // ---- Load metadata ----
  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<any>>("/schoolfee/metadata");
        if (data.success) {
          setSessions(data.data.sessions);
          setRawClasses(data.data.schoolClasses);

          const current = data.data.sessions.find((s: any) => s.isCurrentSession);
          if (current) {
            setSessionId(current.id);
            const t = current.terms.find((x: any) => x.isCurrentTerm) ?? current.terms[0];
            if (t) setTermId(t.id);
          }
        }
      } catch (err) {
        toast({ variant: "error", title: errorMessage(err, "Could not load sessions and classes") });
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ---- Prime price rows once classes are sorted ----
  useEffect(() => {
    setPrices(
      classes.map((c) => ({
        schoolClassId: c.id,
        amount: 0,
        minimumAmountToPay: 0,
      }))
    );
  }, [classes]);

  // ---- List existing fees ----
  const loadFees = useCallback(async () => {
    if (!termId) return;
    setLoadingFees(true);
    try {
      const { data } = await api.get<ApiResponse<FeeItem[]>>("/schoolfee/list", {
        params: { sessionId, termId, includeDeleted: showDeleted },
      });
      if (data.success) setFees(data.data);
      else toast({ variant: "error", title: data.message ?? "Could not load fees" });
    } catch (err) {
      toast({ variant: "error", title: errorMessage(err, "Could not load fees") });
    } finally {
      setLoadingFees(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sessionId, termId, showDeleted]);

  useEffect(() => { loadFees(); }, [loadFees]);

  const session = sessions.find((s) => s.id === sessionId);

  const updatePrice = (idx: number, key: keyof ClassPrice, value: number) => {
    setPrices((prev) => prev.map((p, i) => (i === idx ? { ...p, [key]: value } : p)));
  };

  // ---- Create ----
  const submit = async () => {
    const pricedClasses = prices.filter((p) => p.amount > 0);

    const payload = {
      feeName: feeName.trim(),
      description: description.trim(),
      academicSessionId: sessionId,
      termId,
      classPrices: pricedClasses,
    };

    if (!feeName.trim() || !termId || pricedClasses.length === 0) {
      toast({
        variant: "error",
        title: "Fee name, term, and at least one class price required",
      });
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
      await loadFees();
    } catch (err) {
      toast({ variant: "error", title: errorMessage(err) });
      console.error((err as any)?.response?.data ?? err);
    } finally {
      setSubmitting(false);
    }
  };

  // ---- Soft delete / restore ----
  const softDelete = async (fee: FeeItem) => {
    if (!window.confirm(`Delete "${fee.feeName}"? You can restore it later.`)) return;
    setBusyFeeId(fee.id);
    try {
      const { data } = await api.delete<ApiResponse<string>>(`/schoolfee/soft-delete/${fee.id}`);
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }
      toast({ variant: "success", title: "Fee deleted" });
      await loadFees();
    } catch (err) {
      toast({ variant: "error", title: errorMessage(err) });
    } finally {
      setBusyFeeId(null);
    }
  };

  const restore = async (fee: FeeItem) => {
    setBusyFeeId(fee.id);
    try {
      const { data } = await api.post<ApiResponse<string>>(`/schoolfee/restore/${fee.id}`);
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }
      toast({ variant: "success", title: "Fee restored" });
      await loadFees();
    } catch (err) {
      toast({ variant: "error", title: errorMessage(err) });
    } finally {
      setBusyFeeId(null);
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
        <div className="lg:col-span-2 space-y-6">
          {/* ---------- New fee ---------- */}
          <Card>
            <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
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

          {/* ---------- Class prices (sorted) ---------- */}
          <Card padding="none">
            <div className="p-6 pb-4">
              <h3 className="text-[15px] font-semibold text-surface-900">
                Class prices
              </h3>
              <p className="text-[12px] text-surface-400 mt-0.5">
                Leave a class at ₦0 to skip it.
              </p>
            </div>

            <div className="divide-y divide-surface-100">
              {classes.map((c, idx) => {
                const row = prices.find((p) => p.schoolClassId === c.id);
                const priceIdx = prices.findIndex((p) => p.schoolClassId === c.id);
                if (!row || priceIdx === -1) return null;

                return (
                  <div
                    key={c.id}
                    className="px-6 py-3 grid grid-cols-1 sm:grid-cols-[1fr_160px_160px] gap-3 items-center"
                  >
                    <p className="text-[13px] font-medium text-surface-800">
                      {c.name}
                    </p>
                    <Input
                      type="number"
                      placeholder="Amount"
                      value={row.amount || ""}
                      onChange={(e) => updatePrice(priceIdx, "amount", Number(e.target.value))}
                    />
                    <Input
                      type="number"
                      placeholder="Min to pay"
                      value={row.minimumAmountToPay || ""}
                      onChange={(e) => updatePrice(priceIdx, "minimumAmountToPay", Number(e.target.value))}
                    />
                  </div>
                );
              })}
            </div>
          </Card>

          {/* ---------- Existing fees ---------- */}
          <Card padding="none">
            <div className="p-6 pb-4 flex items-center justify-between gap-3">
              <h3 className="text-[15px] font-semibold text-surface-900">
                Existing fees
              </h3>
              <label className="flex items-center gap-2 text-[12px] text-surface-500 cursor-pointer">
                <input
                  type="checkbox"
                  checked={showDeleted}
                  onChange={(e) => setShowDeleted(e.target.checked)}
                />
                Show deleted
              </label>
            </div>

            <div className="divide-y divide-surface-100">
              {loadingFees && (
                <p className="px-6 py-4 text-[13px] text-surface-400">Loading fees...</p>
              )}

              {!loadingFees && fees.length === 0 && (
                <p className="px-6 py-4 text-[13px] text-surface-400">
                  No fees for this term yet. Create one above.
                </p>
              )}

              {!loadingFees && fees.map((f) => (
                <div
                  key={f.id}
                  className={`px-6 py-3 flex items-center justify-between gap-4 ${f.isDeleted ? "opacity-60" : ""}`}
                >
                  <div className="min-w-0">
                    <p className="text-[13px] font-medium text-surface-800 truncate">
                      {f.feeName}
                      {f.isDeleted && (
                        <span className="ml-2 text-[11px] font-normal text-surface-400">Deleted</span>
                      )}
                    </p>
                    <p className="text-[12px] text-surface-400">
                      {f.classPrices?.length ?? 0} class(es) ·{" "}
                      {formatNaira((f.classPrices ?? []).reduce((s, p) => s + p.amount, 0))}
                    </p>
                  </div>

                  {f.isDeleted ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      icon={RotateCcw}
                      loading={busyFeeId === f.id}
                      onClick={() => restore(f)}
                    >
                      Restore
                    </Button>
                  ) : (
                    <Button
                      size="sm"
                      variant="secondary"
                      icon={Trash2}
                      loading={busyFeeId === f.id}
                      onClick={() => softDelete(f)}
                    >
                      Delete
                    </Button>
                  )}
                </div>
              ))}
            </div>
          </Card>
        </div>

        {/* ---------- Summary ---------- */}
        <div>
          <Card className="sticky top-24">
            <h3 className="text-[15px] font-semibold text-surface-900 mb-4">Summary</h3>
            <div className="space-y-3">
              <Row label="Fee name" value={feeName || "-"} />
              <Row label="Classes priced" value={String(prices.filter((p) => p.amount > 0).length)} />
              <div className="pt-3 border-t border-surface-100" />
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
            <p className="mt-3 text-[11px] text-surface-400 text-center">
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
      <span className="text-[12px] text-surface-500">{label}</span>
      <span className={highlight ? "text-[14px] font-semibold text-brand-600" : "text-[13px] font-medium text-surface-800"}>
        {value}
      </span>
    </div>
  );
}
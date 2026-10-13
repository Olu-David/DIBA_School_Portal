import { useEffect, useState } from "react";
import { AlertCircle, MessageCircle, Download, Phone } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Skeleton, Select, Avatar, Badge,
} from "../../components/ui";
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

interface Session { id: string; name: string; isCurrentSession: boolean; terms: { id: string; name: string; isCurrentTerm: boolean }[]; }

export default function ArrearsPage() {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [termId, setTermId] = useState("");
  const [rows, setRows] = useState<ArrearsRow[]>([]);
  const [loading, setLoading] = useState(true);

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

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Finance"
        title="Arrears"
        description="Students with unpaid or partially paid fees for the selected term."
        actions={
          <>
            <Button variant="secondary" icon={Download}>Export</Button>
            <Button variant="secondary" icon={MessageCircle}>Send reminders</Button>
          </>
        }
      />

      <Card padding="sm">
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="flex-1">
            <Select
              value={sessionId}
              onChange={(e) => {
                setSessionId(e.target.value);
                const s = sessions.find((x) => x.id === e.target.value);
                if (s?.terms[0]) setTermId(s.terms.find(t => t.isCurrentTerm)?.id ?? s.terms[0].id);
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
            </p>
          </div>
        </div>
      )}

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
                      <td className="px-6 py-4">
                        <Badge variant="sky">{r.className}</Badge>
                      </td>
                      <td className="px-6 py-4">
                        {r.guardianPhone ? (
                          <a href={`tel:${r.guardianPhone}`} className="inline-flex items-center gap-1.5 text-[12px] text-surface-600 hover:text-brand-700">
                            <Phone className="w-3 h-3" />
                            {r.guardianPhone}
                          </a>
                        ) : <span className="text-surface-400">-</span>}
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
                      <p className="text-[11px] text-surface-400 font-mono">{r.admissionNumber} - {r.className}</p>
                    </div>
                    <p className="text-[13px] font-semibold text-flame-600 tabular-nums">
                      {formatNaira(r.outstanding)}
                    </p>
                  </div>
                  {r.guardianPhone && (
                    <a href={`tel:${r.guardianPhone}`} className="mt-3 inline-flex items-center gap-1.5 text-[12px] text-brand-600">
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
    </div>
  );
}
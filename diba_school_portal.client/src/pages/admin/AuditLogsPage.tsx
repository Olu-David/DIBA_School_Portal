import { useEffect, useState, type ChangeEvent } from "react";
import { ScrollText, Activity, User, Globe } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Spinner, SearchInput } from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { formatDateTime } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

interface AuditLog {
  id: string;
  userId: string | null;
  action: string;
  details: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export default function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const { data } = await api.get<ApiResponse<Paginated<AuditLog>>>(
          "/auditlog/logs?pageNumber=1&pageSize=200"
        );

        if (!data.success) {
          setError(data.message ?? "Could not load audit logs.");
          setLogs([]);
          return;
        }

        const items = data.data?.items;
        setLogs(Array.isArray(items) ? items : []);
      } catch (err: unknown) {
        const e2 = err as {
          response?: { status?: number; data?: { message?: string } };
          message?: string;
        };
        console.error("[AuditLogsPage] Error:", err);
        setError(
          e2?.response?.data?.message ??
            e2?.message ??
            "Could not load audit logs."
        );
        setLogs([]);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, []);

  // Never let a non-array crash the page
  const safeLogs = Array.isArray(logs) ? logs : [];

  const filtered = debounced
    ? safeLogs.filter(
        (l) =>
          l.action.toLowerCase().includes(debounced.toLowerCase()) ||
          (l.details ?? "").toLowerCase().includes(debounced.toLowerCase()) ||
          (l.userId ?? "").toLowerCase().includes(debounced.toLowerCase())
      )
    : safeLogs;

  const onSearchChange = (e: ChangeEvent<HTMLInputElement>) =>
    setSearch(e.target.value);

  return (
    <div>
      <PageHeader
        eyebrow="System"
        title="Audit logs"
        description="Every significant action recorded across the portal."
      />

      <div className="mb-6 max-w-sm">
        <SearchInput
          placeholder="Filter by action or user…"
          value={search}
          onChange={onSearchChange}
        />
      </div>

      <Card padding="none">
        {loading ? (
          <div className="py-16 flex justify-center">
            <Spinner size="lg" label="Loading logs…" />
          </div>
        ) : error ? (
          <div className="py-16 text-center">
            <p className="text-[13px] font-medium text-red-700">{error}</p>
            <p className="text-[12px] text-slate-400 mt-1">
              Open the browser console for details.
            </p>
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState
            icon={ScrollText}
            title={debounced ? "No matching logs" : "No logs yet"}
            description={
              debounced
                ? `Nothing matches "${debounced}".`
                : "Activity will appear here as it happens."
            }
          />
        ) : (
          <div className="divide-y divide-slate-100">
            {filtered.map((log) => (
              <div
                key={log.id}
                className="p-5 flex items-start gap-4 hover:bg-slate-50/50 transition-colors"
              >
                <div className="w-9 h-9 rounded-xl bg-slate-100 grid place-items-center shrink-0">
                  <Activity className="w-4 h-4 text-slate-500" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="text-[13px] font-medium text-slate-800">
                        {log.action}
                      </p>
                      {log.details && (
                        <p className="mt-1 text-[12px] text-slate-500 leading-relaxed">
                          {log.details}
                        </p>
                      )}
                    </div>
                    <span className="text-[11px] text-slate-400 whitespace-nowrap shrink-0">
                      {formatDateTime(log.timestamp)}
                    </span>
                  </div>
                  <div className="mt-3 flex items-center gap-3 flex-wrap">
                    {log.userId && (
                      <span className="inline-flex items-center gap-1.5 text-[11px] text-slate-500">
                        <User className="w-3 h-3" />
                        {log.userId}
                      </span>
                    )}
                    {log.ipAddress && (
                      <span className="inline-flex items-center gap-1.5 text-[11px] text-slate-500">
                        <Globe className="w-3 h-3" />
                        {log.ipAddress}
                      </span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
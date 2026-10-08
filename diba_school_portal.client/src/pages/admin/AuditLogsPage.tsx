import { useEffect, useState, type ChangeEvent } from "react";
import { ScrollText, Activity, User, Globe } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Spinner, SearchInput } from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { formatDateTime } from "../../lib/format";
import api from "../../lib/api";

interface AuditLog {
  id: number;
  userId: string | null;
  action: string;
  details: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export default function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const { data } = await api.get<AuditLog[]>("/auditlog/logs?pageNumber=1&pageSize=50");
        setLogs(Array.isArray(data) ? data : []);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const filtered = debounced
    ? logs.filter(
        (l) =>
          l.action.toLowerCase().includes(debounced.toLowerCase()) ||
          (l.details ?? "").toLowerCase().includes(debounced.toLowerCase())
      )
    : logs;

  const onSearchChange = (e: ChangeEvent<HTMLInputElement>) => setSearch(e.target.value);

  return (
    <div>
      <PageHeader
        eyebrow="System"
        title="Audit logs"
        description="Every significant action recorded across the portal."
      />
      <div className="mb-6 max-w-sm">
        <SearchInput placeholder="Filter by action or user…" value={search} onChange={onSearchChange} />
      </div>
      <Card padding="none">
        {loading ? (
          <div className="py-16 flex justify-center">
            <Spinner size="lg" label="Loading logs…" />
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState icon={ScrollText} title="No logs yet" description="Activity will appear here as it happens." />
        ) : (
          <div className="divide-y divide-slate-100">
            {filtered.map((log) => (
              <div key={log.id} className="p-5 flex items-start gap-4 hover:bg-slate-50/50 transition-colors">
                <div className="w-9 h-9 rounded-xl bg-slate-100 grid place-items-center shrink-0">
                  <Activity className="w-4 h-4 text-slate-500" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="text-[13px] font-medium text-slate-800">{log.action}</p>
                      {log.details && (
                        <p className="mt-1 text-[12px] text-slate-500 leading-relaxed">{log.details}</p>
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

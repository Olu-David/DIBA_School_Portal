import { useEffect, useState } from "react";
import { Users, Phone, Mail } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Avatar, Badge, Skeleton, SearchInput } from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import api from "../../lib/api";

interface ParentRow {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  isActive: boolean;
  childCount?: number;
}

export default function ParentsPage() {
  const [parents, setParents] = useState<ParentRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);

  useEffect(() => {
    (async () => {
      try {
        // We'll list parents through the students endpoint as a stopgap
        // (add a dedicated /parent endpoint later if needed)
        const { data } = await api.get<any>("/student?pageNumber=1&pageSize=1");
        console.log("placeholder", data);
      } catch {
        // ignore — page is a placeholder for now
      } finally {
        setLoading(false);
      }
    })();
  }, [debounced]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Parents & guardians"
        description="Every parent account linked to students."
      />

      <div className="max-w-md">
        <SearchInput
          placeholder="Search parents by name or phone…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
          </div>
        ) : parents.length === 0 ? (
          <EmptyState
            icon={Users}
            title="No parents listed here yet"
            description="Parents appear on the Students page once linked. A dedicated parent directory is coming soon."
          />
        ) : (
          <div className="divide-y divide-slate-100">
            {parents.map((p) => (
              <div key={p.id} className="px-6 py-4 flex items-center gap-4">
                <Avatar name={`${p.firstName} ${p.lastName}`} src={p.profilePictureUrl} size="md" />
                <div className="flex-1 min-w-0">
                  <p className="text-[13px] font-semibold text-slate-900">
                    {p.firstName} {p.lastName}
                  </p>
                  <div className="flex items-center gap-3 mt-0.5 text-[11px] text-slate-500">
                    {p.phoneNumber && (
                      <span className="inline-flex items-center gap-1"><Phone className="w-3 h-3" />{p.phoneNumber}</span>
                    )}
                    {p.email && (
                      <span className="inline-flex items-center gap-1"><Mail className="w-3 h-3" />{p.email}</span>
                    )}
                  </div>
                </div>
                <Badge variant={p.isActive ? "emerald" : "neutral"}>
                  {p.isActive ? "Active" : "Inactive"}
                </Badge>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
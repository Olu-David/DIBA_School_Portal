import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { BookOpen, Plus, Hash } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, SearchInput, Skeleton } from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface Subject { id: string; name: string; code: string; }

export default function SubjectsPage() {
  const navigate = useNavigate();
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 300);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<Subject[]>>("/subject/all");
        if (data.success && Array.isArray(data.data)) setSubjects(data.data);
      } catch { /* silent */ }
      finally { setLoading(false); }
    })();
  }, []);

  const filtered = debounced
    ? subjects.filter(
        (s) =>
          s.name.toLowerCase().includes(debounced.toLowerCase()) ||
          s.code.toLowerCase().includes(debounced.toLowerCase())
      )
    : subjects;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="Subjects"
        description="The full subject catalogue available across the school."
        actions={
          <Button
            icon={Plus}
            onClick={() => navigate("/admin/subjects/new")}
          >
            New subject
          </Button>
        }
      />
      <div className="max-w-md">
        <SearchInput
          placeholder="Search subjects or codes..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>
      <Card padding="none">
        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 p-6">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState
            icon={BookOpen}
            title={search ? "No subjects found" : "No subjects yet"}
            description={search ? `Nothing matches "${search}".` : "Add subjects to get started."}
            action={
              <Button
                icon={Plus}
                onClick={() => navigate("/admin/subjects/new")}
              >
                New subject
              </Button>
            }
          />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 divide-x divide-y divide-surface-100">
            {filtered.map((s) => (
              <div key={s.id} className="p-5 hover:bg-violet-50/40 transition-colors">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-violet-50 grid place-items-center shrink-0">
                    <BookOpen className="w-4 h-4 text-violet-600" strokeWidth={2} />
                  </div>
                  <div className="min-w-0">
                    <p className="text-[14px] font-semibold text-surface-900 truncate">{s.name}</p>
                    {s.code && (
                      <p className="text-[11px] text-surface-400 font-mono mt-0.5 inline-flex items-center gap-1">
                        <Hash className="w-3 h-3" />
                        {s.code}
                      </p>
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
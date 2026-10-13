import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { FileText, ArrowRight } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Skeleton, Avatar, SearchInput, Button } from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

interface StudentRow {
  id: string;
  admissionNumber: string;
  fullName: string;
  className: string;
  profilePictureUrl: string | null;
  isActive: boolean;
  outstandingBalance: number;
  isFullyPaid: boolean;
}

export default function ReportCardsPage() {
  const navigate = useNavigate();
  const [students, setStudents] = useState<StudentRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);

  useEffect(() => {
    setLoading(true);
    (async () => {
      try {
        const params = new URLSearchParams({ pageNumber: "1", pageSize: "50" });
        if (debounced.trim()) params.set("searchTerm", debounced.trim());
        const { data } = await api.get<ApiResponse<Paginated<StudentRow>>>(`/student?${params}`);
        if (data.success) setStudents(data.data.items);
      } finally {
        setLoading(false);
      }
    })();
  }, [debounced]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Results"
        title="Report cards"
        description="Find a student to view or download their term report card."
      />

      <div className="max-w-md">
        <SearchInput
          placeholder="Search by name or admission number..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
          </div>
        ) : students.length === 0 ? (
          <EmptyState
            icon={FileText}
            title="No students found"
            description="Try a different search term."
          />
        ) : (
          <div className="divide-y divide-surface-100">
            {students.map((s) => (
              <div key={s.id} className="px-6 py-4 flex items-center gap-4">
                <Avatar name={s.fullName} src={s.profilePictureUrl} size="md" />
                <div className="flex-1 min-w-0">
                  <p className="text-[13px] font-semibold text-surface-900 truncate">{s.fullName}</p>
                  <p className="text-[11px] text-surface-400 font-mono truncate">
                    {s.admissionNumber} - {s.className}
                  </p>
                </div>
                <Button
                  size="sm"
                  variant="secondary"
                  iconRight={ArrowRight}
                  onClick={() => navigate(`/admin/students/${s.id}`)}
                >
                  Open
                </Button>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
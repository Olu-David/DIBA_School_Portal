import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Users, UserPlus, Phone, Mail } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, Avatar, Badge, Skeleton } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

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
  const navigate = useNavigate();
  const [parents, setParents] = useState<ParentRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        // If you have a /parent endpoint, use it. Otherwise this page shows
        // the placeholder message gracefully.
        const { data } = await api.get<ApiResponse<Paginated<ParentRow>>>(
          "/parent?pageNumber=1&pageSize=100"
        );
        if (data.success) setParents(data.data.items);
      } catch {
        // Endpoint may not exist yet — the placeholder text below covers it.
        setError("Parent directory API is not available yet.");
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Parents & guardians"
        description="Every parent account linked to students."
        actions={
          <Button
            icon={UserPlus}
            onClick={() => navigate("/admin/parents/new")}
          >
            Add parent
          </Button>
        }
      />

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : parents.length === 0 ? (
          <EmptyState
            icon={Users}
            title="No parents listed yet"
            description={
              error ??
              "Click 'Add parent' to create a parent account and link them to a student."
            }
            action={
              <Button
                icon={UserPlus}
                onClick={() => navigate("/admin/parents/new")}
              >
                Add parent
              </Button>
            }
          />
        ) : (
          <div className="divide-y divide-surface-100">
            {parents.map((p) => (
              <div key={p.id} className="px-6 py-4 flex items-center gap-4">
                <Avatar
                  name={`${p.firstName} ${p.lastName}`}
                  src={p.profilePictureUrl}
                  size="md"
                />
                <div className="flex-1 min-w-0">
                  <p className="text-[13px] font-semibold text-surface-900">
                    {p.firstName} {p.lastName}
                  </p>
                  <div className="flex items-center gap-3 mt-0.5 text-[11px] text-surface-500">
                    {p.phoneNumber && (
                      <span className="inline-flex items-center gap-1">
                        <Phone className="w-3 h-3" />
                        {p.phoneNumber}
                      </span>
                    )}
                    {p.email && (
                      <span className="inline-flex items-center gap-1">
                        <Mail className="w-3 h-3" />
                        {p.email}
                      </span>
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
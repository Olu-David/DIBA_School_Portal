import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { UserCog, UserPlus, Phone, Mail } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Pagination, SearchInput, Select,
  Avatar, Badge, Skeleton,
} from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

interface TeacherRow {
  id: string;
  staffId: string;
  fullName: string;
  email: string | null;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  qualification: string;
  primaryRole: string;
  isActive: boolean;
  dateEmployed: string;
}

export default function TeachersPage() {
  const navigate = useNavigate();
  const [rows, setRows] = useState<TeacherRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [role, setRole] = useState("");

  useEffect(() => {
    setLoading(true);
    (async () => {
      try {
        const params = new URLSearchParams({
          pageNumber: String(page),
          pageSize: String(pageSize),
        });
        if (debounced.trim()) params.set("searchTerm", debounced.trim());
        if (role) params.set("primaryRole", role);

        const { data } = await api.get<ApiResponse<Paginated<TeacherRow>>>(`/teacher?${params}`);
        if (data.success) {
          setRows(data.data.items);
          setTotal(data.data.totalCount);
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    })();
  }, [debounced, role, page, pageSize]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Teachers & staff"
        description="Every teaching and non-teaching staff member."
        actions={<Button icon={UserPlus}>Add staff</Button>}
      />

      <Card padding="sm">
        <div className="flex flex-col lg:flex-row gap-3">
          <div className="flex-1">
            <SearchInput
              placeholder="Search by name, staff ID, or email..."
              value={search}
              onChange={(e: ChangeEvent<HTMLInputElement>) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="w-full lg:w-56">
            <Select
              value={role}
              onChange={(e) => { setRole(e.target.value); setPage(1); }}
              options={[
                { value: "", label: "All roles" },
                { value: "Teacher", label: "Teacher" },
                { value: "FormTeacher", label: "Form Teacher" },
                { value: "Principal", label: "Principal" },
                { value: "VicePrincipal", label: "Vice Principal" },
                { value: "Headmaster", label: "Headmaster" },
                { value: "Headmistress", label: "Headmistress" },
                { value: "SchoolAdmin", label: "School Admin" },
                { value: "Accountant", label: "Accountant" },
                { value: "Registrar", label: "Registrar" },
              ]}
            />
          </div>
        </div>
      </Card>

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-14" />)}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState icon={UserCog} title="No staff found" description="Adjust filters or add your first staff member." />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50/50">
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Staff</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Role</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Qualification</th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Contact</th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-100">
                  {rows.map((t) => (
                    <tr
                      key={t.id}
                      onClick={() => navigate(`/admin/teachers/${t.id}`)}
                      className="hover:bg-surface-50/60 cursor-pointer"
                    >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          <Avatar name={t.fullName} src={t.profilePictureUrl} size="md" />
                          <div>
                            <p className="text-[13px] font-semibold text-surface-900">{t.fullName}</p>
                            <p className="text-[11px] text-surface-400 font-mono">{t.staffId}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4"><Badge variant="violet">{t.primaryRole}</Badge></td>
                      <td className="px-6 py-4 text-[12px] text-surface-600">{t.qualification || "-"}</td>
                      <td className="px-6 py-4">
                        <div className="flex flex-col gap-1 text-[12px] text-surface-600">
                          {t.email && (
                            <span className="inline-flex items-center gap-1.5">
                              <Mail className="w-3 h-3 text-surface-400" />{t.email}
                            </span>
                          )}
                          {t.phoneNumber && (
                            <span className="inline-flex items-center gap-1.5">
                              <Phone className="w-3 h-3 text-surface-400" />{t.phoneNumber}
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="px-6 py-4 text-right">
                        {t.isActive ? <Badge variant="emerald">Active</Badge> : <Badge variant="neutral">Inactive</Badge>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="px-6">
              <Pagination pageNumber={page} pageSize={pageSize} totalCount={total} onPageChange={setPage} />
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
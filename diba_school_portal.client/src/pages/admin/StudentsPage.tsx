import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  Users, UserPlus, Filter, Download, MoreHorizontal,
  Phone, GraduationCap, AlertCircle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Pagination, SearchInput,
  Select, Avatar, Badge, Skeleton,
} from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { formatNaira } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";
import type { StudentRow } from "../../types/student";

const SECTION_TONE: Record<string, "rose" | "sky" | "violet" | "emerald"> = {
  Nursery: "rose",
  Primary: "sky",
  JuniorSecondary: "violet",
  SeniorSecondary: "emerald",
};

export default function StudentsPage() {
  const navigate = useNavigate();

  const [students, setStudents] = useState<StudentRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);

  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);

  const [classId, setClassId] = useState("");
  const [hasOutstanding, setHasOutstanding] = useState("");
  const [classes, setClasses] = useState<{ id: string; name: string }[]>([]);

  // Load classes for the filter
  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<{ id: string; name: string }[]>>(
          "/schoolclass/all"
        );
        if (data.success) setClasses(data.data);
      } catch {
        /* silent */
      }
    })();
  }, []);

  // Load students
  useEffect(() => {
    setLoading(true);
    (async () => {
      try {
        const params = new URLSearchParams({
          pageNumber: String(page),
          pageSize: String(pageSize),
        });
        if (debounced.trim()) params.set("searchTerm", debounced.trim());
        if (classId) params.set("classId", classId);
        if (hasOutstanding) params.set("hasOutstanding", hasOutstanding);

        const { data } = await api.get<ApiResponse<Paginated<StudentRow>>>(
          `/student?${params}`
        );
        if (data.success) {
          setStudents(data.data.items);
          setTotal(data.data.totalCount);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [debounced, classId, hasOutstanding, page, pageSize]);

  const onSearchChange = (e: ChangeEvent<HTMLInputElement>) => {
    setSearch(e.target.value);
    setPage(1);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="Students"
        description="Every enrolment across all sections of the school."
        actions={
          <>
            <Button variant="secondary" icon={Download}>
              Export
            </Button>
            <Button icon={UserPlus} onClick={() => navigate("/admin/students/new")}>
              Enroll student
            </Button>
          </>
        }
      />

      {/* Filters */}
      <Card padding="sm">
        <div className="flex flex-col lg:flex-row gap-3">
          <div className="flex-1">
            <SearchInput
              placeholder="Search by name or admission number…"
              value={search}
              onChange={onSearchChange}
            />
          </div>
          <div className="w-full lg:w-56">
            <Select
              value={classId}
              onChange={(e) => {
                setClassId(e.target.value);
                setPage(1);
              }}
              placeholder="All classes"
              options={[
                { value: "", label: "All classes" },
                ...classes.map((c) => ({ value: c.id, label: c.name })),
              ]}
            />
          </div>
          <div className="w-full lg:w-52">
            <Select
              value={hasOutstanding}
              onChange={(e) => {
                setHasOutstanding(e.target.value);
                setPage(1);
              }}
              options={[
                { value: "", label: "Any balance" },
                { value: "true", label: "Has outstanding" },
                { value: "false", label: "Fully paid" },
              ]}
            />
          </div>
          <Button variant="secondary" icon={Filter}>
            More
          </Button>
        </div>
      </Card>

      {/* List */}
      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="flex items-center gap-4">
                <Skeleton className="w-10 h-10 rounded-full" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-3 w-48" />
                  <Skeleton className="h-3 w-32" />
                </div>
                <Skeleton className="h-6 w-20 rounded-lg" />
              </div>
            ))}
          </div>
        ) : students.length === 0 ? (
          <EmptyState
            icon={Users}
            title="No students found"
            description="Try adjusting your filters, or enrol a new student."
            action={
              <Button icon={UserPlus} onClick={() => navigate("/admin/students/new")}>
                Enrol student
              </Button>
            }
          />
        ) : (
          <>
            {/* Desktop table */}
            <div className="hidden lg:block overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-100 bg-slate-50/50">
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">
                      Student
                    </th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">
                      Class
                    </th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">
                      Guardian
                    </th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">
                      Balance
                    </th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-slate-500 px-6 py-3">
                      Status
                    </th>
                    <th className="w-12"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {students.map((s) => (
                    <tr
                      key={s.id}
                      onClick={() => navigate(`/admin/students/${s.id}`)}
                      className="hover:bg-slate-50/60 cursor-pointer transition-colors"
                    >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          <Avatar name={s.fullName} src={s.profilePictureUrl} size="md" />
                          <div>
                            <p className="text-[13px] font-semibold text-slate-900">
                              {s.fullName}
                            </p>
                            <p className="text-[11px] text-slate-400 font-mono">
                              {s.admissionNumber}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <Badge variant="sky">
                          <GraduationCap className="w-3 h-3" />
                          {s.className}
                        </Badge>
                      </td>
                      <td className="px-6 py-4">
                        {s.primaryGuardianPhone ? (
                          <span className="inline-flex items-center gap-1.5 text-[12px] text-slate-600">
                            <Phone className="w-3 h-3 text-slate-400" />
                            {s.primaryGuardianPhone}
                          </span>
                        ) : (
                          <span className="text-[12px] text-slate-400">—</span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right">
                        {s.outstandingBalance > 0 ? (
                          <span className="text-[13px] font-semibold text-rose-600">
                            {formatNaira(s.outstandingBalance)}
                          </span>
                        ) : (
                          <span className="text-[13px] font-medium text-emerald-600">
                            Fully paid
                          </span>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right">
                        {s.isActive ? (
                          <Badge variant="emerald">Active</Badge>
                        ) : (
                          <Badge variant="neutral">Inactive</Badge>
                        )}
                      </td>
                      <td className="px-6 py-4 text-right">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            /* open menu */
                          }}
                          className="p-1.5 rounded-lg text-slate-400 hover:bg-slate-100"
                        >
                          <MoreHorizontal className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Mobile cards */}
            <div className="lg:hidden divide-y divide-slate-100">
              {students.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => navigate(`/admin/students/${s.id}`)}
                  className="w-full text-left p-4 hover:bg-slate-50 flex items-center gap-3"
                >
                  <Avatar name={s.fullName} src={s.profilePictureUrl} size="lg" />
                  <div className="flex-1 min-w-0">
                    <p className="text-[14px] font-semibold text-slate-900 truncate">
                      {s.fullName}
                    </p>
                    <p className="text-[11px] text-slate-400 font-mono mt-0.5">
                      {s.admissionNumber} · {s.className}
                    </p>
                    {s.outstandingBalance > 0 && (
                      <p className="text-[12px] text-rose-600 font-medium mt-1">
                        Outstanding: {formatNaira(s.outstandingBalance)}
                      </p>
                    )}
                  </div>
                </button>
              ))}
            </div>

            {/* Pagination */}
            <div className="px-6">
              <Pagination
                pageNumber={page}
                pageSize={pageSize}
                totalCount={total}
                onPageChange={setPage}
              />
            </div>
          </>
        )}
      </Card>

      {/* Amber banner — school-specific alert */}
      {!loading && students.some((s) => s.outstandingBalance > 0) && (
        <div className="bg-gradient-to-br from-amber-50 to-amber-100/40 border border-amber-200 rounded-2xl p-5 flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="w-11 h-11 rounded-xl bg-white grid place-items-center shadow-sm shrink-0">
            <AlertCircle className="w-5 h-5 text-amber-600" />
          </div>
          <div className="flex-1">
            <p className="text-[14px] font-semibold text-slate-900">
              Outstanding fees detected
            </p>
            <p className="text-[12px] text-slate-600 mt-0.5">
              {students.filter((s) => s.outstandingBalance > 0).length} student(s) on
              this page have unpaid balances.
            </p>
          </div>
          <Button variant="secondary" onClick={() => navigate("/admin/finance/arrears")}>
            View arrears
          </Button>
        </div>
      )}
    </div>
  );
}
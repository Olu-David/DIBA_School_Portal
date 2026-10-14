import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  UserCog,
  UserPlus,
  Phone,
  Mail,
  X,
  GraduationCap,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button,
  Card,
  EmptyState,
  Pagination,
  SearchInput,
  Select,
  Avatar,
  Badge,
  Skeleton,
  FormField,
  Input,
} from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

/* =========================================================
   Types
   ========================================================= */

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

interface SchoolClass {
  id: string;
  name: string;
  academicSection: string;
  displayOrder?: number;
}

interface Subject {
  id: string;
  name: string;
  code: string;
}

const STAFF_ROLES = [
  "Teacher",
  "FormTeacher",
  "Principal",
  "VicePrincipal",
  "Headmaster",
  "Headmistress",
  "SchoolAdmin",
  "Accountant",
  "Registrar",
];

/* =========================================================
   Page
   ========================================================= */

export default function TeachersPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [rows, setRows] = useState<TeacherRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [role, setRole] = useState("");

  const [addOpen, setAddOpen] = useState(false);

  /* ---------- fetch ---------- */
  const loadTeachers = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        pageNumber: String(page),
        pageSize: String(pageSize),
      });
      if (debounced.trim()) params.set("searchTerm", debounced.trim());
      if (role) params.set("primaryRole", role);

      // Use a loose type — we'll narrow it below. Some services return
      // `{ success, data: { items, totalCount } }`, some `{ success, data: [...] }`.
      const { data } = await api.get<any>(`/teacher?${params}`);

      console.log("[teachers] API response:", data);

      if (!data?.success) {
        const msg = data?.message ?? "Could not load staff list";
        console.warn("[teachers] success=false:", msg);
        toast({ variant: "error", title: msg });
        setRows([]);
        setTotal(0);
        return;
      }

      // ---- Normalise payload shape ----
      const payload = data.data;
      let items: TeacherRow[] = [];
      let totalCount = 0;

      if (Array.isArray(payload)) {
        // Plain array — no pagination on the server
        items = payload;
        totalCount = payload.length;
      } else if (payload && Array.isArray(payload.items)) {
        // { items, totalCount, ... }
        items = payload.items;
        totalCount = payload.totalCount ?? items.length;
      } else if (payload && Array.isArray(payload.value)) {
        // ASP.NET default wrapper when a List<T> gets serialized oddly
        items = payload.value;
        totalCount = payload.totalCount ?? items.length;
      } else {
        console.warn(
          "[teachers] Unrecognised response shape. data.data =",
          payload
        );
        items = [];
        totalCount = 0;
      }

      setRows(items);
      setTotal(totalCount);
    } catch (err: any) {
      // Extract everything useful for the console
      const status = err?.response?.status;
      const body = err?.response?.data;
      const msgFromBody = body?.message;
      console.error("[teachers] fetch failed:", {
        status,
        message: err?.message,
        body,
      });

      const friendly =
        status === 401
          ? "Your session expired — please log in again."
          : status === 403
          ? "You don't have permission to view staff."
          : status === 404
          ? "Staff endpoint not found."
          : status === 500
          ? "Server error while loading staff."
          : msgFromBody ?? "Could not load staff list";

      toast({ variant: "error", title: friendly });
      setRows([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTeachers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debounced, role, page, pageSize]);

  /* ---------- render ---------- */
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Teachers & staff"
        description="Every teaching and non-teaching staff member."
        actions={
          <Button icon={UserPlus} onClick={() => setAddOpen(true)}>
            Add staff
          </Button>
        }
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
              onChange={(e) => {
                setRole(e.target.value);
                setPage(1);
              }}
              options={[
                { value: "", label: "All roles" },
                ...STAFF_ROLES.map((r) => ({ value: r, label: r })),
              ]}
            />
          </div>
        </div>
      </Card>

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={UserCog}
            title="No staff found"
            description="Adjust filters or add your first staff member."
            action={
              <Button icon={UserPlus} onClick={() => setAddOpen(true)}>
                Add staff
              </Button>
            }
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50/50">
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                      Staff
                    </th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                      Role
                    </th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                      Qualification
                    </th>
                    <th className="text-left text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                      Contact
                    </th>
                    <th className="text-right text-[11px] font-semibold uppercase tracking-wider text-surface-500 px-6 py-3">
                      Status
                    </th>
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
                          <Avatar
                            name={t.fullName}
                            src={t.profilePictureUrl}
                            size="md"
                          />
                          <div>
                            <p className="text-[13px] font-semibold text-surface-900">
                              {t.fullName}
                            </p>
                            <p className="text-[11px] text-surface-400 font-mono">
                              {t.staffId}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <Badge variant="violet">{t.primaryRole}</Badge>
                      </td>
                      <td className="px-6 py-4 text-[12px] text-surface-600">
                        {t.qualification || "-"}
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex flex-col gap-1 text-[12px] text-surface-600">
                          {t.email && (
                            <span className="inline-flex items-center gap-1.5">
                              <Mail className="w-3 h-3 text-surface-400" />
                              {t.email}
                            </span>
                          )}
                          {t.phoneNumber && (
                            <span className="inline-flex items-center gap-1.5">
                              <Phone className="w-3 h-3 text-surface-400" />
                              {t.phoneNumber}
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="px-6 py-4 text-right">
                        {t.isActive ? (
                          <Badge variant="emerald">Active</Badge>
                        ) : (
                          <Badge variant="neutral">Inactive</Badge>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
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

      {/* =====================================================
          ADD STAFF MODAL
         ===================================================== */}
      {addOpen && (
        <AddStaffModal
          onClose={() => setAddOpen(false)}
          onCreated={async () => {
            setAddOpen(false);
            setPage(1);
            await loadTeachers();
          }}
          onSuccess={(msg) => toast({ variant: "success", title: msg })}
          onError={(msg) => toast({ variant: "error", title: msg })}
        />
      )}
    </div>
  );
}

/* =========================================================
   Add-staff modal
   ========================================================= */

interface AddStaffModalProps {
  onClose: () => void;
  onCreated: () => void;
  onSuccess: (msg: string) => void;
  onError: (msg: string) => void;
}

function AddStaffModal({
  onClose,
  onCreated,
  onSuccess,
  onError,
}: AddStaffModalProps) {
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [loadingRefs, setLoadingRefs] = useState(true);

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    password: "",
    qualification: "",
    dateEmployed: new Date().toISOString().slice(0, 10),
    primaryRole: "Teacher",
  });

  const [selectedClassIds, setSelectedClassIds] = useState<string[]>([]);
  const [selectedSubjectIds, setSelectedSubjectIds] = useState<string[]>([]);

  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // ---- load reference data ----
  useEffect(() => {
    (async () => {
      const [cRes, sRes] = await Promise.allSettled([
        api.get<ApiResponse<SchoolClass[]>>("/schoolclass/all"),
        api.get<ApiResponse<Subject[]>>("/subject/all"),
      ]);

      if (cRes.status === "fulfilled" && cRes.value.data.success) {
        const sorted = [...(cRes.value.data.data ?? [])].sort((a, b) => {
          const ao = a.displayOrder ?? 9999;
          const bo = b.displayOrder ?? 9999;
          return ao !== bo ? ao - bo : a.name.localeCompare(b.name);
        });
        setClasses(sorted);
      }

      if (sRes.status === "fulfilled" && sRes.value.data.success) {
        setSubjects(sRes.value.data.data ?? []);
      }

      setLoadingRefs(false);
    })();
  }, []);

  const toggle = (
    list: string[],
    setList: (v: string[]) => void,
    id: string
  ) => {
    setList(list.includes(id) ? list.filter((x) => x !== id) : [...list, id]);
  };

  // ---- validation ----
  const validate = (): boolean => {
    const e: Record<string, string> = {};
    if (!form.firstName.trim()) e.firstName = "First name is required";
    if (!form.lastName.trim()) e.lastName = "Last name is required";
    if (!form.email.trim()) e.email = "Email is required";
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
      e.email = "Enter a valid email";
    if (!form.phoneNumber.trim()) e.phoneNumber = "Phone is required";
    if (!form.password.trim()) e.password = "Set an initial password";
    else if (form.password.length < 6)
      e.password = "At least 6 characters";
    if (!form.primaryRole) e.primaryRole = "Choose a role";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  // ---- submit ----
  const handleSubmit = async () => {
    if (!validate()) return;

    const payload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim(),
      phoneNumber: form.phoneNumber.trim(),
      password: form.password,
      qualification: form.qualification.trim() || null,
      dateEmployed: form.dateEmployed || null,
      primaryRole: form.primaryRole,
      classIds: selectedClassIds,
      subjectIds: selectedSubjectIds,
    };

    setSaving(true);
    try {
      const { data } = await api.post<ApiResponse<{ id: string }>>(
        "/teacher/create",
        payload
      );

      if (!data.success) {
        onError(data.message ?? "Failed to create staff member");
        return;
      }

      onSuccess("Staff member created");
      onCreated();
    } catch (err: unknown) {
      const msg =
        (err as any)?.response?.data?.message ??
        "Could not create staff member";
      onError(msg);
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <div
        className="fixed inset-0 bg-surface-950/50 z-40"
        onClick={() => !saving && onClose()}
        aria-hidden
      />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl w-full max-w-3xl max-h-[92vh] flex flex-col overflow-hidden">
          {/* Header */}
          <div className="p-5 flex items-center justify-between border-b border-surface-100 shrink-0">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-violet-50 grid place-items-center">
                <UserPlus className="w-5 h-5 text-violet-600" />
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-surface-900">
                  Add staff member
                </h2>
                <p className="text-[12px] text-surface-500">
                  Creates a login account and staff profile.
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => !saving && onClose()}
              className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          {/* Body */}
          <div className="flex-1 overflow-y-auto p-5 space-y-6">
            {/* Identity */}
            <section>
              <h3 className="text-[12px] font-semibold uppercase tracking-wider text-surface-500 mb-3">
                Identity
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField label="First name" required error={errors.firstName}>
                  <Input
                    value={form.firstName}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, firstName: e.target.value }))
                    }
                    placeholder="e.g. Emeka"
                  />
                </FormField>
                <FormField label="Last name" required error={errors.lastName}>
                  <Input
                    value={form.lastName}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, lastName: e.target.value }))
                    }
                    placeholder="e.g. Nwosu"
                  />
                </FormField>
                <FormField
                  label="Email (login)"
                  required
                  error={errors.email}
                >
                  <Input
                    type="email"
                    value={form.email}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, email: e.target.value }))
                    }
                    placeholder="staff@school.edu.ng"
                  />
                </FormField>
                <FormField
                  label="Phone number"
                  required
                  error={errors.phoneNumber}
                >
                  <Input
                    value={form.phoneNumber}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, phoneNumber: e.target.value }))
                    }
                    placeholder="080…"
                  />
                </FormField>
                <FormField
                  label="Initial password"
                  required
                  error={errors.password}
                  hint="They can change it after first login."
                >
                  <Input
                    type="password"
                    value={form.password}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, password: e.target.value }))
                    }
                    placeholder="At least 6 characters"
                  />
                </FormField>
                <FormField
                  label="Primary role"
                  required
                  error={errors.primaryRole}
                >
                  <Select
                    value={form.primaryRole}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, primaryRole: e.target.value }))
                    }
                    options={STAFF_ROLES.map((r) => ({
                      value: r,
                      label: r,
                    }))}
                  />
                </FormField>
              </div>
            </section>

            {/* Professional */}
            <section>
              <h3 className="text-[12px] font-semibold uppercase tracking-wider text-surface-500 mb-3">
                Professional details
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField label="Qualification">
                  <Input
                    value={form.qualification}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, qualification: e.target.value }))
                    }
                    placeholder="e.g. B.Sc, PGDE"
                  />
                </FormField>
                <FormField label="Date employed">
                  <Input
                    type="date"
                    value={form.dateEmployed}
                    onChange={(e) =>
                      setForm((f) => ({ ...f, dateEmployed: e.target.value }))
                    }
                  />
                </FormField>
              </div>
            </section>

            {/* Classes + subjects */}
            <section>
              <h3 className="text-[12px] font-semibold uppercase tracking-wider text-surface-500 mb-3 flex items-center gap-1.5">
                <GraduationCap className="w-3.5 h-3.5" />
                Assignments (optional)
              </h3>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Classes */}
                <div>
                  <p className="text-[12px] font-medium text-surface-700 mb-2">
                    Classes
                  </p>
                  {loadingRefs ? (
                    <Skeleton className="h-24" />
                  ) : classes.length === 0 ? (
                    <p className="text-[12px] text-surface-400 italic">
                      No classes yet
                    </p>
                  ) : (
                    <div className="max-h-48 overflow-y-auto border border-surface-200 rounded-xl divide-y divide-surface-100">
                      {classes.map((c) => (
                        <label
                          key={c.id}
                          className="flex items-center gap-2 px-3 py-2 text-[12px] hover:bg-surface-50 cursor-pointer"
                        >
                          <input
                            type="checkbox"
                            checked={selectedClassIds.includes(c.id)}
                            onChange={() =>
                              toggle(
                                selectedClassIds,
                                setSelectedClassIds,
                                c.id
                              )
                            }
                            className="w-4 h-4 rounded"
                          />
                          <span className="text-surface-800">{c.name}</span>
                        </label>
                      ))}
                    </div>
                  )}
                  {selectedClassIds.length > 0 && (
                    <p className="text-[11px] text-surface-500 mt-1">
                      {selectedClassIds.length} selected
                    </p>
                  )}
                </div>

                {/* Subjects */}
                <div>
                  <p className="text-[12px] font-medium text-surface-700 mb-2">
                    Subjects
                  </p>
                  {loadingRefs ? (
                    <Skeleton className="h-24" />
                  ) : subjects.length === 0 ? (
                    <p className="text-[12px] text-surface-400 italic">
                      No subjects yet
                    </p>
                  ) : (
                    <div className="max-h-48 overflow-y-auto border border-surface-200 rounded-xl divide-y divide-surface-100">
                      {subjects.map((s) => (
                        <label
                          key={s.id}
                          className="flex items-center gap-2 px-3 py-2 text-[12px] hover:bg-surface-50 cursor-pointer"
                        >
                          <input
                            type="checkbox"
                            checked={selectedSubjectIds.includes(s.id)}
                            onChange={() =>
                              toggle(
                                selectedSubjectIds,
                                setSelectedSubjectIds,
                                s.id
                              )
                            }
                            className="w-4 h-4 rounded"
                          />
                          <span className="text-surface-800 flex-1 truncate">
                            {s.name}
                          </span>
                          <span className="text-[10px] text-surface-400 font-mono">
                            {s.code}
                          </span>
                        </label>
                      ))}
                    </div>
                  )}
                  {selectedSubjectIds.length > 0 && (
                    <p className="text-[11px] text-surface-500 mt-1">
                      {selectedSubjectIds.length} selected
                    </p>
                  )}
                </div>
              </div>
            </section>
          </div>

          {/* Footer */}
          <div className="p-5 border-t border-surface-100 flex justify-end gap-3 shrink-0">
            <Button variant="secondary" onClick={onClose} disabled={saving}>
              Cancel
            </Button>
            <Button icon={UserPlus} loading={saving} onClick={handleSubmit}>
              Create staff member
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
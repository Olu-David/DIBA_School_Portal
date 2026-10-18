import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import {
  UserCog, UserPlus, Phone, Mail, X, GraduationCap, RefreshCw,
  Copy, CheckCircle2, KeyRound, AlertCircle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Pagination, SearchInput, Select,
  Avatar, Badge, Skeleton, FormField, Input,
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

interface CreateAccountResponse {
  userId: string;
  temporaryPassword: string;
  role: string;
  email?: string | null;
  phoneNumber?: string;
  admissionNumber?: string | null;
  profilePictureUrl?: string | null;
}

/* =========================================================
   Constants
   ========================================================= */

const STAFF_ROLES = [
  "Teacher",
  "FormTeacher",
  "Principal",
  "VicePrincipal",
  "Headmaster",
  "Headmistress",
  "SchoolAdmin",
  "Accountant",
  "Cashier",
  "Registrar",
] as const;

// Used only in the create flow: only teaching roles get class/subject assignment
const TEACHING_ROLES = ["Teacher", "FormTeacher"] as const;

/* =========================================================
   Page
   ========================================================= */

export default function TeachersPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [rows, setRows] = useState<TeacherRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [role, setRole] = useState("");

  const [addOpen, setAddOpen] = useState(false);

  const [credentials, setCredentials] = useState<{
    fullName: string;
    role: string;
    password: string;
  } | null>(null);

  const abortRef = useRef<AbortController | null>(null);

  /* ---------- fetch ---------- */
  const loadTeachers = useCallback(async () => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    setError(null);
    try {
      const params = new URLSearchParams({
        pageNumber: String(page),
        pageSize: String(pageSize),
      });
      if (debounced.trim()) params.set("searchTerm", debounced.trim());
      if (role) params.set("primaryRole", role);

      const { data } = await api.get<ApiResponse<Paginated<TeacherRow>>>(
        `/teacher?${params}`,
        { signal: controller.signal }
      );

      if (!data.success || !data.data) {
        setError(data.message ?? "Could not load staff list.");
        setRows([]);
        setTotal(0);
        return;
      }

      const payload = data.data as unknown as {
        items?: TeacherRow[];
        totalCount?: number;
        data?: TeacherRow[];
        total?: number;
      };
      const items = payload.items ?? payload.data ?? [];
      const count = payload.totalCount ?? payload.total ?? items.length;

      setRows(items);
      setTotal(count);
    } catch (err: unknown) {
      const e2 = err as { name?: string; code?: string };
      if (e2?.name === "CanceledError" || e2?.code === "ERR_CANCELED") return;

      const e3 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e3?.response?.status;
      const message =
        e3?.response?.data?.message ??
        e3?.message ??
        (status === 401
          ? "Your session expired — please log in again."
          : status === 403
          ? "You don't have permission to view staff."
          : status === 500
          ? "Server error (500). Check the API logs."
          : status === 502
          ? "The API is not reachable (502). It may still be deploying."
          : "Could not load staff list.");

      setError(message);
      setRows([]);
      setTotal(0);
    } finally {
      if (abortRef.current === controller) {
        setLoading(false);
      }
    }
  }, [debounced, role, page, pageSize]);

  useEffect(() => {
    loadTeachers();
    return () => abortRef.current?.abort();
  }, [loadTeachers]);

  const roleOptions = useMemo(
    () => [
      { value: "", label: "All roles" },
      ...STAFF_ROLES.map((r) => ({ value: r, label: r })),
    ],
    []
  );

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
              options={roleOptions}
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
        ) : error ? (
          <div className="p-6 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">
                Couldn't load staff
              </p>
              <p className="text-[13px] text-red-800/80 mt-1">{error}</p>
            </div>
            <Button
              variant="secondary"
              icon={RefreshCw}
              onClick={loadTeachers}
            >
              Retry
            </Button>
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

      {addOpen && (
        <AddStaffModal
          onClose={() => setAddOpen(false)}
          onCreated={async () => {
            setAddOpen(false);
            setPage(1);
            await loadTeachers();
          }}
          onSuccess={(data) => {
            setAddOpen(false);
            setCredentials({
              fullName: `${data.firstName} ${data.lastName}`,
              role: data.role,
              password: data.temporaryPassword,
            });
          }}
          onError={(msg) => toast({ variant: "error", title: msg })}
        />
      )}

      {credentials && (
        <CredentialsModal
          data={credentials}
          onClose={() => setCredentials(null)}
        />
      )}
    </div>
  );
}

/* =========================================================
   Add-staff modal — unchanged
   ========================================================= */

interface AddStaffModalProps {
  onClose: () => void;
  onCreated: () => void;
  onSuccess: (data: {
    firstName: string;
    lastName: string;
    role: string;
    temporaryPassword: string;
  }) => void;
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
    qualification: "",
    dateEmployed: new Date().toISOString().slice(0, 10),
    primaryRole: "Teacher" as string,
  });

  const [selectedClassIds, setSelectedClassIds] = useState<string[]>([]);
  const [selectedSubjectIds, setSelectedSubjectIds] = useState<string[]>([]);

  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const [cRes, sRes] = await Promise.allSettled([
        api.get<ApiResponse<SchoolClass[]>>("/schoolclass/all"),
        api.get<ApiResponse<Subject[]>>("/subject/all"),
      ]);
      if (cancelled) return;

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
    return () => {
      cancelled = true;
    };
  }, []);

  const toggle = (
    list: string[],
    setList: (v: string[]) => void,
    id: string
  ) => {
    setList(list.includes(id) ? list.filter((x) => x !== id) : [...list, id]);
  };

  const validate = (): boolean => {
    const e: Record<string, string> = {};
    if (!form.firstName.trim()) e.firstName = "First name is required";
    if (!form.lastName.trim()) e.lastName = "Last name is required";
    if (!form.email.trim()) e.email = "Email is required";
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
      e.email = "Enter a valid email";
    if (!form.phoneNumber.trim()) e.phoneNumber = "Phone is required";
    if (!form.primaryRole) e.primaryRole = "Choose a role";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = async () => {
    if (!validate()) return;

    setSaving(true);
    try {
      const fd = new FormData();
      fd.append("firstName", form.firstName.trim());
      fd.append("lastName", form.lastName.trim());
      fd.append("role", form.primaryRole);
      fd.append("email", form.email.trim());
      fd.append("phoneNumber", form.phoneNumber.trim());

      const { data: createRes } = await api.post<
        ApiResponse<CreateAccountResponse>
      >("/auth/create-account", fd);

      if (!createRes.success || !createRes.data) {
        onError(createRes.message ?? "Failed to create staff member");
        return;
      }

      let teacherId: string | null = null;
      if ((TEACHING_ROLES as readonly string[]).includes(createRes.data.role)) {
        try {
          const { data: tRes } = await api.get<ApiResponse<{ id: string }>>(
            `/teacher/by-user/${createRes.data.userId}`
          );
          if (tRes.success && tRes.data) teacherId = tRes.data.id;
        } catch {
          /* non-fatal */
        }
      }

      if (teacherId && selectedClassIds.length > 0) {
        try {
          await api.put<ApiResponse<boolean>>(
            `/teacher/${teacherId}/classes`,
            selectedClassIds
          );
        } catch {
          /* non-fatal */
        }
      }

      if (teacherId && selectedSubjectIds.length > 0) {
        try {
          await api.put<ApiResponse<boolean>>(
            `/teacher/${teacherId}/subjects`,
            selectedSubjectIds
          );
        } catch {
          /* non-fatal */
        }
      }

      onCreated();
      onSuccess({
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        role: createRes.data.role,
        temporaryPassword: createRes.data.temporaryPassword,
      });
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      const message =
        e2?.response?.data?.message ??
        e2?.message ??
        "Could not create staff member";

      if (status === 415) {
        onError(
          "Server rejected the content type. The client should send FormData for /auth/create-account."
        );
      } else if (status === 403) {
        onError("You don't have permission to create staff accounts.");
      } else if (status === 409) {
        onError("A user with this email already exists.");
      } else {
        onError(message);
      }
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

          <div className="flex-1 overflow-y-auto p-5 space-y-6">
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
                <FormField label="Email (login)" required error={errors.email}>
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
              <p className="mt-3 text-[11px] text-surface-400">
                A temporary password will be generated automatically and shown
                after creation.
              </p>
            </section>

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

            <section>
              <h3 className="text-[12px] font-semibold uppercase tracking-wider text-surface-500 mb-3 flex items-center gap-1.5">
                <GraduationCap className="w-3.5 h-3.5" />
                Assignments (optional)
              </h3>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
                              toggle(selectedClassIds, setSelectedClassIds, c.id)
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

/* =========================================================
   Credentials modal
   ========================================================= */

interface CredentialsModalProps {
  data: { fullName: string; role: string; password: string };
  onClose: () => void;
}

function CredentialsModal({ data, onClose }: CredentialsModalProps) {
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(data.password);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard unavailable */
    }
  };

  return (
    <>
      <div
        className="fixed inset-0 bg-surface-950/50 z-40"
        onClick={onClose}
        aria-hidden
      />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
          <div className="text-center">
            <div className="w-14 h-14 rounded-2xl bg-emerald-50 grid place-items-center mx-auto mb-4">
              <CheckCircle2 className="w-6 h-6 text-emerald-600" />
            </div>
            <h2 className="text-[16px] font-semibold text-surface-900">
              {data.fullName} created
            </h2>
            <p className="text-[12px] text-surface-500 mt-1">
              Share these credentials with the staff member.
            </p>
          </div>

          <div className="mt-6 space-y-3">
            <div className="p-3.5 rounded-xl bg-surface-50 border border-surface-100">
              <p className="text-[11px] text-surface-500 mb-1">Role</p>
              <p className="text-[13px] font-semibold text-surface-900">
                {data.role}
              </p>
            </div>

            <div className="p-3.5 rounded-xl bg-surface-50 border border-surface-100">
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-[11px] text-surface-500 mb-1 flex items-center gap-1">
                    <KeyRound className="w-3 h-3" />
                    Temporary password
                  </p>
                  <p className="text-[13px] font-mono font-semibold text-surface-900 break-all">
                    {data.password}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={copy}
                  className="p-2 rounded-lg text-surface-400 hover:text-emerald-600 hover:bg-emerald-50"
                  title="Copy password"
                >
                  {copied ? (
                    <CheckCircle2 className="w-4 h-4 text-emerald-600" />
                  ) : (
                    <Copy className="w-4 h-4" />
                  )}
                </button>
              </div>
            </div>
          </div>

          <p className="mt-4 text-[11px] text-surface-500 text-center">
            The staff member must change this password after first login.
          </p>

          <div className="mt-6">
            <Button fullWidth onClick={onClose}>
              Done
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
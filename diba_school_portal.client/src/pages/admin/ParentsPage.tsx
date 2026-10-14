import { useEffect, useMemo, useState } from "react";
import {
  GraduationCap,
  Mail,
  Phone,
  Search,
  UserPlus,
  Users,
  X,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Avatar,
  Badge,
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  Select,
  Skeleton,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

/* =========================================================
   Types
   ========================================================= */

export interface ParentRow {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  isActive: boolean;
  childCount?: number;
}

interface SchoolClass {
  id: string;
  name: string;
  academicSection: string;
  displayOrder?: number;
}

interface StudentRow {
  id: string;
  admissionNumber: string;
  fullName: string;
  className: string;
  profilePictureUrl: string | null;
  isActive: boolean;
}

interface CreateParentPayload {
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string;
  studentProfileId: string;
  relationship: string;
  isPrimaryContact: boolean;
}

const RELATIONSHIPS = [
  "Father",
  "Mother",
  "Guardian",
  "Uncle",
  "Aunt",
  "Grandparent",
  "Sibling",
  "Other",
];

/* =========================================================
   Page
   ========================================================= */

export default function ParentsPage() {
  const { toast } = useToast();

  const [parents, setParents] = useState<ParentRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");

  const [addOpen, setAddOpen] = useState(false);

  /* ----- fetch parents ----- */
  const loadParents = async () => {
    setLoading(true);
    try {
      const { data } = await api.get<ApiResponse<Paginated<ParentRow>>>(
        "/parent?pageNumber=1&pageSize=200"
      );
      if (data.success) {
        setParents(data.data.items ?? []);
        setError(null);
      } else {
        setError(data.message ?? "Could not load parents.");
      }
    } catch (err: unknown) {
      const status = (err as any)?.response?.status;
      if (status === 404) {
        setError("Parent directory API is not available yet.");
      } else {
        setError(
          (err as any)?.response?.data?.message ?? "Could not load parents."
        );
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadParents();
  }, []);

  /* ----- search ----- */
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return parents;
    return parents.filter((p) => {
      const name = `${p.firstName} ${p.lastName}`.toLowerCase();
      return (
        name.includes(q) ||
        (p.email ?? "").toLowerCase().includes(q) ||
        (p.phoneNumber ?? "").toLowerCase().includes(q)
      );
    });
  }, [parents, search]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="People"
        title="Parents & guardians"
        description={`${parents.length} account${
          parents.length === 1 ? "" : "s"
        } linked to students.`}
        actions={
          <Button icon={UserPlus} onClick={() => setAddOpen(true)}>
            Add parent
          </Button>
        }
      />

      {parents.length > 0 && (
        <Card>
          <div className="flex items-center gap-3">
            <div className="flex-1">
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by name, email or phone…"
              />
            </div>
            <Button
              variant="secondary"
              icon={Search}
              onClick={() => setSearch("")}
              disabled={!search}
            >
              Clear
            </Button>
          </div>
        </Card>
      )}

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
              <Button icon={UserPlus} onClick={() => setAddOpen(true)}>
                Add parent
              </Button>
            }
          />
        ) : filtered.length === 0 ? (
          <EmptyState
            icon={Search}
            title="No matches"
            description={`No parent matches "${search}".`}
            action={
              <Button variant="secondary" onClick={() => setSearch("")}>
                Clear search
              </Button>
            }
          />
        ) : (
          <div className="divide-y divide-surface-100">
            {filtered.map((p) => (
              <div
                key={p.id}
                className="px-6 py-4 flex items-center gap-4 hover:bg-surface-50 transition-colors"
              >
                <Avatar
                  name={`${p.firstName} ${p.lastName}`}
                  src={p.profilePictureUrl}
                  size="md"
                />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="text-[13px] font-semibold text-surface-900">
                      {p.firstName} {p.lastName}
                    </p>
                    {typeof p.childCount === "number" && p.childCount > 0 && (
                      <Badge variant="sky">
                        {p.childCount} child
                        {p.childCount === 1 ? "" : "ren"}
                      </Badge>
                    )}
                  </div>
                  <div className="flex items-center gap-3 mt-0.5 text-[11px] text-surface-500">
                    {p.phoneNumber && (
                      <span className="inline-flex items-center gap-1">
                        <Phone className="w-3 h-3" />
                        {p.phoneNumber}
                      </span>
                    )}
                    {p.email && (
                      <span className="inline-flex items-center gap-1 truncate">
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

      {/* =========================================================
          ADD PARENT MODAL
         ========================================================= */}
      {addOpen && (
        <AddParentModal
          onClose={() => setAddOpen(false)}
          onCreated={async () => {
            setAddOpen(false);
            await loadParents();
          }}
          onError={(msg) => toast({ variant: "error", title: msg })}
          onSuccess={(msg) => toast({ variant: "success", title: msg })}
        />
      )}
    </div>
  );
}

/* =========================================================
   Add-parent modal (self-contained)
   ========================================================= */

interface AddParentModalProps {
  onClose: () => void;
  onCreated: () => void;
  onSuccess: (msg: string) => void;
  onError: (msg: string) => void;
}

function AddParentModal({
  onClose,
  onCreated,
  onSuccess,
  onError,
}: AddParentModalProps) {
  // Data
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [students, setStudents] = useState<StudentRow[]>([]);
  const [loadingData, setLoadingData] = useState(true);

  // Form
  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    relationship: "Guardian",
    isPrimaryContact: true,
  });

  const [selectedClassId, setSelectedClassId] = useState("");
  const [selectedStudentId, setSelectedStudentId] = useState("");

  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  /* ----- load classes + students once ----- */
  useEffect(() => {
    (async () => {
      const [cRes, sRes] = await Promise.allSettled([
        api.get<ApiResponse<SchoolClass[]>>("/schoolclass/all"),
        api.get<ApiResponse<{ items: StudentRow[] }>>(
          "/student?pageNumber=1&pageSize=100"
        ),
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
        setStudents(sRes.value.data.data.items ?? []);
      }

      setLoadingData(false);
    })();
  }, []);

  /* ----- filter students by class ----- */
  const studentsInClass = useMemo(() => {
    if (!selectedClassId) return [];
    const cls = classes.find((c) => c.id === selectedClassId);
    if (!cls) return [];
    return students
      .filter((s) => s.className === cls.name)
      .sort((a, b) => a.fullName.localeCompare(b.fullName));
  }, [students, classes, selectedClassId]);

  const selectedStudent = studentsInClass.find(
    (s) => s.id === selectedStudentId
  );

  /* ----- validate ----- */
  const validate = (): boolean => {
    const e: Record<string, string> = {};
    if (!form.firstName.trim()) e.firstName = "First name is required";
    if (!form.lastName.trim()) e.lastName = "Last name is required";
    if (!form.phoneNumber.trim()) e.phoneNumber = "Phone number is required";
    if (!selectedClassId) e.classId = "Select a class";
    if (!selectedStudentId) e.studentId = "Select a student";
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
      e.email = "Enter a valid email";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  /* ----- submit ----- */
  const handleSubmit = async () => {
    if (!validate()) return;

    const payload: CreateParentPayload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim() || null,
      phoneNumber: form.phoneNumber.trim(),
      studentProfileId: selectedStudentId,
      relationship: form.relationship,
      isPrimaryContact: form.isPrimaryContact,
    };

    setSaving(true);
    try {
      const { data } = await api.post<ApiResponse<{ id: string }>>(
        "/parent/create",
        payload
      );

      if (!data.success) {
        onError(data.message ?? "Failed to create parent");
        return;
      }

      onSuccess("Parent account created");
      onCreated();
    } catch (err: unknown) {
      const msg =
        (err as any)?.response?.data?.message ?? "Could not create parent";
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
        <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden">
          {/* Header */}
          <div className="p-5 flex items-center justify-between border-b border-surface-100 shrink-0">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-brand-50 grid place-items-center">
                <UserPlus className="w-5 h-5 text-brand-600" />
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-surface-900">
                  Add parent or guardian
                </h2>
                <p className="text-[12px] text-surface-500">
                  Create an account and link it to a student.
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
          <div className="flex-1 overflow-y-auto p-5 space-y-5">
            {/* Parent details */}
            <section>
              <h3 className="text-[12px] font-semibold uppercase tracking-wider text-surface-500 mb-3">
                Parent details
              </h3>
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <FormField label="First name" required error={errors.firstName}>
                    <Input
                      value={form.firstName}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, firstName: e.target.value }))
                      }
                      placeholder="e.g. Ngozi"
                    />
                  </FormField>
                  <FormField label="Last name" required error={errors.lastName}>
                    <Input
                      value={form.lastName}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, lastName: e.target.value }))
                      }
                      placeholder="e.g. Okafor"
                    />
                  </FormField>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
                  <FormField label="Email" error={errors.email}>
                    <Input
                      type="email"
                      value={form.email}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, email: e.target.value }))
                      }
                      placeholder="name@example.com"
                    />
                  </FormField>
                </div>
              </div>
            </section>

            {/* Link to student */}
            <section>
              <h3 className="text-[12px] font-semibold uppercase tracking-wider text-surface-500 mb-3">
                Link to which student?
              </h3>
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <FormField
                    label="Class"
                    required
                    error={errors.classId}
                  >
                    <Select
                      value={selectedClassId}
                      onChange={(e) => {
                        setSelectedClassId(e.target.value);
                        setSelectedStudentId("");
                      }}
                      placeholder={
                        loadingData ? "Loading classes…" : "Choose a class"
                      }
                      options={classes.map((c) => ({
                        value: c.id,
                        label: c.name,
                      }))}
                    />
                  </FormField>

                  <FormField
                    label="Student"
                    required
                    error={errors.studentId}
                    hint={
                      !selectedClassId
                        ? "Select a class first."
                        : studentsInClass.length === 0
                        ? "No students in this class."
                        : `${studentsInClass.length} available.`
                    }
                  >
                    <Select
                      value={selectedStudentId}
                      onChange={(e) => setSelectedStudentId(e.target.value)}
                      placeholder={
                        !selectedClassId
                          ? "Choose a class first"
                          : studentsInClass.length === 0
                          ? "No students"
                          : "Choose a student"
                      }
                      disabled={
                        !selectedClassId || studentsInClass.length === 0
                      }
                      options={studentsInClass.map((s) => ({
                        value: s.id,
                        label: `${s.fullName} — ${s.admissionNumber}`,
                      }))}
                    />
                  </FormField>
                </div>

                {selectedStudent && (
                  <div className="flex items-center gap-3 p-3 rounded-xl bg-surface-50 border border-surface-100">
                    <Avatar
                      name={selectedStudent.fullName}
                      src={selectedStudent.profilePictureUrl}
                      size="md"
                    />
                    <div className="flex-1 min-w-0">
                      <p className="text-[13px] font-medium text-surface-900">
                        {selectedStudent.fullName}
                      </p>
                      <p className="text-[11px] text-surface-500 font-mono">
                        {selectedStudent.admissionNumber} ·{" "}
                        {selectedStudent.className}
                      </p>
                    </div>
                  </div>
                )}

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <FormField label="Relationship to student" required>
                    <Select
                      value={form.relationship}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, relationship: e.target.value }))
                      }
                      options={RELATIONSHIPS.map((r) => ({
                        value: r,
                        label: r,
                      }))}
                    />
                  </FormField>

                  <div className="flex items-end pb-2">
                    <label className="inline-flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={form.isPrimaryContact}
                        onChange={(e) =>
                          setForm((f) => ({
                            ...f,
                            isPrimaryContact: e.target.checked,
                          }))
                        }
                        className="w-4 h-4 rounded"
                      />
                      <span className="text-[13px] text-surface-700">
                        Set as primary contact
                      </span>
                    </label>
                  </div>
                </div>
              </div>
            </section>

            {classes.length === 0 && !loadingData && (
              <div className="p-4 rounded-xl bg-amber-50 border border-amber-100 text-[12px] text-amber-800">
                No classes exist yet. Add classes before linking a parent to a
                student.
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="p-5 border-t border-surface-100 flex justify-end gap-3 shrink-0">
            <Button variant="secondary" onClick={onClose} disabled={saving}>
              Cancel
            </Button>
            <Button
              icon={UserPlus}
              loading={saving}
              onClick={handleSubmit}
              disabled={classes.length === 0}
            >
              Create parent
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
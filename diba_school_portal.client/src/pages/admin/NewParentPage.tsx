import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { UserPlus, ArrowLeft } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, FormField, Input, Select, Skeleton,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

interface StudentRow {
  id: string;
  admissionNumber: string;
  fullName: string;
  className: string;
  profilePictureUrl: string | null;
}

interface SchoolClass {
  id: string;
  name: string;
  academicSection: string;
  displayOrder?: number;
}

interface LinkResponse {
  userId: string;
  email: string | null;
  phoneNumber: string;
  temporaryPassword: string;
}

// Order sections for the class picker
const SECTION_ORDER: Record<string, number> = {
  PreNursery: 0,
  Nursery: 1,
  Primary: 2,
  JuniorSecondary: 3,
  SeniorSecondary: 4,
};

function classRank(c: SchoolClass): number {
  const sec = SECTION_ORDER[c.academicSection] ?? 99;
  const match = c.name.match(/(\d+)/);
  const num = match ? parseInt(match[1], 10) : 0;
  return sec * 1000 + num;
}

export default function NewParentPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [students, setStudents] = useState<StudentRow[]>([]);
  const [loadingRefs, setLoadingRefs] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [selectedClassId, setSelectedClassId] = useState("");
  const [selectedStudentId, setSelectedStudentId] = useState("");

  const [form, setForm] = useState({
    parentFirstName: "",
    parentLastName: "",
    parentPhone: "",
    parentEmail: "",
    relationship: "Guardian",
    isPrimaryContact: true,
  });
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<LinkResponse | null>(null);

  // ---- Load classes + students in parallel ----
  useEffect(() => {
    (async () => {
      try {
        const [classRes, studentRes] = await Promise.allSettled([
          api.get<ApiResponse<SchoolClass[]>>("/schoolclass/all"),
          api.get<ApiResponse<Paginated<StudentRow>>>(
            "/student?pageNumber=1&pageSize=200"
          ),
        ]);

        if (classRes.status === "fulfilled" && classRes.value.data.success) {
          const sorted = [...(classRes.value.data.data ?? [])].sort(
            (a, b) => classRank(a) - classRank(b)
          );
          setClasses(sorted);
        }

        if (studentRes.status === "fulfilled" && studentRes.value.data.success) {
          setStudents(studentRes.value.data.data.items ?? []);
        } else {
          setLoadError("Could not load students.");
        }
      } catch (err: any) {
        setLoadError(
          err?.response?.data?.message ?? "Could not load form data."
        );
      } finally {
        setLoadingRefs(false);
      }
    })();
  }, []);

  // ---- Students filtered by class ----
  const studentsInClass = useMemo(() => {
    if (!selectedClassId) return [];
    const cls = classes.find((c) => c.id === selectedClassId);
    if (!cls) return [];
    return students
      .filter((s) => s.className === cls.name)
      .sort((a, b) => a.fullName.localeCompare(b.fullName));
  }, [students, classes, selectedClassId]);

  const change = (k: keyof typeof form) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm((p) => ({ ...p, [k]: e.target.value }));

  const submit = async (e: FormEvent) => {
    e.preventDefault();

    if (!selectedStudentId) {
      toast({ variant: "error", title: "Pick a student to link" });
      return;
    }
    if (!form.parentFirstName || !form.parentLastName || !form.parentPhone) {
      toast({ variant: "error", title: "Name and phone are required" });
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<LinkResponse>>(
        "/auth/link-parent",
        {
          studentProfileId: selectedStudentId,
          parentFirstName: form.parentFirstName.trim(),
          parentLastName: form.parentLastName.trim(),
          parentPhone: form.parentPhone.trim(),
          parentEmail: form.parentEmail.trim() || null,
          relationship: form.relationship,
          isPrimaryContact: form.isPrimaryContact,
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }

      setCreated(data.data);
      toast({ variant: "success", title: "Parent created and linked" });
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not link parent",
      });
    } finally {
      setSubmitting(false);
    }
  };

  // ---- Success screen ----
  if (created) {
    return (
      <div className="max-w-2xl mx-auto space-y-6">
        <PageHeader eyebrow="Complete" title="Parent added" />

        <Card>
          <div className="text-center py-4">
            <div className="w-14 h-14 rounded-2xl bg-brand-50 grid place-items-center mx-auto mb-4">
              <UserPlus className="w-6 h-6 text-brand-600" />
            </div>
            <h2 className="text-[16px] font-semibold text-surface-900">
              Parent account ready
            </h2>
            <p className="text-[13px] text-surface-500 mt-1">
              Share these credentials with the parent. They can log in at{" "}
              <code className="font-mono">/auth/parent</code>.
            </p>
          </div>

          <div className="mt-6 space-y-3">
            <Row label="Phone (login)" value={created.phoneNumber} />
            {created.email && <Row label="Email" value={created.email} />}
            <Row label="Temporary password" value={created.temporaryPassword} />
          </div>

          <div className="mt-6 flex gap-3">
            <Button
              variant="secondary"
              fullWidth
              onClick={() => {
                setCreated(null);
                setSelectedClassId("");
                setSelectedStudentId("");
                setForm({
                  parentFirstName: "",
                  parentLastName: "",
                  parentPhone: "",
                  parentEmail: "",
                  relationship: "Guardian",
                  isPrimaryContact: true,
                });
              }}
            >
              Add another
            </Button>
            <Button fullWidth onClick={() => navigate("/admin/parents")}>
              View parents
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  // ---- Form ----
  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/parents")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All parents
      </button>

      <PageHeader
        eyebrow="People"
        title="Add parent or guardian"
        description="Create a parent account and link them to a student."
      />

      <form onSubmit={submit} className="space-y-6">
        {/* ---------- Link to which student ---------- */}
        <Card>
          <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
            Link to which student?
          </h3>

          {loadError && (
            <div className="mb-4 p-3 rounded-xl bg-flame-50 border border-flame-100 text-[12px] text-flame-800">
              {loadError}
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField
              label="Class"
              required
              hint="Pick the class first"
            >
              {loadingRefs ? (
                <Skeleton className="h-11" />
              ) : (
                <Select
                  value={selectedClassId}
                  onChange={(e) => {
                    setSelectedClassId(e.target.value);
                    setSelectedStudentId("");
                  }}
                  placeholder="Choose a class"
                  options={classes.map((c) => ({
                    value: c.id,
                    label: c.name,
                  }))}
                />
              )}
            </FormField>

            <FormField
              label="Student"
              required
              hint={
                !selectedClassId
                  ? "Select a class first."
                  : studentsInClass.length === 0
                  ? "No students in this class."
                  : `${studentsInClass.length} available.`
              }
            >
              {loadingRefs ? (
                <Skeleton className="h-11" />
              ) : (
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
                  disabled={!selectedClassId || studentsInClass.length === 0}
                  options={studentsInClass.map((s) => ({
                    value: s.id,
                    label: `${s.fullName} — ${s.admissionNumber}`,
                  }))}
                />
              )}
            </FormField>
          </div>
        </Card>

        {/* ---------- Parent details ---------- */}
        <Card>
          <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
            Parent details
          </h3>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="First name" required>
              <Input
                value={form.parentFirstName}
                onChange={change("parentFirstName")}
              />
            </FormField>
            <FormField label="Last name" required>
              <Input
                value={form.parentLastName}
                onChange={change("parentLastName")}
              />
            </FormField>
            <FormField
              label="Phone number"
              required
              hint="This becomes their login username"
            >
              <Input
                value={form.parentPhone}
                onChange={change("parentPhone")}
                placeholder="080..."
              />
            </FormField>
            <FormField label="Email" hint="Optional">
              <Input
                type="email"
                value={form.parentEmail}
                onChange={change("parentEmail")}
              />
            </FormField>
            <FormField label="Relationship" required>
              <Select
                value={form.relationship}
                onChange={change("relationship")}
                options={[
                  { value: "Father", label: "Father" },
                  { value: "Mother", label: "Mother" },
                  { value: "Guardian", label: "Guardian" },
                  { value: "Uncle", label: "Uncle" },
                  { value: "Aunt", label: "Aunt" },
                  { value: "Grandparent", label: "Grandparent" },
                  { value: "Sibling", label: "Sibling" },
                  { value: "Other", label: "Other" },
                ]}
              />
            </FormField>
            <div className="flex items-center gap-2 pt-6">
              <input
                type="checkbox"
                id="primary"
                checked={form.isPrimaryContact}
                onChange={(e) =>
                  setForm((p) => ({ ...p, isPrimaryContact: e.target.checked }))
                }
                className="w-4 h-4 rounded"
              />
              <label htmlFor="primary" className="text-[13px] text-surface-700">
                Primary contact
              </label>
            </div>
          </div>
        </Card>

        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate("/admin/parents")}
          >
            Cancel
          </Button>
          <Button type="submit" icon={UserPlus} loading={submitting}>
            Create & link parent
          </Button>
        </div>
      </form>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between p-3.5 rounded-xl bg-surface-50 border border-surface-100">
      <span className="text-[12px] text-surface-500">{label}</span>
      <span className="text-[13px] font-mono font-semibold text-surface-900">
        {value}
      </span>
    </div>
  );
}
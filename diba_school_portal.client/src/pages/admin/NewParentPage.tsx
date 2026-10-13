import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { UserPlus, ArrowLeft } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, FormField, Input, Select, Avatar,
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

interface LinkResponse {
  userId: string;
  email: string | null;
  phoneNumber: string;
  temporaryPassword: string;
}

export default function NewParentPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [students, setStudents] = useState<StudentRow[]>([]);
  const [form, setForm] = useState({
    studentProfileId: "",
    parentFirstName: "",
    parentLastName: "",
    parentPhone: "",
    parentEmail: "",
    relationship: "Father",
    isPrimaryContact: true,
  });
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<LinkResponse | null>(null);

  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<Paginated<StudentRow>>>(
        "/student?pageNumber=1&pageSize=100"
      );
      if (data.success) setStudents(data.data.items);
    })();
  }, []);

  const change = (k: keyof typeof form) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm((p) => ({ ...p, [k]: e.target.value }));

  const submit = async (e: FormEvent) => {
    e.preventDefault();

    if (!form.studentProfileId) {
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
          studentProfileId: form.studentProfileId,
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
              Share these credentials with the parent. They can log in
              at <code className="font-mono">/auth/parent</code>.
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
                setForm({
                  studentProfileId: "",
                  parentFirstName: "",
                  parentLastName: "",
                  parentPhone: "",
                  parentEmail: "",
                  relationship: "Father",
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
        <Card>
          <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
            Link to which student?
          </h3>
          <FormField label="Student" required>
            <Select
              value={form.studentProfileId}
              onChange={change("studentProfileId")}
              placeholder="Choose a student"
              options={students.map((s) => ({
                value: s.id,
                label: `${s.fullName} - ${s.admissionNumber} (${s.className})`,
              }))}
            />
          </FormField>
        </Card>

        <Card>
          <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
            Parent details
          </h3>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="First name" required>
              <Input value={form.parentFirstName} onChange={change("parentFirstName")} />
            </FormField>
            <FormField label="Last name" required>
              <Input value={form.parentLastName} onChange={change("parentLastName")} />
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
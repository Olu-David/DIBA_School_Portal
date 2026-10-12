import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { UserPlus, Camera, X } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, FormField, Input, Select, Avatar,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface SchoolClass {
  id: string;
  name: string;
  academicSection: string;
}
interface Created {
  userId: string;
  admissionNumber: string;
  temporaryPassword: string;
}

// Only these three are valid SS streams — matches ClassStream enum.
// We never send "None" or "General" from the UI.
const STREAMS = ["Science", "Arts", "Commercial"];

export default function NewStudentPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<Created | null>(null);

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    schoolClassId: "",
    stream: "",
  });

  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      const { data } = await api.get<ApiResponse<SchoolClass[]>>(
        "/schoolclass/all"
      );
      if (data.success) setClasses(data.data);
    })();
  }, []);

  const onFile = (e: ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    setFile(f);
    setPreview(URL.createObjectURL(f));
  };

  const change =
    (k: keyof typeof form) =>
    (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm((p) => ({ ...p, [k]: e.target.value }));

  const selectedClass = classes.find((c) => c.id === form.schoolClassId);
  const isSenior = selectedClass?.academicSection === "SeniorSecondary";

  const submit = async (e: FormEvent) => {
    e.preventDefault();

    if (!form.firstName || !form.lastName || !form.schoolClassId) {
      toast({ variant: "error", title: "Please fill required fields" });
      return;
    }

    if (isSenior && !form.stream) {
      toast({ variant: "error", title: "Please choose a stream for this SS student" });
      return;
    }

    setSubmitting(true);
    try {
      const fd = new FormData();
      fd.append("role", "Student");
      fd.append("firstName", form.firstName);
      fd.append("lastName", form.lastName);
      if (form.email) fd.append("email", form.email);
      if (form.phoneNumber) fd.append("phoneNumber", form.phoneNumber);
      fd.append("schoolClassId", form.schoolClassId);

      // Only send stream for Senior Secondary
      if (isSenior && form.stream) {
        fd.append("stream", form.stream);
      }

      if (file) fd.append("profilePicture", file);

      const { data } = await api.post<ApiResponse<Created>>(
        "/auth/create-account",
        fd,
        { headers: { "Content-Type": "multipart/form-data" } }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }

      setCreated(data.data);
      toast({ variant: "success", title: "Student enrolled" });
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not enrol student",
      });
    } finally {
      setSubmitting(false);
    }
  };

  if (created) {
    return (
      <div className="max-w-2xl mx-auto">
        <PageHeader eyebrow="Enrolment complete" title="Student added" />
        <Card>
          <div className="text-center py-4">
            <div className="w-14 h-14 rounded-2xl bg-emerald-50 grid place-items-center mx-auto mb-4">
              <UserPlus className="w-6 h-6 text-emerald-600" />
            </div>
            <h2 className="text-[16px] font-semibold text-slate-900">
              {form.firstName} {form.lastName} enrolled
            </h2>
            <p className="text-[13px] text-slate-500 mt-1">
              Share these credentials with the guardian. You can link a parent
              from the student's page.
            </p>
          </div>

          <div className="mt-6 space-y-3">
            <CredRow label="Admission number" value={created.admissionNumber} />
            <CredRow
              label="Temporary password"
              value={created.temporaryPassword}
            />
          </div>

          <div className="mt-6 flex gap-3">
            <Button variant="secondary" fullWidth onClick={() => setCreated(null)}>
              Enrol another
            </Button>
            <Button
              fullWidth
              onClick={() => navigate(`/admin/students/${created.userId}`)}
            >
              View student
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <PageHeader
        eyebrow="Enrolment"
        title="Enrol new student"
        description="Create the student account and set their class. Guardians can be linked afterwards."
      />

      <form onSubmit={submit} className="space-y-6">
        <Card>
          <h3 className="text-[15px] font-semibold text-slate-900 mb-5">
            Student details
          </h3>

          <div className="flex items-start gap-5 mb-6">
            <div className="relative">
              {preview ? (
                <img
                  src={preview}
                  alt="preview"
                  className="w-20 h-20 rounded-2xl object-cover border border-slate-200"
                />
              ) : (
                <Avatar name={`${form.firstName} ${form.lastName}`} size="xl" />
              )}
              <label className="absolute -bottom-1 -right-1 w-7 h-7 rounded-full bg-emerald-600 text-white grid place-items-center cursor-pointer shadow-sm hover:bg-emerald-700">
                <Camera className="w-3.5 h-3.5" />
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  className="hidden"
                  onChange={onFile}
                />
              </label>
            </div>
            <div className="text-[12px] text-slate-400 pt-1">
              <p className="font-medium text-slate-600">Profile picture</p>
              <p className="mt-1">JPG, PNG, or WEBP · max 200 KB</p>
              {file && (
                <button
                  type="button"
                  onClick={() => {
                    setFile(null);
                    setPreview(null);
                  }}
                  className="mt-2 inline-flex items-center gap-1 text-rose-600 hover:text-rose-700"
                >
                  <X className="w-3 h-3" /> Remove
                </button>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="First name" required>
              <Input
                value={form.firstName}
                onChange={change("firstName")}
                placeholder="Amina"
              />
            </FormField>
            <FormField label="Last name" required>
              <Input
                value={form.lastName}
                onChange={change("lastName")}
                placeholder="Bello"
              />
            </FormField>
            <FormField label="Email" hint="Optional for students">
              <Input
                type="email"
                value={form.email}
                onChange={change("email")}
                placeholder="student@example.com"
              />
            </FormField>
            <FormField label="Phone">
              <Input
                value={form.phoneNumber}
                onChange={change("phoneNumber")}
                placeholder="080…"
              />
            </FormField>
          </div>
        </Card>

        <Card>
          <h3 className="text-[15px] font-semibold text-slate-900 mb-5">
            Class placement
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Class" required>
              <Select
                value={form.schoolClassId}
                onChange={(e) => {
                  const id = e.target.value;
                  // Reset stream whenever the class changes
                  setForm((p) => ({ ...p, schoolClassId: id, stream: "" }));
                }}
                placeholder="Choose a class"
                options={classes.map((c) => ({ value: c.id, label: c.name }))}
              />
            </FormField>

            {/* Stream picker only shows for Senior Secondary */}
            {isSenior && (
              <FormField
                label="Stream"
                required
                hint="Required for Senior Secondary students"
              >
                <Select
                  value={form.stream}
                  onChange={change("stream")}
                  placeholder="Choose a stream"
                  options={STREAMS.map((s) => ({ value: s, label: s }))}
                />
              </FormField>
            )}
          </div>
        </Card>

        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate("/admin/students")}
          >
            Cancel
          </Button>
          <Button type="submit" icon={UserPlus} loading={submitting}>
            Enrol student
          </Button>
        </div>
      </form>
    </div>
  );
}

function CredRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between p-3.5 rounded-xl bg-slate-50 border border-slate-100">
      <span className="text-[12px] text-slate-500">{label}</span>
      <span className="text-[13px] font-mono font-semibold text-slate-900">
        {value}
      </span>
    </div>
  );
}
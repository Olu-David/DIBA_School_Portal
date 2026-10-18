import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { UserPlus, Sparkles, Camera, X, Loader2 } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button,
  Card,
  FormField,
  Input,
  Select,
  Avatar,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

// ==========================================================
// Types
// ==========================================================
interface SchoolClass {
  id: string;
  name: string;
  academicSection: string;
}

interface Created {
  userId: string;
  admissionNumber: string;
  temporaryPassword: string;
  studentProfileId?: string | null;
  profilePictureUrl?: string | null;
}

const STREAMS = ["Science", "Arts", "Commercial"] as const;

// Detects a stream from a class name like "SS 1 Science" or "SS-1-ARTS"
function detectStream(className: string | undefined): string | null {
  if (!className) return null;
  const lower = className.toLowerCase();
  if (lower.includes("science")) return "Science";
  if (lower.includes("commercial")) return "Commercial";
  if (lower.includes("arts")) return "Arts";
  return null;
}

// ==========================================================
// Component
// ==========================================================
export default function NewStudentPage() {
  const navigate = useNavigate();
  const { toast } = useToast();

  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [created, setCreated] = useState<Created | null>(null);

  // ---- Photo state ----
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [photoFile, setPhotoFile] = useState<File | null>(null);
  const [photoPreview, setPhotoPreview] = useState<string | null>(null);

  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phoneNumber: "",
    schoolClassId: "",
    stream: "",
  });

  // ---- Load classes ----
  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<SchoolClass[]>>(
          "/schoolclass/all"
        );
        if (data.success) setClasses(data.data);
      } catch {
        toast({ variant: "error", title: "Could not load classes" });
      }
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const change =
    (k: keyof typeof form) =>
    (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm((p) => ({ ...p, [k]: e.target.value }));

  const selectedClass = classes.find((c) => c.id === form.schoolClassId);
  const isSenior = selectedClass?.academicSection === "SeniorSecondary";
  const autoDetectedStream = detectStream(selectedClass?.name);
  const needsManualStream = isSenior && !autoDetectedStream;

  const handleClassChange = (id: string) => {
    const cls = classes.find((c) => c.id === id);
    const detected = detectStream(cls?.name);
    setForm((p) => ({ ...p, schoolClassId: id, stream: detected ?? "" }));
  };

  // ==========================================================
  // Photo picker handlers
  // ==========================================================
  const pickPhoto = () => fileInputRef.current?.click();

  const clearPhoto = () => {
    setPhotoFile(null);
    if (photoPreview) URL.revokeObjectURL(photoPreview);
    setPhotoPreview(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handlePhotoChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 200 * 1024) {
      toast({ variant: "error", title: "Photo must be under 200 KB." });
      return;
    }
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      toast({ variant: "error", title: "Only JPG, PNG, or WEBP are allowed." });
      return;
    }

    // Clean previous preview
    if (photoPreview) URL.revokeObjectURL(photoPreview);

    setPhotoFile(file);
    setPhotoPreview(URL.createObjectURL(file));
  };

  // ==========================================================
  // Submit — multipart/form-data (includes optional photo)
  // ==========================================================
  const submit = async (e: FormEvent) => {
    e.preventDefault();

    // ---- Client-side guards ----
    if (!form.firstName.trim() || !form.lastName.trim() || !form.schoolClassId) {
      toast({ variant: "error", title: "Please fill required fields" });
      return;
    }

    if (isSenior && !form.stream) {
      toast({
        variant: "error",
        title: "Please choose a stream for this SS student",
      });
      return;
    }

    setSubmitting(true);
    try {
      const fd = new FormData();

      // Required
      fd.append("role", "Student");
      fd.append("firstName", form.firstName.trim());
      fd.append("lastName", form.lastName.trim());
      fd.append("schoolClassId", form.schoolClassId);

      // Optional strings — only append when non-empty
      const email = form.email.trim();
      const phone = form.phoneNumber.trim();
      if (email) fd.append("email", email);
      if (phone) fd.append("phoneNumber", phone);

      // Stream — only for Senior Secondary with a chosen value
      if (isSenior && form.stream) fd.append("stream", form.stream);

      // Photo — only if the admin picked one
      if (photoFile) fd.append("profilePicture", photoFile);

      // ⚠️ NO headers argument — browser sets multipart boundary automatically
      const { data } = await api.post<ApiResponse<Created>>(
        "/auth/create-account",
        fd
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }

      setCreated(data.data);
      toast({ variant: "success", title: "Student enrolled" });
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
      };
      const status = e2?.response?.status;
      const message =
        e2?.response?.data?.message ??
        (status === 415
          ? "Server rejected content type — FormData is not being sent correctly."
          : status === 403
          ? "You don't have permission to enrol students."
          : status === 409
          ? "A user with this email already exists."
          : status === 400
          ? "Invalid student data. Check fields."
          : "Could not enrol student");

      toast({ variant: "error", title: message });
    } finally {
      setSubmitting(false);
    }
  };

  // ==========================================================
  // Success screen
  // ==========================================================
  if (created) {
    return (
      <div className="max-w-2xl mx-auto">
        <PageHeader eyebrow="Enrolment complete" title="Student added" />
        <Card>
          <div className="text-center py-4">
            {created.profilePictureUrl ? (
              <img
                src={created.profilePictureUrl}
                alt={`${form.firstName} ${form.lastName}`}
                className="w-14 h-14 rounded-full object-cover mx-auto mb-4 ring-2 ring-emerald-100"
              />
            ) : (
              <div className="w-14 h-14 rounded-2xl bg-brand-50 grid place-items-center mx-auto mb-4">
                <UserPlus className="w-6 h-6 text-brand-600" />
              </div>
            )}
            <h2 className="text-[16px] font-semibold text-surface-900">
              {form.firstName} {form.lastName} enrolled
            </h2>
            <p className="text-[13px] text-surface-500 mt-1">
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
            <Button
              variant="secondary"
              fullWidth
              onClick={() => {
                setCreated(null);
                clearPhoto();
                setForm({
                  firstName: "",
                  lastName: "",
                  email: "",
                  phoneNumber: "",
                  schoolClassId: "",
                  stream: "",
                });
              }}
            >
              Enrol another
            </Button>
            <Button
              fullWidth
              onClick={() =>
                navigate(
                  `/admin/students/${
                    created.studentProfileId ?? created.userId
                  }`
                )
              }
            >
              View student
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  // ==========================================================
  // Form
  // ==========================================================
  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <PageHeader
        eyebrow="Enrolment"
        title="Enrol new student"
        description="Create the student account and set their class. Guardians can be linked afterwards."
      />

      <form onSubmit={submit} className="space-y-6">
        <Card>
          <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
            Student details
          </h3>

          {/* ---------- Photo picker ---------- */}
          <div className="flex items-start gap-5 mb-6">
            {/* Clickable avatar */}
            <div className="relative shrink-0">
              <button
                type="button"
                onClick={pickPhoto}
                disabled={submitting}
                className="relative group rounded-full focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 disabled:cursor-wait"
                title="Add student photo"
                aria-label="Add student photo"
              >
                {photoPreview ? (
                  <img
                    src={photoPreview}
                    alt="Student photo preview"
                    className="w-16 h-16 rounded-full object-cover"
                  />
                ) : (
                  <Avatar name={`${form.firstName} ${form.lastName}`} size="xl" />
                )}

                {/* Camera badge */}
                <div className="absolute bottom-0 right-0 z-10 w-8 h-8 rounded-full bg-emerald-600 text-white ring-2 ring-white shadow-md grid place-items-center transition-transform group-hover:scale-110">
                  {submitting ? (
                    <Loader2 className="w-4 h-4 animate-spin" />
                  ) : (
                    <Camera className="w-4 h-4" />
                  )}
                </div>
              </button>

              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={handlePhotoChange}
              />
            </div>

            {/* Help / remove text */}
            <div className="text-[12px] pt-1 flex-1 min-w-0">
              <p className="font-medium text-surface-700">
                Profile picture (optional)
              </p>
              <p className="mt-1 text-surface-500 leading-relaxed">
                JPG, PNG, or WEBP · max 200 KB
              </p>
              {photoFile && (
                <button
                  type="button"
                  onClick={clearPhoto}
                  className="mt-2 inline-flex items-center gap-1 text-[11px] font-medium text-rose-600 hover:text-rose-700"
                >
                  <X className="w-3 h-3" />
                  Remove photo
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
                placeholder="080..."
              />
            </FormField>
          </div>
        </Card>

        <Card>
          <h3 className="text-[15px] font-semibold text-surface-900 mb-5">
            Class placement
          </h3>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="Class" required>
              <Select
                value={form.schoolClassId}
                onChange={(e) => handleClassChange(e.target.value)}
                placeholder="Choose a class"
                options={classes.map((c) => ({
                  value: c.id,
                  label: c.name,
                }))}
              />
            </FormField>

            {isSenior && autoDetectedStream && (
              <FormField label="Stream" hint="Detected from the class name">
                <div className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-brand-50 border border-brand-200 text-brand-700 text-[14px] font-medium">
                  <Sparkles className="w-4 h-4 shrink-0" />
                  {autoDetectedStream}
                </div>
              </FormField>
            )}

            {needsManualStream && (
              <FormField
                label="Stream"
                required
                hint="This class is not stream-specific — pick the student's stream"
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

          {isSenior && (
            <p className="mt-4 text-[12px] text-surface-500 leading-relaxed">
              Tip: create separate classes for each stream (e.g.{" "}
              <strong className="text-surface-700">SS 1 Science</strong>,{" "}
              <strong className="text-surface-700">SS 1 Arts</strong>,{" "}
              <strong className="text-surface-700">SS 1 Commercial</strong>) and
              the stream will be set automatically.
            </p>
          )}
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

// ==========================================================
// Small helper
// ==========================================================
function CredRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between p-3.5 rounded-xl bg-surface-50 border border-surface-100">
      <span className="text-[12px] text-surface-500">{label}</span>
      <span className="text-[13px] font-mono font-semibold text-surface-900">
        {value}
      </span>
    </div>
  );
}
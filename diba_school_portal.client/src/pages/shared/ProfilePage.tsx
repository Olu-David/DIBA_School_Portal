import { useRef, useState, type ChangeEvent, type FormEvent } from "react";
import {
  Lock, Save, Camera, Pencil, X, Loader2, CheckCircle2,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, FormField, Input, Avatar, Badge,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useAuth } from "../../hooks/useAuth";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface UpdateProfileResponse {
  id: string;
  email: string | null;
  firstName: string;
  lastName: string;
  profilePictureUrl: string | null;
  role: string;
  classId: string | null;
}

export default function ProfilePage() {
  const { user, updateUser } = useAuth();
  const { toast } = useToast();

  // ==========================================================
  // Profile edit state
  // ==========================================================
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [profileForm, setProfileForm] = useState({
    firstName: user?.firstName ?? "",
    lastName: user?.lastName ?? "",
    phoneNumber: "",
  });

  // ==========================================================
  // Profile picture state
  // ==========================================================
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [uploading, setUploading] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  // ==========================================================
  // Password change state
  // ==========================================================
  const [pw, setPw] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [pwSubmitting, setPwSubmitting] = useState(false);

  // ==========================================================
  // Profile edit handlers
  // ==========================================================
  const startEditing = () => {
    setProfileForm({
      firstName: user?.firstName ?? "",
      lastName: user?.lastName ?? "",
      phoneNumber: "",
    });
    setEditing(true);
  };

  const cancelEditing = () => setEditing(false);

  const submitProfile = async (e: FormEvent) => {
    e.preventDefault();

    if (!profileForm.firstName.trim() || !profileForm.lastName.trim()) {
      toast({ variant: "error", title: "First and last name are required." });
      return;
    }

    setSaving(true);
    try {
      const { data } = await api.put<ApiResponse<UpdateProfileResponse>>(
        "/auth/profile",
        {
          firstName: profileForm.firstName.trim(),
          lastName: profileForm.lastName.trim(),
          phoneNumber: profileForm.phoneNumber.trim() || null,
        }
      );

      if (!data.success || !data.data) {
        toast({ variant: "error", title: data.message ?? "Update failed." });
        return;
      }

      updateUser({
        firstName: data.data.firstName,
        lastName: data.data.lastName,
      });

      toast({ variant: "success", title: "Profile updated" });
      setEditing(false);
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      toast({
        variant: "error",
        title:
          e2?.response?.data?.message ??
          e2?.message ??
          "Update failed.",
      });
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // Profile picture handlers
  // ==========================================================
  const pickFile = () => fileInputRef.current?.click();

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 200 * 1024) {
      toast({ variant: "error", title: "Picture must be under 200 KB." });
      return;
    }
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      toast({ variant: "error", title: "Only JPG, PNG, or WEBP are allowed." });
      return;
    }

    const previewLocal = URL.createObjectURL(file);
    setPreviewUrl(previewLocal);
    setUploading(true);

    try {
      const fd = new FormData();
      fd.append("picture", file);

      const { data } = await api.post<ApiResponse<string>>(
        "/auth/profile-picture",
        fd
      );

      if (!data.success || !data.data) {
        toast({ variant: "error", title: data.message ?? "Upload failed." });
        setPreviewUrl(null);
        return;
      }

      updateUser({ profilePictureUrl: data.data });
      toast({ variant: "success", title: "Profile picture updated" });
    } catch (err: unknown) {
      const e2 = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Upload failed.",
      });
      setPreviewUrl(null);
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
      URL.revokeObjectURL(previewLocal);
    }
  };

  // ==========================================================
  // Password change handler — WITH catch + client validation
  // ==========================================================
  const submitPw = async (e: FormEvent) => {
    e.preventDefault();

    // ---------- Client-side validation (instant feedback) ----------
    if (!pw.currentPassword) {
      toast({ variant: "error", title: "Enter your current password." });
      return;
    }
    if (pw.newPassword.length < 8) {
      toast({ variant: "error", title: "New password must be at least 8 characters." });
      return;
    }
    if (!/[A-Z]/.test(pw.newPassword)) {
      toast({ variant: "error", title: "New password must contain an uppercase letter." });
      return;
    }
    if (!/[a-z]/.test(pw.newPassword)) {
      toast({ variant: "error", title: "New password must contain a lowercase letter." });
      return;
    }
    if (!/[0-9]/.test(pw.newPassword)) {
      toast({ variant: "error", title: "New password must contain a number." });
      return;
    }
    if (!/[^A-Za-z0-9]/.test(pw.newPassword)) {
      toast({
        variant: "error",
        title: "New password must contain a symbol (e.g. !@#$%).",
      });
      return;
    }
    if (pw.newPassword !== pw.confirmNewPassword) {
      toast({ variant: "error", title: "New passwords do not match." });
      return;
    }

    // ---------- Server-side (final authority) ----------
    setPwSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<boolean>>(
        "/auth/change-password",
        pw
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }

      toast({ variant: "success", title: "Password updated" });
      setPw({ currentPassword: "", newPassword: "", confirmNewPassword: "" });
    } catch (err: unknown) {
      // ⚠️ THIS catch is what was missing. Backend returns 400 for
      //     wrong current password or policy violations.
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      const message = e2?.response?.data?.message;

      toast({
        variant: "error",
        title:
          message ??
          (status === 400
            ? "Password change failed. Check your current password."
            : status === 401
            ? "Session expired — please log in again."
            : "Could not change password."),
      });
    } finally {
      setPwSubmitting(false);
    }
  };

  const displayPicture = previewUrl ?? user?.profilePictureUrl ?? null;

  // ==========================================================
  // Render
  // ==========================================================
  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <PageHeader eyebrow="Account" title="My profile" />

      {/* ===============================================
          IDENTITY CARD
          =============================================== */}
      <Card>
        <div className="flex items-start gap-5">
          <div className="relative shrink-0">
            <Avatar
              name={`${user?.firstName} ${user?.lastName}`}
              src={displayPicture ?? undefined}
              size="xl"
            />
            <button
              type="button"
              onClick={pickFile}
              disabled={uploading}
              className="absolute -bottom-1 -right-1 w-7 h-7 rounded-full bg-gold-600 text-white grid place-items-center shadow-sm hover:bg-gold-700 disabled:opacity-60 disabled:cursor-wait"
              title="Change profile picture"
            >
              {uploading ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <Camera className="w-3.5 h-3.5" />
              )}
            </button>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={handleFileChange}
            />
          </div>

          <div className="flex-1 min-w-0">
            {!editing ? (
              <>
                <h2 className="text-[16px] font-semibold text-slate-900">
                  {user?.firstName} {user?.lastName}
                </h2>
                <p className="text-[12px] text-slate-400 mt-0.5">
                  {user?.email ?? "-"}
                </p>
                <div className="mt-2 flex items-center gap-2">
                  <Badge variant="emerald">{user?.role}</Badge>
                  <button
                    type="button"
                    onClick={startEditing}
                    className="text-[12px] font-medium text-brand-600 hover:text-brand-700 inline-flex items-center gap-1"
                  >
                    <Pencil className="w-3 h-3" />
                    Edit
                  </button>
                </div>
              </>
            ) : (
              <form onSubmit={submitProfile} className="space-y-3">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <FormField label="First name" required>
                    <Input
                      value={profileForm.firstName}
                      onChange={(e) =>
                        setProfileForm((p) => ({
                          ...p,
                          firstName: e.target.value,
                        }))
                      }
                    />
                  </FormField>
                  <FormField label="Last name" required>
                    <Input
                      value={profileForm.lastName}
                      onChange={(e) =>
                        setProfileForm((p) => ({
                          ...p,
                          lastName: e.target.value,
                        }))
                      }
                    />
                  </FormField>
                </div>
                <FormField
                  label="Phone number"
                  hint="Optional — used for SMS notifications"
                >
                  <Input
                    value={profileForm.phoneNumber}
                    onChange={(e) =>
                      setProfileForm((p) => ({
                        ...p,
                        phoneNumber: e.target.value,
                      }))
                    }
                    placeholder="080…"
                  />
                </FormField>

                <div className="flex justify-end gap-2 pt-1">
                  <Button
                    type="button"
                    variant="secondary"
                    icon={X}
                    onClick={cancelEditing}
                    disabled={saving}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" icon={Save} loading={saving}>
                    Save changes
                  </Button>
                </div>
              </form>
            )}
          </div>
        </div>
      </Card>

      {/* ===============================================
          PASSWORD CARD
          =============================================== */}
      <Card>
        <div className="flex items-center gap-3 mb-5">
          <div className="w-9 h-9 rounded-xl bg-sky-50 grid place-items-center">
            <Lock className="w-4 h-4 text-sky-600" />
          </div>
          <div>
            <h3 className="text-[15px] font-semibold text-slate-900">
              Change password
            </h3>
            <p className="text-[12px] text-slate-400">
              Use a strong password with letters, numbers, and symbols.
            </p>
          </div>
        </div>

        <form onSubmit={submitPw} className="space-y-4">
          <FormField label="Current password" required>
            <Input
              type="password"
              value={pw.currentPassword}
              onChange={(e) =>
                setPw({ ...pw, currentPassword: e.target.value })
              }
              required
              autoComplete="current-password"
            />
          </FormField>

          <FormField label="New password" required>
            <Input
              type="password"
              value={pw.newPassword}
              onChange={(e) => setPw({ ...pw, newPassword: e.target.value })}
              required
              autoComplete="new-password"
            />
            <PasswordRequirements password={pw.newPassword} />
          </FormField>

          <FormField label="Confirm new password" required>
            <Input
              type="password"
              value={pw.confirmNewPassword}
              onChange={(e) =>
                setPw({ ...pw, confirmNewPassword: e.target.value })
              }
              required
              autoComplete="new-password"
            />
            {pw.confirmNewPassword.length > 0 &&
              pw.newPassword !== pw.confirmNewPassword && (
                <p className="mt-1 text-[11px] text-rose-600">
                  Passwords do not match
                </p>
              )}
          </FormField>

          <div className="flex justify-end">
            <Button type="submit" icon={Save} loading={pwSubmitting}>
              Update password
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}

// ==========================================================
// Live password requirements checklist
// ==========================================================
function PasswordRequirements({ password }: { password: string }) {
  const rules: { ok: boolean; label: string }[] = [
    { ok: password.length >= 8, label: "At least 8 characters" },
    { ok: /[A-Z]/.test(password), label: "One uppercase letter (A–Z)" },
    { ok: /[a-z]/.test(password), label: "One lowercase letter (a–z)" },
    { ok: /[0-9]/.test(password), label: "One number (0–9)" },
    {
      ok: /[^A-Za-z0-9]/.test(password),
      label: "One symbol (e.g. ! @ # $ %)",
    },
  ];

  return (
    <ul className="mt-2 space-y-1">
      {rules.map((r) => (
        <li
          key={r.label}
          className={`text-[11px] flex items-center gap-1.5 transition-colors ${
            r.ok ? "text-emerald-600" : "text-slate-400"
          }`}
        >
          {r.ok ? (
            <CheckCircle2 className="w-3 h-3 shrink-0" />
          ) : (
            <span className="w-3 h-3 shrink-0 rounded-full border border-slate-300" />
          )}
          {r.label}
        </li>
      ))}
    </ul>
  );
}
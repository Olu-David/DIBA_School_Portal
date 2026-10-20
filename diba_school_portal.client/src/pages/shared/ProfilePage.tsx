import { useRef, useState, useEffect, type ChangeEvent, type FormEvent } from "react";
import {
  Lock, Save, Camera, Pencil, X, Loader2, CheckCircle2, AlertCircle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, FormField, Input, Avatar, Badge,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useAuth } from "../../hooks/useAuth";
import { withCacheBust } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface UpdateProfileResponse {
  id: string;
  email: string | null;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;       // ← now included
  profilePictureUrl: string | null;
  role: string;
  classId: string | null;
}

export default function ProfilePage() {
  const { user, updateUser } = useAuth();
  const { toast } = useToast();

  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [profileForm, setProfileForm] = useState({
    firstName: user?.firstName ?? "",
    lastName: user?.lastName ?? "",
    phoneNumber: user?.phoneNumber ?? "",   // ← prefill, don't wipe
  });

  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [uploading, setUploading] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  const [pw, setPw] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [pwSubmitting, setPwSubmitting] = useState(false);

  // Keep the form prefill in sync if the user logs in / refreshes later.
  useEffect(() => {
    if (!editing) {
      setProfileForm({
        firstName: user?.firstName ?? "",
        lastName: user?.lastName ?? "",
        phoneNumber: user?.phoneNumber ?? "",
      });
    }
  }, [user?.firstName, user?.lastName, user?.phoneNumber, editing]);

  const startEditing = () => {
    setProfileForm({
      firstName: user?.firstName ?? "",
      lastName: user?.lastName ?? "",
      phoneNumber: user?.phoneNumber ?? "",   // ← prefill on open too
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
        phoneNumber: data.data.phoneNumber,   // ← keep cached copy fresh
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
        title: e2?.response?.data?.message ?? e2?.message ?? "Update failed.",
      });
    } finally {
      setSaving(false);
    }
  };

  const pickFile = () => fileInputRef.current?.click();

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 200 * 1024) {
      toast({ variant: "error", title: "Picture must be under 200 KB." });
      e.target.value = "";   // allow picking the same file again
      return;
    }
    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      toast({ variant: "error", title: "Only JPG, PNG, or WEBP are allowed." });
      e.target.value = "";
      return;
    }

    // Local preview so the picture swaps instantly while uploading.
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

      // Update the cached user AND bump the cache-bust version so the
      // browser refetches the server image even if the URL didn't change.
      updateUser({
        profilePictureUrl: data.data,
        updatedAt: new Date().toISOString(),
      });

      // Drop the local blob preview — the render now falls back to the
      // authoritative server URL (which we just cache-busted).
      setPreviewUrl(null);

      toast({ variant: "success", title: "Profile picture updated" });
    } catch (err: unknown) {
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
            ? "Invalid image. Check file type and size."
            : status === 401
            ? "Session expired — log in again."
            : status === 403
            ? "You don't have permission to upload."
            : status === 500
            ? "Server error — check API terminal."
            : "Upload failed."),
      });
      setPreviewUrl(null);
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
      URL.revokeObjectURL(previewLocal);
    }
  };

  const submitPw = async (e: FormEvent) => {
    e.preventDefault();

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

  // Blob preview wins while uploading; otherwise the server URL (cache-busted).
  const serverPicture = withCacheBust(
    user?.profilePictureUrl,
    user?.updatedAt ?? null
  );
  const displayPicture = previewUrl ?? serverPicture ?? null;

  if (!user) {
    return (
      <div className="max-w-2xl mx-auto">
        <PageHeader eyebrow="Account" title="My profile" />
        <Card>
          <div className="flex items-start gap-3 py-6">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">
                Not logged in — user data missing
              </p>
              <p className="text-[13px] text-red-800/80 mt-1">
                The profile page requires an authenticated user.
              </p>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <PageHeader eyebrow="Account" title="My profile" />

      {/* ===============================================
          IDENTITY CARD — avatar + upload button
          =============================================== */}
      <Card>
        <div className="flex flex-col sm:flex-row sm:items-center gap-6">
          {/* ---------- Avatar block ---------- */}
          <div className="flex flex-col items-center shrink-0">
            <button
              type="button"
              onClick={pickFile}
              disabled={uploading}
              className="relative group cursor-pointer rounded-full focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2 disabled:cursor-wait disabled:opacity-80"
              title="Click to change profile picture"
              aria-label="Change profile picture"
            >
              <Avatar
                name={`${user.firstName} ${user.lastName}`}
                src={displayPicture ?? undefined}
                size="xl"
              />

              <div className="absolute bottom-0 right-0 z-10 w-8 h-8 rounded-full bg-emerald-600 text-white ring-2 ring-white shadow-md grid place-items-center transition-transform group-hover:scale-110 group-disabled:scale-100">
                {uploading ? (
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
              onChange={handleFileChange}
            />

            <p className="mt-3 text-[11px] text-slate-500 text-center">
              {uploading ? "Uploading…" : "Click photo to change"}
            </p>
          </div>

          {/* ---------- Identity text ---------- */}
          <div className="flex-1 min-w-0 sm:border-l sm:border-slate-100 sm:pl-6">
            {!editing ? (
              <>
                <h2 className="text-[18px] font-semibold text-slate-900">
                  {user.firstName} {user.lastName}
                </h2>
                <p className="text-[13px] text-slate-500 mt-0.5">
                  {user.email ?? "-"}
                </p>
                <div className="mt-3 flex items-center gap-3 flex-wrap">
                  <Badge variant="emerald">{user.role}</Badge>
                  <button
                    type="button"
                    onClick={startEditing}
                    className="text-[12px] font-medium text-brand-600 hover:text-brand-700 inline-flex items-center gap-1"
                  >
                    <Pencil className="w-3 h-3" />
                    Edit name & phone
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
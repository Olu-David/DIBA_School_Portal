import { useState, type FormEvent } from "react";
import { KeyRound, X, AlertCircle, CheckCircle2 } from "lucide-react";
import { Button, FormField, Input } from "./ui";
import { useToast } from "../hooks/useToast";
import api from "../lib/api";
import type { ApiResponse } from "../types/common";

interface ResetPasswordModalProps {
  /** Human-readable name — shown in the header */
  fullName: string;
  /** Display role label, e.g. "Student", "Teacher" */
  role: string;
  /**
   * Either one of these is required. The backend accepts either:
   *   - studentProfileId → "Students" table row Id
   *   - targetUserId     → "AspNetUsers" row Id
   * Pass whichever one you have. If both are passed, the backend prefers targetUserId.
   */
  studentProfileId?: string;
  targetUserId?: string;
  onClose: () => void;
}

export default function ResetPasswordModal({
  fullName,
  role,
  studentProfileId,
  targetUserId,
  onClose,
}: ResetPasswordModalProps) {
  const { toast } = useToast();

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [saving, setSaving] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();

    // ---- Client-side guards ----
    if (!studentProfileId && !targetUserId) {
      toast({
        variant: "error",
        title: "Missing identifier — cannot reset.",
      });
      return;
    }

    if (password.length < 8) {
      toast({
        variant: "error",
        title: "Password must be at least 8 characters.",
      });
      return;
    }

    if (password !== confirm) {
      toast({
        variant: "error",
        title: "Passwords do not match.",
      });
      return;
    }

    // ---- Build payload — only include the identifier that exists ----
    const payload: Record<string, unknown> = {
      newPassword: password,
    };

    if (targetUserId && targetUserId.trim() !== "") {
      payload.targetUserId = targetUserId.trim();
    } else if (studentProfileId && studentProfileId.trim() !== "") {
      payload.studentProfileId = studentProfileId.trim();
    }

    setSaving(true);
    try {
      const { data } = await api.post<ApiResponse<boolean>>(
        "/auth/admin-reset-password",
        payload
      );

      if (!data.success) {
        toast({
          variant: "error",
          title: data.message ?? "Password reset failed.",
        });
        return;
      }

      toast({
        variant: "success",
        title: "Password reset successfully.",
        description: "Share the new password with the user.",
      });
      onClose();
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      const message =
        e2?.response?.data?.message ??
        e2?.message ??
        (status === 401
          ? "Session expired — please log in again."
          : status === 403
          ? "You don't have permission to reset passwords."
          : "Could not reset password.");

      toast({ variant: "error", title: message });
    } finally {
      setSaving(false);
    }
  };

  /* ---- Guard against totally missing props (defensive) ---- */
  const missingIdentifier = !studentProfileId && !targetUserId;

  return (
    <>
      <div
        className="fixed inset-0 bg-surface-950/50 z-40"
        onClick={() => !saving && onClose()}
        aria-hidden
      />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
          {/* Header */}
          <div className="p-5 flex items-center justify-between border-b border-surface-100">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-amber-50 grid place-items-center">
                <KeyRound className="w-5 h-5 text-amber-600" />
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-surface-900">
                  Reset password
                </h2>
                <p className="text-[12px] text-surface-500">
                  {role} · {fullName}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => !saving && onClose()}
              className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
              aria-label="Close"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          {/* Warning if props are missing */}
          {missingIdentifier && (
            <div className="mx-5 mt-5 p-3 rounded-xl bg-red-50 border border-red-100 flex items-start gap-2">
              <AlertCircle className="w-4 h-4 text-red-600 mt-0.5 shrink-0" />
              <p className="text-[12px] text-red-800">
                No identifier provided. Close this modal and try again from the
                student page.
              </p>
            </div>
          )}

          {/* Form */}
          <form onSubmit={submit}>
            <div className="p-5 space-y-4">
              <FormField label="New password" required>
                <Input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Min 8 chars, Aa1!"
                  autoComplete="new-password"
                  disabled={saving || missingIdentifier}
                />
              </FormField>

              <FormField label="Confirm password" required>
                <Input
                  type="password"
                  value={confirm}
                  onChange={(e) => setConfirm(e.target.value)}
                  placeholder="Re-enter password"
                  autoComplete="new-password"
                  disabled={saving || missingIdentifier}
                />
              </FormField>

              {/* Live password strength hints */}
              <PasswordHints password={password} />

              <p className="text-[11px] text-surface-500">
                The user's current session will be invalidated. They'll need to
                log in again with this new password.
              </p>
            </div>

            {/* Footer */}
            <div className="p-5 border-t border-surface-100 flex justify-end gap-3">
              <Button
                type="button"
                variant="secondary"
                onClick={onClose}
                disabled={saving}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                icon={KeyRound}
                loading={saving}
                disabled={missingIdentifier}
              >
                Reset password
              </Button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}

/* =========================================================
   Password hints — live feedback while typing
   ========================================================= */
function PasswordHints({ password }: { password: string }) {
  if (!password) return null;

  const rules = [
    { ok: password.length >= 8, label: "8+ characters" },
    { ok: /[A-Z]/.test(password), label: "Uppercase letter" },
    { ok: /[a-z]/.test(password), label: "Lowercase letter" },
    { ok: /[0-9]/.test(password), label: "Number" },
    { ok: /[^A-Za-z0-9]/.test(password), label: "Symbol" },
  ];

  return (
    <div className="flex flex-wrap gap-2">
      {rules.map((r) => (
        <span
          key={r.label}
          className={`inline-flex items-center gap-1 text-[10px] font-medium px-2 py-0.5 rounded-md ${
            r.ok
              ? "bg-emerald-50 text-emerald-700"
              : "bg-surface-100 text-surface-500"
          }`}
        >
          {r.ok ? (
            <CheckCircle2 className="w-2.5 h-2.5" />
          ) : (
            <span className="w-2.5 h-2.5 rounded-full border border-surface-300" />
          )}
          {r.label}
        </span>
      ))}
    </div>
  );
}
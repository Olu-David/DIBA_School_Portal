import { useState } from "react";
import { KeyRound, X, AlertCircle, CheckCircle2 } from "lucide-react";
import { Button, FormField, Input } from "./ui";
import { useToast } from "../hooks/useToast";
import api from "../lib/api";
import type { ApiResponse } from "../types/common";

interface ResetPasswordModalProps {
  /** Display name shown in the header */
  fullName: string;
  /** "Student" / "Teacher" / "Accountant" / etc. — shown as a hint */
  role: string;
  /** User.Id for staff. Ignored if studentProfileId is set. */
  targetUserId?: string;
  /** Student.Id for students. Ignored if targetUserId is set. */
  studentProfileId?: string;
  onClose: () => void;
  onSuccess?: () => void;
}

const PASSWORD_RULES = [
  { label: "At least 8 characters", test: (p: string) => p.length >= 8 },
  { label: "One uppercase letter", test: (p: string) => /[A-Z]/.test(p) },
  { label: "One lowercase letter", test: (p: string) => /[a-z]/.test(p) },
  { label: "One digit", test: (p: string) => /[0-9]/.test(p) },
  { label: "One symbol (!@#$…)", test: (p: string) => /[^A-Za-z0-9]/.test(p) },
];

export default function ResetPasswordModal({
  fullName,
  role,
  targetUserId,
  studentProfileId,
  onClose,
  onSuccess,
}: ResetPasswordModalProps) {
  const { toast } = useToast();
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [saving, setSaving] = useState(false);

  const allRulesPass = PASSWORD_RULES.every((r) => r.test(password));
  const passwordsMatch = password.length > 0 && password === confirm;
  const canSubmit = allRulesPass && passwordsMatch && !saving;

  const submit = async () => {
    if (!canSubmit) return;

    setSaving(true);
    try {
      // Send the right ID field — backend accepts either
      const payload = targetUserId
        ? { targetUserId, newPassword: password }
        : { studentProfileId, newPassword: password };

      const { data } = await api.post<ApiResponse<boolean>>(
        "/auth/admin-reset-password",
        payload
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Reset failed." });
        return;
      }

      toast({
        variant: "success",
        title: "Password reset",
        description: `New password is ready — share it with ${fullName}.`,
      });
      onSuccess?.();
      onClose();
    } catch (err: unknown) {
      const e2 = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not reset password.",
      });
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
        <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
          {/* Header */}
          <div className="flex items-start justify-between mb-5">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-brand-50 grid place-items-center">
                <KeyRound className="w-5 h-5 text-brand-600" />
              </div>
              <div>
                <h2 className="text-[15px] font-semibold text-surface-900">
                  Reset password
                </h2>
                <p className="text-[12px] text-surface-500">
                  {role} — {fullName}
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

          {/* Warning */}
          <div className="p-3 rounded-xl bg-amber-50 border border-amber-100 mb-5 flex items-start gap-2">
            <AlertCircle className="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
            <p className="text-[12px] text-amber-800 leading-relaxed">
              The existing password stops working immediately. {fullName} will
              be signed out of any active session.
            </p>
          </div>

          {/* Form */}
          <div className="space-y-4">
            <FormField label="New password" required>
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter a strong password"
                autoComplete="new-password"
                disabled={saving}
              />
            </FormField>

            <FormField label="Confirm password" required>
              <Input
                type="password"
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                placeholder="Re-enter to confirm"
                autoComplete="new-password"
                disabled={saving}
              />
            </FormField>

            {/* Password strength checklist */}
            <div className="p-3 rounded-xl bg-surface-50 border border-surface-100">
              <p className="text-[11px] font-medium text-surface-600 mb-2">
                Password requirements
              </p>
              <ul className="space-y-1">
                {PASSWORD_RULES.map((rule) => {
                  const ok = rule.test(password);
                  return (
                    <li
                      key={rule.label}
                      className={`flex items-center gap-2 text-[11px] ${
                        ok ? "text-emerald-700" : "text-surface-500"
                      }`}
                    >
                      <span
                        className={`w-3.5 h-3.5 rounded-full border flex items-center justify-center shrink-0 ${
                          ok
                            ? "bg-emerald-500 border-emerald-500"
                            : "bg-white border-surface-300"
                        }`}
                      >
                        {ok && (
                          <CheckCircle2 className="w-2.5 h-2.5 text-white" />
                        )}
                      </span>
                      {rule.label}
                    </li>
                  );
                })}
              </ul>
            </div>

            {/* Mismatch warning */}
            {confirm.length > 0 && password !== confirm && (
              <p className="text-[11px] text-rose-600">
                Passwords do not match.
              </p>
            )}
          </div>

          {/* Footer */}
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="secondary" onClick={onClose} disabled={saving}>
              Cancel
            </Button>
            <Button
              icon={KeyRound}
              onClick={submit}
              loading={saving}
              disabled={!canSubmit}
            >
              Reset password
            </Button>
          </div>
        </div>
      </div>
    </>
  );
}
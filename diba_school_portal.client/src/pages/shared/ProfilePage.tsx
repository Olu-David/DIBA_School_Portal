import { useState, type FormEvent } from "react";
import { Lock, Save, Camera } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, FormField, Input, Avatar, Badge } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useAuth } from "../../hooks/useAuth";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function ProfilePage() {
  const { user } = useAuth();
  const { toast } = useToast();
  const [pw, setPw] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [submitting, setSubmitting] = useState(false);

  const submitPw = async (e: FormEvent) => {
    e.preventDefault();
    if (pw.newPassword !== pw.confirmNewPassword) {
      toast({ variant: "error", title: "Passwords do not match" });
      return;
    }
    setSubmitting(true);
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
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <PageHeader eyebrow="Account" title="My profile" />

      <Card>
        <div className="flex items-center gap-5">
          <div className="relative">
            <Avatar
              name={`${user?.firstName} ${user?.lastName}`}
              src={user?.profilePictureUrl}
              size="xl"
            />
            <button
              type="button"
              className="absolute -bottom-1 -right-1 w-7 h-7 rounded-full bg-emerald-600 text-white grid place-items-center shadow-sm hover:bg-emerald-700"
            >
              <Camera className="w-3.5 h-3.5" />
            </button>
          </div>
          <div>
            <h2 className="text-[16px] font-semibold text-slate-900">
              {user?.firstName} {user?.lastName}
            </h2>
            <p className="text-[12px] text-slate-400 mt-0.5">{user?.email ?? "—"}</p>
            <div className="mt-2">
              <Badge variant="emerald">{user?.role}</Badge>
            </div>
          </div>
        </div>
      </Card>

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
              onChange={(e) => setPw({ ...pw, currentPassword: e.target.value })}
              required
            />
          </FormField>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <FormField label="New password" required>
              <Input
                type="password"
                value={pw.newPassword}
                onChange={(e) => setPw({ ...pw, newPassword: e.target.value })}
                required
              />
            </FormField>
            <FormField label="Confirm new password" required>
              <Input
                type="password"
                value={pw.confirmNewPassword}
                onChange={(e) => setPw({ ...pw, confirmNewPassword: e.target.value })}
                required
              />
            </FormField>
          </div>
          <div className="flex justify-end">
            <Button type="submit" icon={Save} loading={submitting}>
              Update password
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
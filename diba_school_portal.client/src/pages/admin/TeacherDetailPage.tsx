import { useEffect, useState, useMemo, type ChangeEvent } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Mail, Phone, GraduationCap, BookOpen, Users, Calendar,
  Pencil, Save, X, Power, KeyRound, AlertCircle, ShieldCheck,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Card, Avatar, Badge, Skeleton, EmptyState, Button, FormField, Input,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useRole } from "../../hooks/useRole";
import { formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

// ==========================================================
// Types
// ==========================================================
interface TeacherDetail {
  id: string;
  userId: string;
  staffId: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  qualification: string;
  dateEmployed: string;
  primaryRole: string;
  isActive: boolean;
  subjectCount: number;
  classCount: number;
  subjects: { subjectId: string; subjectName: string; subjectCode: string }[];
  classes: { classId: string; className: string; section: number }[];
}

interface UpdateTeacherPayload {
  firstName: string;
  lastName: string;
  qualification: string;
  phoneNumber: string;
  isActive: boolean;
}

interface SubjectOption {
  id: string;
  name: string;
  code: string;
}

interface ClassOption {
  id: string;
  name: string;
  academicSection: string;
  displayOrder?: number;
}

// ==========================================================
// Role config
// ==========================================================
const ASSIGNABLE_ROLES = [
  "Teacher",
  "FormTeacher",
  "Accountant",
  "Registrar",
  "SchoolAdmin",
  "VicePrincipal",
  "Headmaster",
  "Headmistress",
  "Principal",
] as const;

const LEADERSHIP_ROLES = ["Principal", "Headmaster", "Headmistress"];

// ==========================================================
// Component
// ==========================================================
export default function TeacherDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { role: myRole } = useRole();

  // ---------- Core ----------
  const [teacher, setTeacher] = useState<TeacherDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // ---------- Identity edit ----------
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<UpdateTeacherPayload>({
    firstName: "", lastName: "", qualification: "", phoneNumber: "", isActive: true,
  });

  // ---------- Active toggle ----------
  const [toggling, setToggling] = useState(false);

  // ---------- Password reset ----------
  const [showReset, setShowReset] = useState(false);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [resetting, setResetting] = useState(false);

  // ---------- Subject editor ----------
  const [editingSubjects, setEditingSubjects] = useState(false);
  const [savingSubjects, setSavingSubjects] = useState(false);
  const [loadingSubjectRefs, setLoadingSubjectRefs] = useState(false);
  const [allSubjects, setAllSubjects] = useState<SubjectOption[]>([]);
  const [selectedSubjectIds, setSelectedSubjectIds] = useState<string[]>([]);

  // ---------- Class editor ----------
  const [editingClasses, setEditingClasses] = useState(false);
  const [savingClasses, setSavingClasses] = useState(false);
  const [loadingClassRefs, setLoadingClassRefs] = useState(false);
  const [allClasses, setAllClasses] = useState<ClassOption[]>([]);
  const [selectedClassIds, setSelectedClassIds] = useState<string[]>([]);

  // ---------- Role change ----------
  const [showRoleChange, setShowRoleChange] = useState(false);
  const [selectedRole, setSelectedRole] = useState("");
  const [changingRole, setChangingRole] = useState(false);

  // ==========================================================
  // LOAD
  // ==========================================================
  const load = async () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<ApiResponse<TeacherDetail>>(`/teacher/${id}`);
      if (!data.success || !data.data) {
        setError(data.message ?? "Could not load teacher.");
        setTeacher(null);
        return;
      }
      setTeacher(data.data);
      setForm({
        firstName: data.data.firstName,
        lastName: data.data.lastName,
        qualification: data.data.qualification ?? "",
        phoneNumber: data.data.phoneNumber ?? "",
        isActive: data.data.isActive,
      });
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      setError(e2?.response?.data?.message ?? e2?.message ?? "Could not load teacher.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  // ==========================================================
  // IDENTITY EDIT
  // ==========================================================
  const startEdit = () => {
    if (!teacher) return;
    setForm({
      firstName: teacher.firstName,
      lastName: teacher.lastName,
      qualification: teacher.qualification ?? "",
      phoneNumber: teacher.phoneNumber ?? "",
      isActive: teacher.isActive,
    });
    setEditing(true);
    setShowReset(false);
    setShowRoleChange(false);
  };

  const cancelEdit = () => {
    setEditing(false);
    if (teacher) {
      setForm({
        firstName: teacher.firstName,
        lastName: teacher.lastName,
        qualification: teacher.qualification ?? "",
        phoneNumber: teacher.phoneNumber ?? "",
        isActive: teacher.isActive,
      });
    }
  };

  const saveEdit = async () => {
    if (!teacher) return;
    if (!form.firstName.trim() || !form.lastName.trim()) {
      toast({ variant: "error", title: "First and last name are required." });
      return;
    }
    setSaving(true);
    try {
      const { data } = await api.put<ApiResponse<TeacherDetail>>(
        `/teacher/${teacher.id}`,
        {
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          qualification: form.qualification.trim(),
          phoneNumber: form.phoneNumber.trim(),
          isActive: form.isActive,
        }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Update failed." });
        return;
      }
      toast({ variant: "success", title: "Teacher updated" });
      setEditing(false);
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not update teacher.",
      });
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // TOGGLE ACTIVE
  // ==========================================================
  const toggleActive = async () => {
    if (!teacher) return;
    const newStatus = !teacher.isActive;
    if (
      !window.confirm(
        `Are you sure you want to ${newStatus ? "activate" : "deactivate"} ` +
        `${teacher.firstName} ${teacher.lastName}?`
      )
    ) return;

    setToggling(true);
    try {
      const { data } = await api.put<ApiResponse<TeacherDetail>>(
        `/teacher/${teacher.id}`,
        {
          firstName: teacher.firstName,
          lastName: teacher.lastName,
          qualification: teacher.qualification ?? "",
          phoneNumber: teacher.phoneNumber ?? "",
          isActive: newStatus,
        }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Could not change status." });
        return;
      }
      toast({
        variant: "success",
        title: newStatus ? "Teacher activated" : "Teacher deactivated",
      });
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not change status.",
      });
    } finally {
      setToggling(false);
    }
  };

  // ==========================================================
  // RESET PASSWORD
  // ==========================================================
  const openReset = () => {
    setEditing(false);
    setShowRoleChange(false);
    setShowReset(true);
    setNewPassword("");
    setConfirmPassword("");
  };

  const cancelReset = () => {
    setShowReset(false);
    setNewPassword("");
    setConfirmPassword("");
  };

  const submitReset = async () => {
    if (!teacher) return;

    if (newPassword.length < 8) {
      toast({ variant: "error", title: "Password must be at least 8 characters." });
      return;
    }
    if (!/[A-Z]/.test(newPassword) || !/[a-z]/.test(newPassword) ||
        !/[0-9]/.test(newPassword) || !/[^A-Za-z0-9]/.test(newPassword)) {
      toast({
        variant: "error",
        title: "Password must contain upper, lower, digit and symbol.",
      });
      return;
    }
    if (newPassword !== confirmPassword) {
      toast({ variant: "error", title: "Passwords do not match." });
      return;
    }

    setResetting(true);
    try {
      const { data } = await api.post<ApiResponse<boolean>>(
        "/auth/admin-reset-password",
        { targetUserId: teacher.userId, newPassword }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Reset failed." });
        return;
      }
      toast({
        variant: "success",
        title: "Password reset",
        description: "Share the new password with the teacher.",
      });
      cancelReset();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not reset password.",
      });
    } finally {
      setResetting(false);
    }
  };

  // ==========================================================
  // CHANGE ROLE
  // ==========================================================
  const openRoleChange = () => {
    if (!teacher) return;
    setEditing(false);
    setShowReset(false);
    setSelectedRole(teacher.primaryRole);
    setShowRoleChange(true);
  };

  const cancelRoleChange = () => {
    setShowRoleChange(false);
    setSelectedRole("");
  };

  const submitRoleChange = async () => {
    if (!teacher) return;
    if (!selectedRole) {
      toast({ variant: "error", title: "Pick a role first." });
      return;
    }
    if (selectedRole === teacher.primaryRole) {
      toast({ variant: "error", title: "That's already their role." });
      return;
    }

    if (
      !window.confirm(
        `Change ${teacher.firstName} ${teacher.lastName}'s role from ` +
        `"${teacher.primaryRole}" to "${selectedRole}"?\n\n` +
        "The user will need to log out and log back in for the change to take effect."
      )
    ) return;

    setChangingRole(true);
    try {
      const { data } = await api.put<ApiResponse<unknown>>(
        `/auth/users/${teacher.userId}/role`,
        { newRole: selectedRole }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Role change failed." });
        return;
      }
      toast({
        variant: "success",
        title: "Role updated",
        description:
          data.message ??
          "The user must log out and log back in for the change to apply.",
      });
      setShowRoleChange(false);
      await load();
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not change role.",
      });
    } finally {
      setChangingRole(false);
    }
  };

  // ==========================================================
  // SUBJECT EDITOR
  // ==========================================================
  const openSubjectEditor = async () => {
    if (!teacher) return;
    setEditingSubjects(true);
    setSelectedSubjectIds(teacher.subjects.map((s) => s.subjectId));

    if (allSubjects.length > 0) return;

    setLoadingSubjectRefs(true);
    try {
      const { data } = await api.get<ApiResponse<SubjectOption[]>>("/subject/all");
      if (data.success) setAllSubjects(data.data ?? []);
      else toast({ variant: "error", title: data.message ?? "Could not load subjects." });
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not load subjects.",
      });
    } finally {
      setLoadingSubjectRefs(false);
    }
  };

  const saveSubjects = async () => {
    if (!teacher) return;
    setSavingSubjects(true);
    try {
      const { data } = await api.put<ApiResponse<boolean>>(
        `/teacher/${teacher.id}/subjects`,
        selectedSubjectIds
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to save subjects." });
        return;
      }
      toast({ variant: "success", title: "Subjects updated" });
      setEditingSubjects(false);
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Failed to save subjects.",
      });
    } finally {
      setSavingSubjects(false);
    }
  };

  const toggleSubject = (sid: string) => {
    setSelectedSubjectIds((prev) =>
      prev.includes(sid) ? prev.filter((x) => x !== sid) : [...prev, sid]
    );
  };

  // ==========================================================
  // CLASS EDITOR
  // ==========================================================
  const openClassEditor = async () => {
    if (!teacher) return;
    setEditingClasses(true);
    setSelectedClassIds(teacher.classes.map((c) => c.classId));

    if (allClasses.length > 0) return;

    setLoadingClassRefs(true);
    try {
      const { data } = await api.get<ApiResponse<ClassOption[]>>("/schoolclass/all");
      if (data.success) {
        const sorted = [...(data.data ?? [])].sort((a, b) => {
          const ao = a.displayOrder ?? 9999;
          const bo = b.displayOrder ?? 9999;
          return ao !== bo ? ao - bo : a.name.localeCompare(b.name);
        });
        setAllClasses(sorted);
      } else {
        toast({ variant: "error", title: data.message ?? "Could not load classes." });
      }
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Could not load classes.",
      });
    } finally {
      setLoadingClassRefs(false);
    }
  };

  const saveClasses = async () => {
    if (!teacher) return;
    setSavingClasses(true);
    try {
      const { data } = await api.put<ApiResponse<boolean>>(
        `/teacher/${teacher.id}/classes`,
        selectedClassIds
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to save classes." });
        return;
      }
      toast({ variant: "success", title: "Classes updated" });
      setEditingClasses(false);
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } }; message?: string };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? e2?.message ?? "Failed to save classes.",
      });
    } finally {
      setSavingClasses(false);
    }
  };

  const toggleClass = (cid: string) => {
    setSelectedClassIds((prev) =>
      prev.includes(cid) ? prev.filter((x) => x !== cid) : [...prev, cid]
    );
  };

  // ==========================================================
  // Derived role permissions
  // ==========================================================
  const roleIsChangeableByMe = useMemo(() => {
    if (!teacher) return false;
    if (teacher.primaryRole === "SuperAdmin") return false;
    if (myRole === "SuperAdmin") return true;
    if (myRole === "Principal") {
      if (LEADERSHIP_ROLES.includes(teacher.primaryRole)) return false;
      return true;
    }
    return false;
  }, [myRole, teacher]);

  const availableRoles = useMemo(() => {
    return ASSIGNABLE_ROLES.filter((r) => {
      if (r === teacher?.primaryRole) return true;
      if (myRole !== "SuperAdmin" && LEADERSHIP_ROLES.includes(r)) return false;
      return true;
    });
  }, [myRole, teacher]);

  // ==========================================================
  // LOADING
  // ==========================================================
  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <Card><Skeleton className="h-64" /></Card>
      </div>
    );
  }

  // ==========================================================
  // ERROR
  // ==========================================================
  if (error || !teacher) {
    return (
      <div className="space-y-6">
        <button
          type="button"
          onClick={() => navigate("/admin/teachers")}
          className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          All staff
        </button>
        <Card>
          <div className="flex items-start gap-3 py-6">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">Couldn't load teacher</p>
              <p className="text-[13px] text-red-800/80 mt-1">{error ?? "Teacher not found."}</p>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  const change =
    (k: keyof UpdateTeacherPayload) =>
    (e: ChangeEvent<HTMLInputElement>) =>
      setForm((p) => ({ ...p, [k]: e.target.value }));

  const isAnyModeActive = editing || showReset || showRoleChange;

  // ==========================================================
  // RENDER
  // ==========================================================
  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/teachers")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All staff
      </button>

      <PageHeader
        eyebrow="Staff"
        title={`${teacher.firstName} ${teacher.lastName}`}
        description={`${teacher.primaryRole} · ${teacher.staffId}`}
      />

      {/* ===================== IDENTITY CARD ===================== */}
      <Card>
        <div className="flex flex-col sm:flex-row sm:items-start gap-5">
          <Avatar
            name={`${teacher.firstName} ${teacher.lastName}`}
            src={teacher.profilePictureUrl}
            size="xl"
          />

          <div className="flex-1 min-w-0">
            {/* ---------- View mode ---------- */}
            {!isAnyModeActive && (
              <>
                <div className="flex items-center gap-2 flex-wrap">
                  <h2 className="text-2xl font-semibold text-surface-900">
                    {teacher.firstName} {teacher.lastName}
                  </h2>
                  <Badge variant={teacher.isActive ? "emerald" : "neutral"}>
                    {teacher.isActive ? "Active" : "Inactive"}
                  </Badge>
                  <Badge variant="violet">{teacher.primaryRole}</Badge>
                </div>
                <p className="mt-1 text-[13px] text-surface-500 font-mono">{teacher.staffId}</p>
                <div className="mt-3 flex items-center gap-4 flex-wrap text-[12px] text-surface-500">
                  {teacher.email && (
                    <span className="inline-flex items-center gap-1.5">
                      <Mail className="w-3.5 h-3.5" />{teacher.email}
                    </span>
                  )}
                  {teacher.phoneNumber && (
                    <span className="inline-flex items-center gap-1.5">
                      <Phone className="w-3.5 h-3.5" />{teacher.phoneNumber}
                    </span>
                  )}
                  <span className="inline-flex items-center gap-1.5">
                    <Calendar className="w-3.5 h-3.5" />Employed {formatDate(teacher.dateEmployed)}
                  </span>
                  {teacher.qualification && (
                    <span className="inline-flex items-center gap-1.5">
                      <GraduationCap className="w-3.5 h-3.5" />{teacher.qualification}
                    </span>
                  )}
                </div>
              </>
            )}

            {/* ---------- Edit identity mode ---------- */}
            {editing && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <FormField label="First name" required>
                    <Input value={form.firstName} onChange={change("firstName")} />
                  </FormField>
                  <FormField label="Last name" required>
                    <Input value={form.lastName} onChange={change("lastName")} />
                  </FormField>
                  <FormField label="Qualification">
                    <Input value={form.qualification} onChange={change("qualification")} placeholder="e.g. B.Ed" />
                  </FormField>
                  <FormField label="Phone">
                    <Input value={form.phoneNumber} onChange={change("phoneNumber")} placeholder="080…" />
                  </FormField>
                </div>
                <div className="flex items-center gap-2">
                  <input
                    id="active-toggle"
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => setForm((p) => ({ ...p, isActive: e.target.checked }))}
                    className="w-4 h-4 rounded border-slate-300"
                  />
                  <label htmlFor="active-toggle" className="text-[13px] text-slate-700">
                    Active — teacher can log in
                  </label>
                </div>
              </div>
            )}

            {/* ---------- Reset password mode ---------- */}
            {showReset && (
              <div className="space-y-4">
                <div>
                  <h3 className="text-[14px] font-semibold text-surface-900">
                    Reset password for {teacher.firstName} {teacher.lastName}
                  </h3>
                  <p className="text-[12px] text-surface-500 mt-1">
                    Must contain 8+ chars, upper, lower, digit, and symbol.
                  </p>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <FormField label="New password" required>
                    <Input
                      type="password"
                      value={newPassword}
                      onChange={(e) => setNewPassword(e.target.value)}
                      placeholder="Min 8 chars, Aa1!"
                    />
                  </FormField>
                  <FormField label="Confirm password" required>
                    <Input
                      type="password"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      placeholder="Re-enter"
                    />
                  </FormField>
                </div>
              </div>
            )}

            {/* ---------- Change role mode ---------- */}
            {showRoleChange && (
              <div className="space-y-4">
                <div>
                  <h3 className="text-[14px] font-semibold text-surface-900">
                    Change role
                  </h3>
                  <p className="text-[12px] text-surface-500 mt-1">
                    From <strong>{teacher.primaryRole}</strong> to a new role. The user
                    will need to log out and log back in.
                  </p>
                </div>
                <div className="max-w-sm">
                  <FormField label="New role" required>
                    <select
                      value={selectedRole}
                      onChange={(e) => setSelectedRole(e.target.value)}
                      disabled={changingRole}
                      className="w-full rounded-lg border border-slate-200 px-3 py-2 text-[14px] focus:outline-none focus:ring-2 focus:ring-brand-200"
                    >
                      {availableRoles.map((r) => (
                        <option key={r} value={r} disabled={r === teacher.primaryRole}>
                          {r}
                          {r === teacher.primaryRole ? " (current)" : ""}
                        </option>
                      ))}
                    </select>
                  </FormField>
                </div>
              </div>
            )}
          </div>

          {/* ---------- Action buttons (view mode only) ---------- */}
          {!isAnyModeActive && (
            <div className="flex flex-col sm:items-end gap-2 shrink-0">
              <Button variant="secondary" icon={Pencil} onClick={startEdit}>
                Edit
              </Button>
              <Button variant="secondary" icon={Power} onClick={toggleActive} loading={toggling}>
                {teacher.isActive ? "Deactivate" : "Activate"}
              </Button>
              <Button variant="secondary" icon={KeyRound} onClick={openReset}>
                Reset password
              </Button>
              {roleIsChangeableByMe && (
                <Button variant="secondary" icon={ShieldCheck} onClick={openRoleChange}>
                  Change role
                </Button>
              )}
            </div>
          )}
        </div>

        {/* ---------- Footers ---------- */}
        {editing && (
          <div className="mt-5 pt-5 border-t border-slate-100 flex justify-end gap-2">
            <Button variant="secondary" icon={X} onClick={cancelEdit} disabled={saving}>Cancel</Button>
            <Button icon={Save} onClick={saveEdit} loading={saving}>Save changes</Button>
          </div>
        )}

        {showReset && (
          <div className="mt-5 pt-5 border-t border-slate-100 flex justify-end gap-2">
            <Button variant="secondary" icon={X} onClick={cancelReset} disabled={resetting}>Cancel</Button>
            <Button icon={KeyRound} onClick={submitReset} loading={resetting}>Reset password</Button>
          </div>
        )}

        {showRoleChange && (
          <div className="mt-5 pt-5 border-t border-slate-100 flex justify-end gap-2">
            <Button variant="secondary" icon={X} onClick={cancelRoleChange} disabled={changingRole}>Cancel</Button>
            <Button
              icon={ShieldCheck}
              onClick={submitRoleChange}
              loading={changingRole}
              disabled={selectedRole === teacher.primaryRole}
            >
              Change role
            </Button>
          </div>
        )}
      </Card>

      {/* ===================== SUBJECTS + CLASSES ===================== */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* ---------- Subjects card ---------- */}
        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between gap-2">
            <h3 className="text-[15px] font-semibold text-surface-900 flex items-center gap-2">
              <BookOpen className="w-4 h-4 text-surface-500" />
              Subjects taught ({teacher.subjectCount})
            </h3>
            {!editingSubjects ? (
              <Button variant="ghost" size="sm" icon={Pencil} onClick={openSubjectEditor}>
                Edit
              </Button>
            ) : (
              <div className="flex gap-2">
                <Button
                  variant="ghost" size="sm" icon={X}
                  onClick={() => setEditingSubjects(false)}
                  disabled={savingSubjects}
                >
                  Cancel
                </Button>
                <Button size="sm" icon={Save} onClick={saveSubjects} loading={savingSubjects}>
                  Save
                </Button>
              </div>
            )}
          </div>

          {editingSubjects ? (
            <div className="px-6 pb-6">
              {loadingSubjectRefs ? (
                <Skeleton className="h-32" />
              ) : allSubjects.length === 0 ? (
                <p className="text-[12px] text-surface-400 italic">No subjects exist yet.</p>
              ) : (
                <>
                  <div className="max-h-64 overflow-y-auto border border-surface-200 rounded-xl divide-y divide-surface-100">
                    {allSubjects.map((s) => (
                      <label
                        key={s.id}
                        className="flex items-center gap-2 px-3 py-2 text-[12px] hover:bg-surface-50 cursor-pointer"
                      >
                        <input
                          type="checkbox"
                          checked={selectedSubjectIds.includes(s.id)}
                          onChange={() => toggleSubject(s.id)}
                          className="w-4 h-4 rounded"
                        />
                        <span className="flex-1 text-surface-800 truncate">{s.name}</span>
                        <span className="text-[10px] text-surface-400 font-mono">{s.code}</span>
                      </label>
                    ))}
                  </div>
                  <p className="text-[11px] text-surface-500 mt-2">
                    {selectedSubjectIds.length} selected
                  </p>
                </>
              )}
            </div>
          ) : teacher.subjects.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={BookOpen} title="No subjects assigned" />
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {teacher.subjects.map((s) => (
                <div key={s.subjectId} className="px-6 py-3.5 flex items-center justify-between">
                  <p className="text-[13px] font-medium text-surface-800">{s.subjectName}</p>
                  <span className="text-[11px] text-surface-400 font-mono">{s.subjectCode}</span>
                </div>
              ))}
            </div>
          )}
        </Card>

        {/* ---------- Classes card ---------- */}
        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between gap-2">
            <h3 className="text-[15px] font-semibold text-surface-900 flex items-center gap-2">
              <Users className="w-4 h-4 text-surface-500" />
              Classes assigned ({teacher.classCount})
            </h3>
            {!editingClasses ? (
              <Button variant="ghost" size="sm" icon={Pencil} onClick={openClassEditor}>
                Edit
              </Button>
            ) : (
              <div className="flex gap-2">
                <Button
                  variant="ghost" size="sm" icon={X}
                  onClick={() => setEditingClasses(false)}
                  disabled={savingClasses}
                >
                  Cancel
                </Button>
                <Button size="sm" icon={Save} onClick={saveClasses} loading={savingClasses}>
                  Save
                </Button>
              </div>
            )}
          </div>

          {editingClasses ? (
            <div className="px-6 pb-6">
              {loadingClassRefs ? (
                <Skeleton className="h-32" />
              ) : allClasses.length === 0 ? (
                <p className="text-[12px] text-surface-400 italic">No classes exist yet.</p>
              ) : (
                <>
                  <div className="max-h-64 overflow-y-auto border border-surface-200 rounded-xl divide-y divide-surface-100">
                    {allClasses.map((c) => (
                      <label
                        key={c.id}
                        className="flex items-center gap-2 px-3 py-2 text-[12px] hover:bg-surface-50 cursor-pointer"
                      >
                        <input
                          type="checkbox"
                          checked={selectedClassIds.includes(c.id)}
                          onChange={() => toggleClass(c.id)}
                          className="w-4 h-4 rounded"
                        />
                        <span className="text-surface-800">{c.name}</span>
                      </label>
                    ))}
                  </div>
                  <p className="text-[11px] text-surface-500 mt-2">
                    {selectedClassIds.length} selected
                  </p>
                </>
              )}
            </div>
          ) : teacher.classes.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={Users} title="No classes assigned" />
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {teacher.classes.map((c) => (
                <button
                  key={c.classId}
                  type="button"
                  onClick={() => navigate(`/admin/classes/${c.classId}`)}
                  className="w-full text-left px-6 py-3.5 hover:bg-surface-50 flex items-center justify-between"
                >
                  <p className="text-[13px] font-medium text-surface-800">{c.className}</p>
                  <Badge variant="sky">View</Badge>
                </button>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
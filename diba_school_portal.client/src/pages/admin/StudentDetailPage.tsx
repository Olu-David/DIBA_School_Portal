import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Mail, Phone, Calendar, GraduationCap, Users,
  Wallet, TrendingUp, Pencil, Trash2, Receipt, FileText, Plus,
  X, AlertTriangle,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Avatar, Badge, Skeleton, EmptyState, StatCard,
  FormField, Input, Select,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useRole } from "../../hooks/useRole";
import { formatNaira, formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";
import type { StudentDetail } from "../../types/student";

// ==========================================================
// Types
// ==========================================================
interface Payment {
  id: string;
  referenceNumber: string;
  amountPaid: number;
  paymentDate: string;
  gateway: string;
  status: string;
  termName: string;
  sessionName: string;
}

interface SchoolClass {
  id: string;
  name: string;
  academicSection: string;
}

// ==========================================================
// Constants
// ==========================================================
const STREAM_TONE: Record<string, "sky" | "violet" | "amber" | "rose"> = {
  Science: "sky",
  Arts: "violet",
  Commercial: "amber",
  General: "rose",
};

const STREAMS = ["Science", "Arts", "Commercial"];

// Roles allowed to edit/delete a student (matches backend
// [Authorize(Roles = "SuperAdmin,Principal,Registrar,SchoolAdmin")]
// on PUT/DELETE /api/Student/{id})
const STUDENT_EDITOR_ROLES = [
  "SuperAdmin",
  "Principal",
  "Registrar",
  "SchoolAdmin",
];

// ==========================================================
// Component
// ==========================================================
export default function StudentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { role } = useRole();

  // ---- Role check — simple and stable ----
  const canEdit =
    role != null && STUDENT_EDITOR_ROLES.includes(role);

  // ---- Data ----
  const [student, setStudent] = useState<StudentDetail | null>(null);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorStatus, setErrorStatus] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // ---- Edit dialog state ----
  const [editOpen, setEditOpen] = useState(false);
  const [editForm, setEditForm] = useState({
    firstName: "",
    lastName: "",
    phoneNumber: "",
    schoolClassId: "",
    stream: "",
    isActive: true,
  });
  const [saving, setSaving] = useState(false);

  // ---- Delete dialog state ----
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);

  // ==========================================================
  // Load everything on mount
  // ==========================================================
  useEffect(() => {
    if (!id) return;

    let cancelled = false;
    setLoading(true);
    setErrorStatus(null);
    setErrorMessage(null);

    (async () => {
      try {
        const [sRes, pRes, cRes] = await Promise.allSettled([
          api.get<ApiResponse<StudentDetail>>(`/student/${id}`),
          api.get<ApiResponse<Payment[]>>(`/payment/student/${id}`),
          api.get<ApiResponse<SchoolClass[]>>(`/schoolclass/all`),
        ]);

        if (cancelled) return;

        // -------- Student --------
        if (sRes.status === "fulfilled" && sRes.value.data.success && sRes.value.data.data) {
          setStudent(sRes.value.data.data);
        } else if (sRes.status === "rejected") {
          const status = (sRes.reason as any)?.response?.status ?? null;
          const msg =
            (sRes.reason as any)?.response?.data?.message ??
            "Could not load student.";
          setErrorStatus(status);
          setErrorMessage(msg);
        } else {
          setErrorStatus(404);
          setErrorMessage("Student not found.");
        }

        // -------- Payments (non-fatal) --------
        if (pRes.status === "fulfilled" && pRes.value.data.success) {
          setPayments((pRes.value.data.data ?? []).slice(0, 8));
        } else {
          setPayments([]);
        }

        // -------- Classes (non-fatal) --------
        if (cRes.status === "fulfilled" && cRes.value.data.success) {
          setClasses(cRes.value.data.data ?? []);
        } else {
          setClasses([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [id]);

  // ==========================================================
  // Edit
  // ==========================================================
  const openEdit = () => {
    if (!canEdit || !student) return;
    setEditForm({
      firstName: student.firstName,
      lastName: student.lastName,
      phoneNumber: student.phoneNumber ?? "",
      schoolClassId: student.schoolClassId ?? "",
      stream: student.stream ?? "",
      isActive: student.isActive,
    });
    setEditOpen(true);
  };

  const saveEdit = async () => {
    if (!canEdit || !student) return;

    if (!editForm.firstName.trim() || !editForm.lastName.trim()) {
      toast({ variant: "error", title: "First and last name are required." });
      return;
    }

    const selectedClass = classes.find((c) => c.id === editForm.schoolClassId);
    const isSenior = selectedClass?.academicSection === "SeniorSecondary";

    if (isSenior && !editForm.stream) {
      toast({ variant: "error", title: "Stream is required for this class." });
      return;
    }

    setSaving(true);
    try {
      const { data } = await api.put<ApiResponse<StudentDetail>>(
        `/student/${student.id}`,
        {
          firstName: editForm.firstName.trim(),
          lastName: editForm.lastName.trim(),
          phoneNumber: editForm.phoneNumber.trim() || null,
          schoolClassId: editForm.schoolClassId,
          stream: isSenior ? editForm.stream : null,
          isActive: editForm.isActive,
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to save." });
        return;
      }

      setStudent(data.data);
      setEditOpen(false);
      toast({ variant: "success", title: "Student updated." });
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not save changes.",
      });
    } finally {
      setSaving(false);
    }
  };

  // ==========================================================
  // Delete (soft)
  // ==========================================================
  const confirmDelete = async () => {
    if (!canEdit || !student) return;
    setDeleting(true);
    try {
      const { data } = await api.delete<ApiResponse<boolean>>(
        `/student/${student.id}`
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to delete." });
        return;
      }
      toast({ variant: "success", title: "Student removed." });
      navigate("/admin/students");
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not delete student.",
      });
    } finally {
      setDeleting(false);
      setDeleteOpen(false);
    }
  };

  // ==========================================================
  // Loading / error states
  // ==========================================================
  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <Card>
          <Skeleton className="h-24" />
        </Card>
      </div>
    );
  }

  if (!student) {
    const title =
      errorStatus === 403
        ? "You don't have access to this student"
        : errorStatus === 404
        ? "Student not found"
        : "Could not load student";

    return (
      <Card>
        <EmptyState
          icon={Users}
          title={title}
          description={errorMessage ?? undefined}
          action={
            <Button onClick={() => navigate("/admin/students")}>Back</Button>
          }
        />
      </Card>
    );
  }

  const parentLinks = student.parents ?? [];
  const editIsSenior =
    classes.find((c) => c.id === editForm.schoolClassId)?.academicSection ===
    "SeniorSecondary";

  // ==========================================================
  // Render
  // ==========================================================
  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/students")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All students
      </button>

      {/* ===============================================
          IDENTITY
          =============================================== */}
      <Card>
        <div className="flex flex-col sm:flex-row sm:items-center gap-5">
          <Avatar
            name={`${student.firstName} ${student.lastName}`}
            src={student.profilePictureUrl}
            size="xl"
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h1 className="text-2xl font-semibold text-surface-900">
                {student.firstName} {student.lastName}
              </h1>
              {student.isActive ? (
                <Badge variant="emerald">Active</Badge>
              ) : (
                <Badge variant="neutral">Inactive</Badge>
              )}
              {student.stream && (
                <Badge variant={STREAM_TONE[student.stream] ?? "sky"}>
                  {student.stream}
                </Badge>
              )}
            </div>
            <p className="mt-1 text-[13px] text-surface-500 font-mono">
              {student.admissionNumber}
            </p>
            <div className="mt-3 flex items-center gap-4 flex-wrap text-[12px] text-surface-500">
              <span className="inline-flex items-center gap-1.5">
                <GraduationCap className="w-3.5 h-3.5" />
                {student.className}
                {student.stream && ` - ${student.stream}`}
              </span>
              {student.email && (
                <span className="inline-flex items-center gap-1.5">
                  <Mail className="w-3.5 h-3.5" />
                  {student.email}
                </span>
              )}
              {student.phoneNumber && (
                <span className="inline-flex items-center gap-1.5">
                  <Phone className="w-3.5 h-3.5" />
                  {student.phoneNumber}
                </span>
              )}
              <span className="inline-flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5" />
                Enrolled {formatDate(student.createdAt)}
              </span>
            </div>
          </div>

          {canEdit && (
            <div className="flex gap-2 shrink-0">
              <Button variant="secondary" icon={Pencil} onClick={openEdit}>
                Edit
              </Button>
              <Button
                variant="ghost"
                icon={Trash2}
                className="text-flame-600 hover:bg-flame-50"
                onClick={() => setDeleteOpen(true)}
              >
                Delete
              </Button>
            </div>
          )}
        </div>
      </Card>

      {/* ===============================================
          STATS
          =============================================== */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Outstanding balance"
          value={formatNaira(student.outstandingBalance ?? 0)}
          hint={student.isFullyPaid ? "Fully paid" : "Fees unpaid"}
          icon={Wallet}
          tone={student.isFullyPaid ? "emerald" : "rose"}
        />
        <StatCard
          label="Latest average"
          value={
            student.latestAverageScore
              ? `${student.latestAverageScore.toFixed(1)}%`
              : "-"
          }
          hint={student.latestTermName ?? "No results yet"}
          icon={TrendingUp}
          tone="sky"
        />
        <StatCard
          label="Class position"
          value={
            student.latestClassPosition ? `#${student.latestClassPosition}` : "-"
          }
          hint="Most recent term"
          icon={GraduationCap}
          tone="violet"
        />
        <StatCard
          label="Guardians linked"
          value={parentLinks.length}
          hint="Family contacts"
          icon={Users}
          tone="amber"
        />
      </div>

      {/* ===============================================
          GUARDIANS + PAYMENTS
          =============================================== */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <h3 className="text-[15px] font-semibold text-surface-900">
              Guardians & contacts
            </h3>
            {canEdit && (
              <Button
                variant="ghost"
                size="sm"
                icon={Plus}
                onClick={() => navigate(`/admin/parents/new?studentId=${student.id}`)}
              >
                Link
              </Button>
            )}
          </div>

          {parentLinks.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={Users}
                title="No guardians linked"
                description="Add a parent or guardian to enable billing and report notifications."
              />
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {parentLinks.map((p) => (
                <div
                  key={p.parentUserId}
                  className="px-6 py-4 flex items-center gap-3"
                >
                  <Avatar
                    name={p.fullName}
                    src={p.profilePictureUrl}
                    size="md"
                  />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-[13px] font-medium text-surface-800">
                        {p.fullName}
                      </p>
                      {p.isPrimaryContact && (
                        <Badge variant="emerald">Primary</Badge>
                      )}
                    </div>
                    <p className="text-[11px] text-surface-400 mt-0.5">
                      {p.relationship}
                      {p.phoneNumber && ` - ${p.phoneNumber}`}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <h3 className="text-[15px] font-semibold text-surface-900">
              Recent payments
            </h3>
            <Button
              variant="ghost"
              size="sm"
              iconRight={Receipt}
              onClick={() => navigate("/admin/finance/payments")}
            >
              All
            </Button>
          </div>

          {payments.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={Receipt}
                title="No payments yet"
                description="Payments made by or for this student will appear here."
              />
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {payments.map((p) => (
                <div
                  key={p.id}
                  className="px-6 py-3.5 flex items-center gap-3"
                >
                  <div className="w-9 h-9 rounded-xl bg-brand-50 grid place-items-center shrink-0">
                    <Receipt className="w-4 h-4 text-brand-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-surface-800 font-mono">
                      {p.referenceNumber}
                    </p>
                    <p className="text-[11px] text-surface-400 mt-0.5">
                      {p.termName} - {p.gateway}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-[13px] font-semibold text-surface-900 tabular-nums">
                      {formatNaira(p.amountPaid ?? 0)}
                    </p>
                    <p className="text-[11px] text-surface-400">
                      {formatDate(p.paymentDate)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      {/* ===============================================
          REPORT CARDS CALLOUT
          =============================================== */}
      <Card className="bg-gradient-to-br from-violet-50 to-violet-100/30 border-violet-100">
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-white grid place-items-center shadow-sm">
            <FileText className="w-5 h-5 text-violet-600" />
          </div>
          <div className="flex-1">
            <h3 className="text-[14px] font-semibold text-surface-900">
              Report cards
            </h3>
            <p className="text-[12px] text-surface-600 mt-0.5">
              View or download any term report for this student.
            </p>
          </div>
          <Button
            variant="secondary"
            onClick={() => navigate(`/admin/report-cards`)}
          >
            Open results
          </Button>
        </div>
      </Card>

      {/* ===============================================
          EDIT MODAL — editors only
          =============================================== */}
      {canEdit && editOpen && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !saving && setEditOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col overflow-hidden">
              <div className="p-5 flex items-center justify-between border-b border-surface-100 shrink-0">
                <h2 className="text-[15px] font-semibold text-surface-900">
                  Edit student
                </h2>
                <button
                  type="button"
                  onClick={() => !saving && setEditOpen(false)}
                  className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <div className="flex-1 overflow-y-auto p-5 space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <FormField label="First name" required>
                    <Input
                      value={editForm.firstName}
                      onChange={(e) =>
                        setEditForm((f) => ({
                          ...f,
                          firstName: e.target.value,
                        }))
                      }
                    />
                  </FormField>
                  <FormField label="Last name" required>
                    <Input
                      value={editForm.lastName}
                      onChange={(e) =>
                        setEditForm((f) => ({
                          ...f,
                          lastName: e.target.value,
                        }))
                      }
                    />
                  </FormField>
                </div>

                <FormField label="Phone">
                  <Input
                    value={editForm.phoneNumber}
                    onChange={(e) =>
                      setEditForm((f) => ({
                        ...f,
                        phoneNumber: e.target.value,
                      }))
                    }
                    placeholder="080..."
                  />
                </FormField>

                <FormField label="Class" required>
                  <Select
                    value={editForm.schoolClassId}
                    onChange={(e) => {
                      const cid = e.target.value;
                      const cls = classes.find((c) => c.id === cid);
                      const isSeniorNow =
                        cls?.academicSection === "SeniorSecondary";
                      setEditForm((f) => ({
                        ...f,
                        schoolClassId: cid,
                        stream: isSeniorNow ? f.stream : "",
                      }));
                    }}
                    options={classes.map((c) => ({
                      value: c.id,
                      label: c.name,
                    }))}
                  />
                </FormField>

                {editIsSenior && (
                  <FormField label="Stream" required>
                    <Select
                      value={editForm.stream}
                      onChange={(e) =>
                        setEditForm((f) => ({
                          ...f,
                          stream: e.target.value,
                        }))
                      }
                      placeholder="Choose a stream"
                      options={STREAMS.map((s) => ({
                        value: s,
                        label: s,
                      }))}
                    />
                  </FormField>
                )}

                <div className="flex items-center gap-2 pt-2">
                  <input
                    id="isActive"
                    type="checkbox"
                    checked={editForm.isActive}
                    onChange={(e) =>
                      setEditForm((f) => ({
                        ...f,
                        isActive: e.target.checked,
                      }))
                    }
                    className="w-4 h-4 rounded"
                  />
                  <label
                    htmlFor="isActive"
                    className="text-[13px] text-surface-700"
                  >
                    Student is active
                  </label>
                </div>
              </div>

              <div className="p-5 border-t border-surface-100 flex justify-end gap-3 shrink-0">
                <Button
                  variant="secondary"
                  onClick={() => setEditOpen(false)}
                  disabled={saving}
                >
                  Cancel
                </Button>
                <Button icon={Pencil} loading={saving} onClick={saveEdit}>
                  Save changes
                </Button>
              </div>
            </div>
          </div>
        </>
      )}

      {/* ===============================================
          DELETE MODAL — editors only
          =============================================== */}
      {canEdit && deleteOpen && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !deleting && setDeleteOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
              <div className="p-6">
                <div className="w-12 h-12 rounded-2xl bg-flame-50 grid place-items-center mb-4">
                  <AlertTriangle className="w-5 h-5 text-flame-600" />
                </div>
                <h2 className="text-[16px] font-semibold text-surface-900">
                  Delete this student?
                </h2>
                <p className="mt-2 text-[13px] text-surface-600 leading-relaxed">
                  <strong className="text-surface-900">
                    {student.firstName} {student.lastName}
                  </strong>{" "}
                  will be soft-deleted. Their record, payments, and results are
                  kept — but they'll no longer appear in lists or be able to
                  log in.
                </p>
              </div>

              <div className="p-5 border-t border-surface-100 flex justify-end gap-3">
                <Button
                  variant="secondary"
                  onClick={() => setDeleteOpen(false)}
                  disabled={deleting}
                >
                  Cancel
                </Button>
                <Button
                  variant="danger"
                  icon={Trash2}
                  loading={deleting}
                  onClick={confirmDelete}
                >
                  Yes, delete
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
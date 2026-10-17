import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  Users, Wallet, GraduationCap, ClipboardCheck, Calendar,
  CircleDot, AlertCircle, UserPlus, Receipt, BookOpen,
  CheckCircle2, ArrowRight, BookMarked, TrendingUp,
} from "lucide-react";
import { useAuth } from "../../hooks/useAuth";
import { useRole } from "../../hooks/useRole";
import PageHeader from "../../components/PageHeader";
import { Button, Card, StatCard, Skeleton, Badge } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse, Paginated } from "../../types/common";

// ==========================================================
// Shared: interface for teacher assignment rows
// ==========================================================
interface Assignment {
  classId: string;
  className: string;
  section: number;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  studentCount: number;
  scoredCount: number;
  submissionStatus: string;
}

export default function Dashboard() {
  const { user } = useAuth();
  const { role, isScoreEntry, isAdmin, isFinance } = useRole();

  const greeting = (() => {
    const h = new Date().getHours();
    if (h < 12) return "Good morning";
    if (h < 17) return "Good afternoon";
    return "Good evening";
  })();

  // Only ACTUAL teachers get the teacher dashboard.
  // hasRole() returns true for SuperAdmin on every group — correct for
  // "can access" checks, wrong for "is exactly this role?" checks.
  const isActualTeacher = role === "Teacher" || role === "FormTeacher";

  if (isActualTeacher) {
    return (
      <TeacherDashboard
        greeting={greeting}
        firstName={user?.firstName ?? ""}
      />
    );
  }

  // Everyone else — SuperAdmin, Principal, Headmaster/Headmistress,
  // VicePrincipal, SchoolAdmin, Accountant, Registrar — sees the admin view.
  return (
    <AdminDashboard
      greeting={greeting}
      firstName={user?.firstName ?? ""}
      canManageStudents={isAdmin}
      canManageFinance={isFinance}
      canManageScores={isScoreEntry}
      role={role}
    />
  );
}

// ==========================================================
// TEACHER DASHBOARD
// ==========================================================
function TeacherDashboard({
  greeting,
  firstName,
}: {
  greeting: string;
  firstName: string;
}) {
  const navigate = useNavigate();
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<Assignment[]>>(
          "/termsubjectresult/my-assignments"
        );
        if (data.success) setAssignments(data.data ?? []);
      } catch {
        /* silent */
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  // ---- Derived stats ----
  const uniqueClasses = new Set(assignments.map((a) => a.classId)).size;
  const uniqueSubjects = new Set(assignments.map((a) => a.subjectId)).size;
  const totalStudents = assignments.reduce((s, a) => s + a.studentCount, 0);
  const pendingCount = assignments.filter(
    (a) => a.submissionStatus !== "Submitted"
  ).length;
  const submittedCount = assignments.filter(
    (a) => a.submissionStatus === "Submitted"
  ).length;

  // Show the "needs attention" assignments first
  const needsAttention = assignments
    .filter((a) => a.submissionStatus !== "Submitted")
    .slice(0, 4);

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow="Teacher overview"
        title={`${greeting}, ${firstName}`}
        description="Your classes, subjects, and pending score entries."
        actions={
          <>
            <Button
              variant="secondary"
              icon={ClipboardCheck}
              onClick={() => navigate("/admin/my-assignments")}
            >
              My Assignments
            </Button>
            <Button
              icon={CircleDot}
              onClick={() => navigate("/admin/score-entry")}
            >
              Enter Scores
            </Button>
          </>
        }
      />

      {/* KPI grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 lg:gap-6">
        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} padding="md">
              <Skeleton className="w-10 h-10 rounded-xl mb-5" />
              <Skeleton className="h-3 w-24 mb-2" />
              <Skeleton className="h-6 w-32" />
            </Card>
          ))
        ) : (
          <>
            <StatCard
              label="My Classes"
              value={uniqueClasses}
              hint="Assigned this term"
              icon={GraduationCap}
              tone="brand"
            />
            <StatCard
              label="My Subjects"
              value={uniqueSubjects}
              hint="Across all classes"
              icon={BookMarked}
              tone="sky"
            />
            <StatCard
              label="My Students"
              value={totalStudents}
              hint="In your classes"
              icon={Users}
              tone="violet"
            />
            <StatCard
              label="Pending Scores"
              value={pendingCount}
              hint={
                pendingCount === 0
                  ? "All submitted"
                  : `${submittedCount} already submitted`
              }
              icon={ClipboardCheck}
              tone={pendingCount === 0 ? "emerald" : "amber"}
            />
          </>
        )}
      </div>

      {/* Needs attention */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2" padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <div>
              <h2 className="text-[15px] font-semibold text-surface-900">
                Classes needing scores
              </h2>
              <p className="text-[12px] text-surface-400 mt-0.5">
                The classes and subjects where you haven't submitted scores yet.
              </p>
            </div>
            <Link
              to="/admin/my-assignments"
              className="text-[12px] font-semibold text-brand-600 hover:text-brand-700 inline-flex items-center gap-1"
            >
              All assignments
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>

          {loading ? (
            <div className="p-6 space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-14" />
              ))}
            </div>
          ) : needsAttention.length === 0 ? (
            <div className="px-6 pb-6">
              <div className="flex flex-col items-center justify-center text-center py-10">
                <div className="w-14 h-14 rounded-2xl bg-brand-50 grid place-items-center mb-4">
                  <CheckCircle2 className="w-6 h-6 text-brand-600" />
                </div>
                <p className="text-[14px] font-semibold text-surface-800">
                  You're all caught up
                </p>
                <p className="text-[12px] text-surface-400 mt-1">
                  Every assigned class and subject has submitted scores.
                </p>
              </div>
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {needsAttention.map((a) => {
                const pct =
                  a.studentCount > 0
                    ? Math.round((a.scoredCount / a.studentCount) * 100)
                    : 0;
                return (
                  <button
                    key={`${a.classId}-${a.subjectId}`}
                    type="button"
                    onClick={() =>
                      navigate(
                        `/admin/score-entry?classId=${a.classId}&subjectId=${a.subjectId}`
                      )
                    }
                    className="w-full text-left px-6 py-4 hover:bg-surface-50 transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      <div className="w-10 h-10 rounded-xl bg-brand-50 grid place-items-center shrink-0">
                        <ClipboardCheck className="w-4 h-4 text-brand-600" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <p className="text-[13px] font-semibold text-surface-900">
                            {a.className}
                          </p>
                          <Badge variant="sky">{a.subjectCode}</Badge>
                          <Badge
                            variant={
                              a.submissionStatus === "Draft"
                                ? "amber"
                                : "neutral"
                            }
                          >
                            {a.submissionStatus === "Draft"
                              ? "Draft saved"
                              : "Not started"}
                          </Badge>
                        </div>
                        <p className="text-[12px] text-surface-500 mt-0.5">
                          {a.subjectName} - {a.scoredCount} of{" "}
                          {a.studentCount} scored
                        </p>
                        <div className="mt-2 h-1.5 bg-surface-100 rounded-full overflow-hidden max-w-xs">
                          <div
                            className="h-full bg-brand-500 rounded-full transition-all"
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                      </div>
                      <ArrowRight className="w-4 h-4 text-surface-300 shrink-0" />
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </Card>

        {/* Quick links column */}
        <Card>
          <h2 className="text-[15px] font-semibold text-surface-900 mb-5">
            Quick access
          </h2>
          <div className="space-y-2">
            {[
              {
                label: "My classes & students",
                to: "/admin/classes",
                icon: GraduationCap,
              },
              {
                label: "Enter scores",
                to: "/admin/score-entry",
                icon: ClipboardCheck,
              },
              {
                label: "My assignments",
                to: "/admin/my-assignments",
                icon: BookOpen,
              },
              {
                label: "Report cards",
                to: "/admin/report-cards",
                icon: TrendingUp,
              },
            ].map((item) => {
              const Icon = item.icon;
              return (
                <button
                  key={item.label}
                  type="button"
                  onClick={() => navigate(item.to)}
                  className="w-full text-left p-3 rounded-xl border border-surface-100 hover:border-brand-200 hover:bg-brand-50/40 transition-colors text-[13px] font-medium text-surface-800 inline-flex items-center gap-3"
                >
                  <Icon className="w-4 h-4 text-brand-600 shrink-0" />
                  {item.label}
                </button>
              );
            })}
          </div>
        </Card>
      </div>
    </div>
  );
}

// ==========================================================
// ADMIN / LEADERSHIP DASHBOARD
// ==========================================================
function AdminDashboard({
  greeting,
  firstName,
  canManageStudents,
  canManageFinance,
  canManageScores,
  role,
}: {
  greeting: string;
  firstName: string;
  canManageStudents: boolean;
  canManageFinance: boolean;
  canManageScores: boolean;
  role: string | undefined;
}) {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [studentCount, setStudentCount] = useState<number | null>(null);

  useEffect(() => {
    if (!canManageStudents) {
      setLoading(false);
      return;
    }
    (async () => {
      try {
        const res = await api.get<ApiResponse<Paginated<unknown>>>(
          "/student?pageNumber=1&pageSize=1"
        );
        setStudentCount(res.data.data.totalCount);
      } catch {
        /* silent */
      } finally {
        setLoading(false);
      }
    })();
  }, [canManageStudents]);

  // Build the quick-actions list based on permissions
  const quickActions = [
    canManageStudents && {
      label: "Add new student",
      icon: UserPlus,
      to: "/admin/students/new",
    },
    canManageFinance && {
      label: "Record cash payment",
      icon: Receipt,
      to: "/admin/finance/record-cash",
    },
    // canManageScores && {
    //   label: "Publish results",
    //   icon: CheckCircle2,
    //   to: "/admin/results-approval",
    // },
    {
      label: "Academic sessions",
      icon: BookOpen,
      to: "/admin/sessions",
    },
  ].filter(Boolean) as {
    label: string;
    icon: typeof Users;
    to: string;
  }[];

  const eyebrow =
    role === "SuperAdmin"
      ? "Super Admin overview"
      : role === "Principal" || role === "Headmaster" || role === "Headmistress"
      ? "Leadership overview"
      : role === "Accountant"
      ? "Finance overview"
      : role === "Registrar"
      ? "Registrar overview"
      : "Overview";

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow={eyebrow}
        title={`${greeting}, ${firstName}`}
        description="Here's what's happening across the school today."
        actions={
          <>
            <Button
              variant="secondary"
              icon={Calendar}
              onClick={() => navigate("/admin/sessions")}
            >
              Current Term
            </Button>
            <Button
              icon={CircleDot}
              onClick={() =>
                navigate(
                  canManageStudents
                    ? "/admin/students/new"
                    : "/admin/results-approval"
                )
              }
            >
              Quick action
            </Button>
          </>
        }
      />

      {/* KPI grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 lg:gap-6">
        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} padding="md">
              <Skeleton className="w-10 h-10 rounded-xl mb-5" />
              <Skeleton className="h-3 w-24 mb-2" />
              <Skeleton className="h-6 w-32" />
            </Card>
          ))
        ) : (
          <>
            <StatCard
              label="Total Students"
              value={studentCount?.toLocaleString() ?? "-"}
              hint="Active enrollments"
              icon={Users}
              tone="brand"
            />
            <StatCard
              label="Fees Collected"
              value="-"
              hint="Term to date"
              icon={Wallet}
              tone="sky"
            />
            <StatCard
              label="Classes"
              value="-"
              hint="Across all sections"
              icon={GraduationCap}
              tone="violet"
            />
            <StatCard
              label="Pending Results"
              value="-"
              hint="Awaiting approval"
              icon={ClipboardCheck}
              tone="amber"
            />
          </>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <h2 className="text-[15px] font-semibold text-surface-900">
            Recent activity
          </h2>
          <p className="text-[12px] text-surface-400 mb-5">
            Latest events across the school
          </p>
          <div className="py-8 text-center">
            <p className="text-[13px] text-surface-400">
              No activity to display yet.
            </p>
          </div>
        </Card>

        <Card>
          <h2 className="text-[15px] font-semibold text-surface-900 mb-5">
            Quick actions
          </h2>
          <div className="space-y-2">
            {quickActions.map((action) => {
              const Icon = action.icon;
              return (
                <button
                  key={action.label}
                  type="button"
                  onClick={() => navigate(action.to)}
                  className="w-full text-left p-3 rounded-xl border border-surface-100 hover:border-brand-200 hover:bg-brand-50/40 transition-colors text-[13px] font-medium text-surface-800 inline-flex items-center gap-3"
                >
                  <Icon className="w-4 h-4 text-brand-600 shrink-0" />
                  {action.label}
                </button>
              );
            })}
          </div>
        </Card>
      </div>

      <div className="bg-gradient-to-br from-brand-50 to-brand-100/50 rounded-2xl border border-brand-100 p-6 flex flex-col sm:flex-row sm:items-center gap-4">
        <div className="w-11 h-11 rounded-xl bg-white grid place-items-center shadow-sm shrink-0">
          <AlertCircle className="w-5 h-5 text-brand-600" />
        </div>
        <div className="flex-1">
          <p className="text-[14px] font-semibold text-surface-900">
            Keep your data fresh
          </p>
          <p className="text-[12px] text-surface-600 mt-0.5">
            Add students, record payments, and update sessions to see accurate
            figures.
          </p>
        </div>
        <Button
          variant="secondary"
          onClick={() => navigate("/admin/sessions")}
        >
          Learn more
        </Button>
      </div>
    </div>
  );
}
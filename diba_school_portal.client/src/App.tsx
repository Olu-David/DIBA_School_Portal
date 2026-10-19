import { lazy, Suspense } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ToastProvider } from "./hooks/useToast";
import ProtectedRoute from "./components/ProtectedRoute";
import {
  STAFF_ROLES,
  FINANCE_ROLES,
  TEACHING_ROLES,
  ENQUIRY_ROLES,
  AUDIT_ROLES,
  ADMIN_ROLES,
  RESULT_VIEWERS,
} from "./lib/roles";

// ==========================================================
// Layouts
// ==========================================================
import PublicLayout from "./layouts/PublicLayout";
import AuthLayout from "./layouts/AuthLayout";
import AdminLayout from "./layouts/AdminLayout";
import StudentLayout from "./layouts/StudentLayout";
import ParentLayout from "./layouts/ParentLayout";

// ==========================================================
// Public pages
// ==========================================================
import HomePage from "./pages/public/HomePage";
import AboutPage from "./pages/public/AboutPage";
import AcademicsPage from "./pages/public/AcademicsPage";
import AdmissionsPage from "./pages/public/AdmissionsPage";
import ContactPage from "./pages/public/ContactPage";

// ==========================================================
// Auth pages
// ==========================================================
import StaffLogin from "./pages/auth/StaffLogin";
import StudentLogin from "./pages/auth/StudentLogin";
import ParentLogin from "./pages/auth/ParentLogin";

// ==========================================================
// Admin pages — lazy
// ==========================================================
const Dashboard = lazy(() => import("./pages/admin/Dashboard"));
const StudentsPage = lazy(() => import("./pages/admin/StudentsPage"));
const NewStudentPage = lazy(() => import("./pages/admin/NewStudentPage"));
const StudentDetailPage = lazy(() => import("./pages/admin/StudentDetailPage"));
const TeachersPage = lazy(() => import("./pages/admin/TeachersPage"));
const TeacherDetailPage = lazy(() => import("./pages/admin/TeacherDetailPage"));
const ParentsPage = lazy(() => import("./pages/admin/ParentsPage"));
const NewParentPage = lazy(() => import("./pages/admin/NewParentPage"));
const ClassesPage = lazy(() => import("./pages/admin/ClassesPage"));
const NewClassPage = lazy(() => import("./pages/admin/NewClassPage"));
const ClassDetailPage = lazy(() => import("./pages/admin/ClassDetailPage"));
const SubjectsPage = lazy(() => import("./pages/admin/SubjectsPage"));
const NewSubjectPage = lazy(() => import("./pages/admin/NewSubjectPage"));
const AcademicSessionsPage = lazy(() => import("./pages/admin/AcademicSessionsPage"));
const AuditLogsPage = lazy(() => import("./pages/admin/AuditLogsPage"));
const EnquiriesPage = lazy(() => import("./pages/admin/EnquiriesPage"));
const SchoolResultsPage = lazy(() => import("./pages/admin/SchoolResultsPage"));
const ReportCardsPage = lazy(() => import("./pages/admin/ReportCardsPage"));
const MyAssignmentsPage = lazy(() => import("./pages/admin/MyAssignmentsPage"));
const SubmittedResultsPage = lazy(() => import("./pages/admin/SubmittedResultsPage"));

// ==========================================================
// Finance — lazy
// ==========================================================
const FinanceDashboard = lazy(() => import("./pages/finance/FinanceDashboard"));
const RecordCash = lazy(() => import("./pages/finance/RecordCash"));
const PaymentsListPage = lazy(() => import("./pages/finance/PaymentsListPage"));
const ArrearsPage = lazy(() => import("./pages/finance/ArrearsPage"));
const FeesPage = lazy(() => import("./pages/finance/FeesPage"));
// 👇 NEW
const ExpensesPage = lazy(() => import("./pages/finance/ExpensesPage"));
const RecordExpense = lazy(() => import("./pages/finance/RecordExpense"));

// ==========================================================
// Teacher — lazy
// ==========================================================
const ScoreEntryPage = lazy(() => import("./pages/teacher/ScoreEntryPage"));
const ClassSubjectResultsPage = lazy(
  () => import("./pages/teacher/ClassSubjectResultsPage")
);

// ==========================================================
// Student — lazy
// ==========================================================
const StudentDashboard = lazy(() => import("./pages/student/StudentDashboard"));
const MyBillPage = lazy(() => import("./pages/student/MyBillPage"));
const MyResultsPage = lazy(() => import("./pages/student/MyResultsPage"));
const PaymentCallbackPage = lazy(
  () => import("./pages/student/PaymentCallbackPage")
);

// ==========================================================
// Parent — lazy
// ==========================================================
const ParentDashboard = lazy(() => import("./pages/parent/ParentDashboard"));
const ChildDetailPage = lazy(() => import("./pages/parent/ChildDetailPage"));

// ==========================================================
// Shared — lazy
// ==========================================================
const ProfilePage = lazy(() => import("./pages/shared/ProfilePage"));
const ReportCardViewer = lazy(() => import("./pages/shared/ReportCardViewer"));
const NotificationsPage = lazy(() => import("./pages/shared/NotificationsPage"));

const MANAGERS = [
  "SuperAdmin",
  "Principal",
  "Headmaster",
  "Headmistress",
  "VicePrincipal",
  "SchoolAdmin",
  "Registrar",
] as const;

const CLASSROOM_ROLES = [
  "Teacher",
  "FormTeacher",
  "Principal",
  "VicePrincipal",
  "Headmaster",
  "Headmistress",
  "SchoolAdmin",
] as const;

// 👇 NEW — tightened finance split
// Full finance policy access
const FINANCE_FULL_ROLES = [
  "SuperAdmin",
  "Principal",
  "Accountant",
] as const;

// Cash collection access (Cashier + everyone above)
const FINANCE_CASHIER_ROLES = [
  "SuperAdmin",
  "Principal",
  "Accountant",
  "Cashier",
] as const;

// ==========================================================
// Loading fallback
// ==========================================================
function PageLoader() {
  return (
    <div className="grid place-items-center min-h-screen bg-slate-50">
      <div className="flex flex-col items-center gap-3">
        <div className="w-10 h-10 rounded-full border-2 border-slate-200 border-t-emerald-600 animate-spin" />
        <p className="text-[12px] text-slate-400">Loading…</p>
      </div>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <Suspense fallback={<PageLoader />}>
            <Routes>
              {/* PUBLIC */}
              <Route element={<PublicLayout />}>
                <Route path="/" element={<HomePage />} />
                <Route path="/about" element={<AboutPage />} />
                <Route path="/academics" element={<AcademicsPage />} />
                <Route path="/admissions" element={<AdmissionsPage />} />
                <Route path="/contact" element={<ContactPage />} />
              </Route>

              {/* AUTH */}
              <Route path="/auth" element={<AuthLayout />}>
                <Route index element={<Navigate to="/auth/staff" replace />} />
                <Route path="staff" element={<StaffLogin />} />
                <Route path="student" element={<StudentLogin />} />
                <Route path="parent" element={<ParentLogin />} />
              </Route>

              {/* PAYMENT CALLBACK */}
              <Route
                element={
                  <ProtectedRoute
                    allowedRoles={[
                      "Student",
                      "Parent",
                      "Accountant",
                      "SuperAdmin",
                      "Principal",
                    ]}
                  />
                }
              >
                <Route
                  path="/student/payment/callback"
                  element={<PaymentCallbackPage />}
                />
              </Route>

              {/* ADMIN */}
              <Route element={<ProtectedRoute allowedRoles={STAFF_ROLES} />}>
                <Route path="/admin" element={<AdminLayout />}>
                  <Route index element={<Dashboard />} />
                  <Route path="notifications" element={<NotificationsPage />} />
                  <Route path="profile" element={<ProfilePage />} />

                  {/* CLASSES */}
                  <Route path="classes" element={<ClassesPage />} />
                  <Route
                    element={
                      <ProtectedRoute allowedRoles={[...MANAGERS] as any} />
                    }
                  >
                    <Route path="classes/new" element={<NewClassPage />} />
                  </Route>
                  <Route path="classes/:classId" element={<ClassDetailPage />} />

                  {/* STUDENTS */}
                  <Route path="students" element={<StudentsPage />} />
                  <Route
                    element={
                      <ProtectedRoute allowedRoles={[...MANAGERS] as any} />
                    }
                  >
                    <Route path="students/new" element={<NewStudentPage />} />
                  </Route>
                  <Route path="students/:id" element={<StudentDetailPage />} />

                  {/* TEACHERS */}
                  <Route path="teachers" element={<TeachersPage />} />
                  <Route path="teachers/:id" element={<TeacherDetailPage />} />

                  {/* PARENTS */}
                  <Route path="parents" element={<ParentsPage />} />
                  <Route
                    element={
                      <ProtectedRoute
                        allowedRoles={
                          ["SuperAdmin", "Principal", "Registrar"] as any
                        }
                      />
                    }
                  >
                    <Route path="parents/new" element={<NewParentPage />} />
                  </Route>

                  {/* SUBJECTS */}
                  <Route path="subjects" element={<SubjectsPage />} />
                  <Route element={<ProtectedRoute allowedRoles={ADMIN_ROLES} />}>
                    <Route path="subjects/new" element={<NewSubjectPage />} />
                  </Route>

                  {/* SESSIONS */}
                  <Route element={<ProtectedRoute allowedRoles={ADMIN_ROLES} />}>
                    <Route path="sessions" element={<AcademicSessionsPage />} />
                  </Route>

                  {/* RESULTS */}
                  <Route
                    element={<ProtectedRoute allowedRoles={RESULT_VIEWERS} />}
                  >
                    <Route path="results" element={<SchoolResultsPage />} />
                    <Route path="report-cards" element={<ReportCardsPage />} />
                    <Route
                      path="report-cards/:studentId/:sessionId/:termId"
                      element={<ReportCardViewer />}
                    />
                  </Route>

                  {/* SCORE ENTRY */}
                  <Route
                    element={
                      <ProtectedRoute
                        allowedRoles={[
                          "SuperAdmin",
                          "Principal",
                          "VicePrincipal",
                          "Headmaster",
                          "Headmistress",
                          "Teacher",
                          "FormTeacher",
                        ]}
                      />
                    }
                  >
                    <Route path="score-entry" element={<ScoreEntryPage />} />
                  </Route>

                  {/* MY CLASSROOM */}
                  <Route
                    element={
                      <ProtectedRoute
                        allowedRoles={[...CLASSROOM_ROLES] as any}
                      />
                    }
                  >
                    <Route path="my-assignments" element={<MyAssignmentsPage />} />
                    <Route path="my-results" element={<SubmittedResultsPage />} />
                    <Route
                      path="class-subject-results"
                      element={<ClassSubjectResultsPage />}
                    />
                  </Route>

                  {/* ================================================
                      FINANCE — split into two permission tiers
                      ================================================ */}

                  {/* Full finance policy — SuperAdmin, Principal, Accountant */}
                  <Route
                    element={
                      <ProtectedRoute
                        allowedRoles={[...FINANCE_FULL_ROLES] as any}
                      />
                    }
                  >
                    <Route path="finance" element={<FinanceDashboard />} />
                    <Route path="finance/fees" element={<FeesPage />} />
                    {/* 👇 NEW — expenses only for full finance roles */}
                    <Route path="finance/expenses" element={<ExpensesPage />} />
                    <Route
                      path="finance/record-expense"
                      element={<RecordExpense />}
                    />
                  </Route>

                  {/* Cash collection — Cashier + everyone above */}
                  <Route
                    element={
                      <ProtectedRoute
                        allowedRoles={[...FINANCE_CASHIER_ROLES] as any}
                      />
                    }
                  >
                    <Route
                      path="finance/payments"
                      element={<PaymentsListPage />}
                    />
                    <Route path="finance/arrears" element={<ArrearsPage />} />
                    <Route
                      path="finance/record-cash"
                      element={<RecordCash />}
                    />
                  </Route>

                  {/* ENQUIRIES */}
                  <Route element={<ProtectedRoute allowedRoles={ENQUIRY_ROLES} />}>
                    <Route path="enquiries" element={<EnquiriesPage />} />
                  </Route>

                  {/* AUDIT */}
                  <Route element={<ProtectedRoute allowedRoles={AUDIT_ROLES} />}>
                    <Route path="audit-logs" element={<AuditLogsPage />} />
                  </Route>
                </Route>
              </Route>

              {/* STUDENT */}
              <Route element={<ProtectedRoute allowedRoles={["Student"]} />}>
                <Route path="/student" element={<StudentLayout />}>
                  <Route index element={<StudentDashboard />} />
                  <Route path="bill" element={<MyBillPage />} />
                  <Route path="results" element={<MyResultsPage />} />
                  <Route
                    path="results/:studentId/:sessionId/:termId"
                    element={<ReportCardViewer />}
                  />
                  <Route path="notifications" element={<NotificationsPage />} />
                  <Route path="profile" element={<ProfilePage />} />
                </Route>
              </Route>

              {/* PARENT */}
              <Route element={<ProtectedRoute allowedRoles={["Parent"]} />}>
                <Route path="/parent" element={<ParentLayout />}>
                  <Route index element={<ParentDashboard />} />
                  <Route path="children" element={<ParentDashboard />} />
                  <Route
                    path="children/:studentProfileId"
                    element={<ChildDetailPage />}
                  />
                  <Route
                    path="children/:studentProfileId/results"
                    element={<MyResultsPage />}
                  />
                  <Route
                    path="children/:studentProfileId/payments"
                    element={<MyBillPage />}
                  />
                  <Route
                    path="children/:studentProfileId/report-cards/:sessionId/:termId"
                    element={<ReportCardViewer />}
                  />
                  <Route path="notifications" element={<NotificationsPage />} />
                  <Route path="profile" element={<ProfilePage />} />
                </Route>
              </Route>

              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Suspense>
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
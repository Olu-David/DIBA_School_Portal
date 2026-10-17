import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ToastProvider } from "./hooks/useToast";
import ProtectedRoute from "./components/ProtectedRoute";
import {
  STAFF_ROLES,
  FINANCE_ROLES,
  TEACHING_ROLES,
  LEADERSHIP_ROLES,
  ENQUIRY_ROLES,
  AUDIT_ROLES,
  ADMIN_ROLES,
  RESULT_VIEWERS,
} from "./lib/roles";

// Layouts
import PublicLayout from "./layouts/PublicLayout";
import AuthLayout from "./layouts/AuthLayout";
import AdminLayout from "./layouts/AdminLayout";
import StudentLayout from "./layouts/StudentLayout";
import ParentLayout from "./layouts/ParentLayout";

// Public pages
import HomePage from "./pages/public/HomePage";
import AboutPage from "./pages/public/AboutPage";
import AcademicsPage from "./pages/public/AcademicsPage";
import AdmissionsPage from "./pages/public/AdmissionsPage";
import ContactPage from "./pages/public/ContactPage";

// Auth pages
import StaffLogin from "./pages/auth/StaffLogin";
import StudentLogin from "./pages/auth/StudentLogin";
import ParentLogin from "./pages/auth/ParentLogin";

// Admin pages
import Dashboard from "./pages/admin/Dashboard";
import StudentsPage from "./pages/admin/StudentsPage";
import NewStudentPage from "./pages/admin/NewStudentPage";
import StudentDetailPage from "./pages/admin/StudentDetailPage";
import TeachersPage from "./pages/admin/TeachersPage";
import TeacherDetailPage from "./pages/admin/TeacherDetailPage";
import ParentsPage from "./pages/admin/ParentsPage";
import NewParentPage from "./pages/admin/NewParentPage";
import ClassesPage from "./pages/admin/ClassesPage";
import NewClassPage from "./pages/admin/NewClassPage";
import ClassDetailPage from "./pages/admin/ClassDetailPage";
import SubjectsPage from "./pages/admin/SubjectsPage";
import NewSubjectPage from "./pages/admin/NewSubjectPage";
import AcademicSessionsPage from "./pages/admin/AcademicSessionsPage";
import AuditLogsPage from "./pages/admin/AuditLogsPage";
import EnquiriesPage from "./pages/admin/EnquiriesPage";
import SchoolResultsPage from "./pages/admin/SchoolResultsPage";
import ReportCardsPage from "./pages/admin/ReportCardsPage";
import MyAssignmentsPage from "./pages/admin/MyAssignmentsPage";
import SubmittedResultsPage from "./pages/admin/SubmittedResultsPage";

// Finance
import FinanceDashboard from "./pages/finance/FinanceDashboard";
import RecordCash from "./pages/finance/RecordCash";
import PaymentsListPage from "./pages/finance/PaymentsListPage";
import ArrearsPage from "./pages/finance/ArrearsPage";
import FeesPage from "./pages/finance/FeesPage";

// Teacher
import ScoreEntryPage from "./pages/teacher/ScoreEntryPage";
import ClassSubjectResultsPage from "./pages/teacher/ClassSubjectResultsPage";

// Student
import StudentDashboard from "./pages/student/StudentDashboard";
import MyBillPage from "./pages/student/MyBillPage";
import MyResultsPage from "./pages/student/MyResultsPage";
import PaymentCallbackPage from "./pages/student/PaymentCallbackPage";  

// Parent
import ParentDashboard from "./pages/parent/ParentDashboard";
import ChildDetailPage from "./pages/parent/ChildDetailPage";

// Shared
import ProfilePage from "./pages/shared/ProfilePage";
import ReportCardViewer from "./pages/shared/ReportCardViewer";
import NotificationsPage from "./pages/shared/NotificationsPage";

const MANAGERS = [
  "SuperAdmin",
  "Principal",
  "Headmaster",
  "Headmistress",
  "VicePrincipal",
  "SchoolAdmin",
  "Registrar",
] as const;

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
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

            {/* =====================================================
                PAYMENT CALLBACK — SHARED ACROSS ROLES
                Flutterwave redirects here after checkout.
                Must work for Student, Parent, and Accountant.
                No layout wrapper — it's a full-screen verification page.
                ===================================================== */}
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
              <Route path="/student/payment/callback" element={<PaymentCallbackPage />} />
            </Route>

            {/* ADMIN (any staff) */}
            <Route element={<ProtectedRoute allowedRoles={STAFF_ROLES} />}>
              <Route path="/admin" element={<AdminLayout />}>
                <Route index element={<Dashboard />} />
                <Route path="notifications" element={<NotificationsPage />} />
                <Route path="profile" element={<ProfilePage />} />

                {/* ---------- CLASSES ---------- */}
                <Route path="classes" element={<ClassesPage />} />
                <Route
                  element={<ProtectedRoute allowedRoles={[...MANAGERS] as any} />}
                >
                  <Route path="classes/new" element={<NewClassPage />} />
                </Route>
                <Route path="classes/:classId" element={<ClassDetailPage />} />

                {/* ---------- STUDENTS ---------- */}
                <Route path="students" element={<StudentsPage />} />
                <Route
                  element={<ProtectedRoute allowedRoles={[...MANAGERS] as any} />}
                >
                  <Route path="students/new" element={<NewStudentPage />} />
                </Route>
                <Route path="students/:id" element={<StudentDetailPage />} />

                {/* ---------- TEACHERS ---------- */}
                <Route path="teachers" element={<TeachersPage />} />
                <Route path="teachers/:id" element={<TeacherDetailPage />} />

                {/* ---------- PARENTS ---------- */}
                <Route path="parents" element={<ParentsPage />} />
                <Route
                  element={
                    <ProtectedRoute
                      allowedRoles={["SuperAdmin", "Principal", "Registrar"] as any}
                    />
                  }
                >
                  <Route path="parents/new" element={<NewParentPage />} />
                </Route>

                {/* ---------- SUBJECTS ---------- */}
                <Route path="subjects" element={<SubjectsPage />} />
                <Route element={<ProtectedRoute allowedRoles={ADMIN_ROLES} />}>
                  <Route path="subjects/new" element={<NewSubjectPage />} />
                </Route>

                {/* ---------- SESSIONS ---------- */}
                <Route element={<ProtectedRoute allowedRoles={ADMIN_ROLES} />}>
                  <Route path="sessions" element={<AcademicSessionsPage />} />
                </Route>

                {/* ---------- RESULTS (admin tier) ---------- */}
                <Route element={<ProtectedRoute allowedRoles={RESULT_VIEWERS} />}>
                  <Route path="results" element={<SchoolResultsPage />} />
                  <Route path="report-cards" element={<ReportCardsPage />} />
                  <Route
                    path="report-cards/:studentId/:sessionId/:termId"
                    element={<ReportCardViewer />}
                  />
                </Route>

                {/* ---------- SCORE ENTRY (teachers + leadership) ---------- */}
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

                {/* ---------- TEACHER-ONLY ---------- */}
                <Route element={<ProtectedRoute allowedRoles={TEACHING_ROLES} />}>
                  <Route path="my-assignments" element={<MyAssignmentsPage />} />
                  <Route path="my-results" element={<SubmittedResultsPage />} />
                  <Route
                    path="class-subject-results"
                    element={<ClassSubjectResultsPage />}
                  />
                </Route>

                {/* ---------- FINANCE ---------- */}
                <Route element={<ProtectedRoute allowedRoles={FINANCE_ROLES} />}>
                  <Route path="finance" element={<FinanceDashboard />} />
                  <Route path="finance/payments" element={<PaymentsListPage />} />
                  <Route path="finance/arrears" element={<ArrearsPage />} />
                  <Route path="finance/fees" element={<FeesPage />} />
                  <Route path="finance/record-cash" element={<RecordCash />} />
                </Route>

                {/* ---------- ENQUIRIES ---------- */}
                <Route element={<ProtectedRoute allowedRoles={ENQUIRY_ROLES} />}>
                  <Route path="enquiries" element={<EnquiriesPage />} />
                </Route>

                {/* ---------- AUDIT ---------- */}
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
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
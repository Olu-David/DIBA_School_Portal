import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ToastProvider } from "./hooks/useToast";
import ProtectedRoute from "./components/ProtectedRoute";

// ---------------------------------------------------------
// Layouts
// ---------------------------------------------------------
import AuthLayout from "./layouts/AuthLayout";
import AdminLayout from "./layouts/AdminLayout";
import StudentLayout from "./layouts/StudentLayout";
import ParentLayout from "./layouts/ParentLayout";

// ---------------------------------------------------------
// Auth pages
// ---------------------------------------------------------
import StaffLogin from "./pages/auth/StaffLogin";
import StudentLogin from "./pages/auth/StudentLogin";
import ParentLogin from "./pages/auth/ParentLogin";

// ---------------------------------------------------------
// Admin pages
// ---------------------------------------------------------
import Dashboard from "./pages/admin/Dashboard";
import StudentsPage from "./pages/admin/StudentsPage";
import NewStudentPage from "./pages/admin/NewStudentPage";
import StudentDetailPage from "./pages/admin/StudentDetailPage";
import ClassesPage from "./pages/admin/ClassesPage";
import ClassDetailPage from "./pages/admin/ClassDetailPage";
import SubjectsPage from "./pages/admin/SubjectsPage";
import AcademicSessionsPage from "./pages/admin/AcademicSessionsPage";
import AuditLogsPage from "./pages/admin/AuditLogsPage";

// ---------------------------------------------------------
// Finance pages
// ---------------------------------------------------------
import FinanceDashboard from "./pages/finance/FinanceDashboard";
import RecordCash from "./pages/finance/RecordCash";
import PaymentsListPage from "./pages/finance/PaymentsListPage";
import ArrearsPage from "./pages/finance/ArrearsPage";
import FeesPage from "./pages/finance/FeesPage";

// ---------------------------------------------------------
// Teacher pages
// ---------------------------------------------------------
import ScoreEntryPage from "./pages/teacher/ScoreEntryPage";

// ---------------------------------------------------------
// Student pages
// ---------------------------------------------------------
import StudentDashboard from "./pages/student/StudentDashboard";
import MyBillPage from "./pages/student/MyBillPage";
import MyResultsPage from "./pages/student/MyResultsPage";

// ---------------------------------------------------------
// Parent pages
// ---------------------------------------------------------
import ParentDashboard from "./pages/parent/ParentDashboard";
import ChildDetailPage from "./pages/parent/ChildDetailPage";

// ---------------------------------------------------------
// Shared pages
// ---------------------------------------------------------
import ProfilePage from "./pages/shared/ProfilePage";
import ReportCardViewer from "./pages/shared/ReportCardViewer";

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <Routes>
            {/* ============================================= */}
            {/* Public — auth                               */}
            {/* ============================================= */}
            <Route path="/auth" element={<AuthLayout />}>
              <Route index element={<Navigate to="/auth/staff" replace />} />
              <Route path="staff" element={<StaffLogin />} />
              <Route path="student" element={<StudentLogin />} />
              <Route path="parent" element={<ParentLogin />} />
            </Route>

            {/* ============================================= */}
            {/* Admin / staff area                          */}
            {/* ============================================= */}
            <Route element={<ProtectedRoute />}>
              <Route path="/admin" element={<AdminLayout />}>
                <Route index element={<Dashboard />} />

                {/* Students */}
                <Route path="students" element={<StudentsPage />} />
                <Route path="students/new" element={<NewStudentPage />} />
                <Route path="students/:id" element={<StudentDetailPage />} />

                {/* Classes */}
                <Route path="classes" element={<ClassesPage />} />
                <Route path="classes/:classId" element={<ClassDetailPage />} />

                {/* Other academics */}
                <Route path="subjects" element={<SubjectsPage />} />
                <Route path="sessions" element={<AcademicSessionsPage />} />
                <Route path="audit-logs" element={<AuditLogsPage />} />

                {/* Finance */}
                <Route path="finance" element={<FinanceDashboard />} />
                <Route path="finance/record-cash" element={<RecordCash />} />
                <Route path="finance/payments" element={<PaymentsListPage />} />
                <Route path="finance/arrears" element={<ArrearsPage />} />
                <Route path="finance/fees" element={<FeesPage />} />

                {/* Teaching */}
                <Route path="score-entry" element={<ScoreEntryPage />} />

                {/* Shared (within admin shell) */}
                <Route path="profile" element={<ProfilePage />} />
                <Route
                  path="report-cards/:studentId/:sessionId/:termId"
                  element={<ReportCardViewer />}
                />
              </Route>
            </Route>

            {/* ============================================= */}
            {/* Student portal                              */}
            {/* ============================================= */}
            <Route element={<ProtectedRoute allowedRoles={["Student"]} />}>
              <Route path="/student" element={<StudentLayout />}>
                <Route index element={<StudentDashboard />} />
                <Route path="bill" element={<MyBillPage />} />
                <Route path="results" element={<MyResultsPage />} />
                <Route path="profile" element={<ProfilePage />} />
              </Route>
            </Route>

            {/* ============================================= */}
            {/* Parent portal                               */}
            {/* ============================================= */}
            <Route element={<ProtectedRoute allowedRoles={["Parent"]} />}>
              <Route path="/parent" element={<ParentLayout />}>
                <Route index element={<ParentDashboard />} />
                <Route
                  path="children/:studentProfileId"
                  element={<ChildDetailPage />}
                />
                <Route path="profile" element={<ProfilePage />} />
              </Route>
            </Route>

            {/* ============================================= */}
            {/* Fallbacks                                   */}
            {/* ============================================= */}
            <Route path="/" element={<Navigate to="/admin" replace />} />
            <Route path="*" element={<Navigate to="/admin" replace />} />
          </Routes>
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
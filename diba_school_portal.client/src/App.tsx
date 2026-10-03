import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import AdminLayout from "./layouts/AdminLayout";
import AuthLayout from "./layouts/AuthLayout";

import StaffLogin from "./pages/auth/StaffLogin";
import StudentLogin from "./pages/auth/StudentLogin";
import ParentLogin from "./pages/auth/ParentLogin";

import Dashboard from "./pages/Dashboard";
import FinanceDashboard from "./pages/finance/FinanceDashboard";
import AuditLogsPage from "./pages/audit/AuditLogsPage";

const Soon = ({ title }: { title: string }) => (
  <div className="bg-white rounded-2xl border border-slate-100 p-8">
    <h1 className="text-2xl font-semibold text-slate-900">{title}</h1>
  </div>
);

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/auth" element={<AuthLayout />}>
            <Route index element={<Navigate to="/auth/staff" replace />} />
            <Route path="staff" element={<StaffLogin />} />
            <Route path="student" element={<StudentLogin />} />
            <Route path="parent" element={<ParentLogin />} />
          </Route>

          <Route
            element={
              <ProtectedRoute
                allowedRoles={[
                  "SuperAdmin",
                  "Principal",
                  "Headmaster",
                  "Headmistress",
                  "VicePrincipal",
                  "SchoolAdmin",
                  "Accountant",
                  "Registrar",
                  "Teacher",
                  "FormTeacher",
                ]}
              />
            }
          >
            <Route path="/admin" element={<AdminLayout />}>
              <Route index element={<Dashboard />} />
              <Route path="students" element={<Soon title="Students" />} />
              <Route path="classes" element={<Soon title="Classes" />} />
              <Route path="finance" element={<FinanceDashboard />} />
              <Route path="audit-logs" element={<AuditLogsPage />} />
              <Route path="settings" element={<Soon title="Settings" />} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute allowedRoles={["Student"]} />}>
            <Route path="/student" element={<Soon title="Student Portal" />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={["Parent"]} />}>
            <Route path="/parent" element={<Soon title="Parent Portal" />} />
          </Route>

          <Route path="/" element={<Navigate to="/admin" replace />} />
          <Route path="*" element={<Navigate to="/admin" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
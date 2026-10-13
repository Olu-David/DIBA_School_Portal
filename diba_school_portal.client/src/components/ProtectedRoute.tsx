import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { hasRole } from "../lib/roles";
import type { UserRole } from "../types/auth";

interface Props {
  /** If omitted, any logged-in user is allowed */
  allowedRoles?: UserRole[];
}

export default function ProtectedRoute({ allowedRoles }: Props) {
  const { isAuthenticated, user } = useAuth();

  // Not logged in → home page (NOT login)
  if (!isAuthenticated || !user) {
    return <Navigate to="/" replace />;
  }

  // Logged in but wrong role → home page (NOT login)
  if (allowedRoles && !hasRole(user.role, allowedRoles)) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { hasRole } from "../lib/roles";
import type { UserRole } from "../types/auth";

interface Props {
  /** If omitted, any logged-in user is allowed */
  allowedRoles?: UserRole[];
  /** Where to redirect on failure. Default: "/" (home) */
  redirectTo?: string;
}

export default function ProtectedRoute({
  allowedRoles,
  redirectTo = "/",
}: Props) {
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated || !user) {
    return <Navigate to={redirectTo} replace />;
  }

  if (allowedRoles && !hasRole(user.role, allowedRoles)) {
    return <Navigate to={redirectTo} replace />;
  }

  return <Outlet />;
}
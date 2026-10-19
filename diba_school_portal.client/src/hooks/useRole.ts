import { useAuth } from "./useAuth";
import {
  hasRole, isStaff, ADMIN_ROLES, FINANCE_ROLES, LEADERSHIP_ROLES,
  AUDIT_ROLES, PEOPLE_ROLES, REGISTRAR_ROLES, ENQUIRY_ROLES,
  TEACHING_ROLES, SCORE_ROLES, RESULT_VIEWERS,
  FINANCE_FULL_ROLES, FINANCE_CASHIER_ROLES,
} from "../lib/roles";

export function useRole() {
  const { user } = useAuth();
  const role = user?.role;

  return {
    role,
    isStaff: isStaff(role),
    isSuperAdmin: role === "SuperAdmin",
    isLeadership: hasRole(role, LEADERSHIP_ROLES),
    isAdmin: hasRole(role, ADMIN_ROLES),
    isFinance: hasRole(role, FINANCE_ROLES),
    isFinanceFull: hasRole(role, FINANCE_FULL_ROLES),
    isCashier: hasRole(role, FINANCE_CASHIER_ROLES),
    isAudit: hasRole(role, AUDIT_ROLES),
    isPeople: hasRole(role, PEOPLE_ROLES),
    isRegistrar: hasRole(role, REGISTRAR_ROLES),
    isEnquiry: hasRole(role, ENQUIRY_ROLES),
    isTeacher: hasRole(role, TEACHING_ROLES),
    isScoreEntry: hasRole(role, SCORE_ROLES),
    isResultViewer: hasRole(role, RESULT_VIEWERS),
    can: (allowed: Parameters<typeof hasRole>[1]) => hasRole(role, allowed),
  };
}
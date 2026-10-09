import type { UserRole } from "../types/auth";

// ==========================================================
// ROLE GROUPS — map 1:1 to backend [Authorize(Roles=...)]
// ==========================================================

/** Every staff member who logs in through /admin */
export const STAFF_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
  "SchoolAdmin", "Accountant", "Registrar", "Teacher", "FormTeacher",
];

/** School leadership — Principal-tier and above */
export const LEADERSHIP_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
];

/** Can manage classes, subjects, sessions, fees */
export const ADMIN_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress",
  "VicePrincipal", "SchoolAdmin",
];

/** Finance access — matches AccountController */
export const FINANCE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "VicePrincipal", "Accountant",
];

/** Who can see audit logs */
export const AUDIT_ROLES: UserRole[] = ["SuperAdmin", "Principal"];

/** Who can manage students, staff, parents */
export const PEOPLE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress",
  "VicePrincipal", "SchoolAdmin", "Registrar",
];

/** Who can create accounts / link parents */
export const REGISTRAR_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Registrar",
];

/** Who can see / manage enquiries */
export const ENQUIRY_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Registrar",
];

/** Teaching roles */
export const TEACHING_ROLES: UserRole[] = ["Teacher", "FormTeacher"];

/** Who can enter / modify scores */
export const SCORE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "VicePrincipal", "Headmaster", "Headmistress",
  "Teacher", "FormTeacher",
];

/** Who can see all school results */
export const RESULT_VIEWERS: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress",
  "VicePrincipal", "SchoolAdmin", "Teacher", "FormTeacher",
];

// ==========================================================
// HELPERS
// ==========================================================

export const hasRole = (
  userRole: UserRole | undefined,
  allowed: UserRole[]
): boolean => {
  if (!userRole) return false;
  // SuperAdmin bypasses every check
  if (userRole === "SuperAdmin") return true;
  return allowed.includes(userRole);
};

/** True if user is any kind of staff (can access /admin) */
export const isStaff = (role: UserRole | undefined): boolean =>
  !!role && STAFF_ROLES.includes(role);

/** Pretty role label */
export const roleLabel = (role: UserRole | undefined): string => {
  if (!role) return "Guest";
  const labels: Record<UserRole, string> = {
    SuperAdmin: "Super Admin",
    Principal: "Principal",
    Headmaster: "Headmaster",
    Headmistress: "Headmistress",
    VicePrincipal: "Vice Principal",
    SchoolAdmin: "School Admin",
    Accountant: "Accountant",
    Registrar: "Registrar",
    Teacher: "Teacher",
    FormTeacher: "Form Teacher",
    Student: "Student",
    Parent: "Parent",
  };
  return labels[role] ?? role;
};

/** Badge tone per role */
export const roleTone = (role: UserRole | undefined):
  "emerald" | "sky" | "violet" | "amber" | "rose" | "neutral" => {
  if (!role) return "neutral";
  switch (role) {
    case "SuperAdmin": return "rose";
    case "Principal":
    case "Headmaster":
    case "Headmistress": return "emerald";
    case "VicePrincipal":
    case "SchoolAdmin": return "violet";
    case "Accountant": return "amber";
    case "Registrar": return "sky";
    case "Teacher":
    case "FormTeacher": return "sky";
    case "Student": return "emerald";
    case "Parent": return "violet";
    default: return "neutral";
  }
};
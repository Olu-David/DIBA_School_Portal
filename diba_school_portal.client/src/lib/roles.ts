import type { UserRole } from "../types/auth";

// ==========================================================
// ROLE GROUPS — map 1:1 to backend [Authorize(Roles=...)]
// ==========================================================

export const STAFF_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
  "SchoolAdmin", "Accountant", "Registrar", "Teacher", "FormTeacher",
];

export const LEADERSHIP_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
];

export const ADMIN_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress",
  "VicePrincipal", "SchoolAdmin",
];

export const FINANCE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "VicePrincipal", "Accountant",
];

export const AUDIT_ROLES: UserRole[] = ["SuperAdmin", "Principal"];

export const PEOPLE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress",
  "VicePrincipal", "SchoolAdmin", "Registrar",
];

export const REGISTRAR_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Registrar",
];

export const ENQUIRY_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Registrar",
];

export const TEACHING_ROLES: UserRole[] = ["Teacher", "FormTeacher"];

export const SCORE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "VicePrincipal", "Headmaster", "Headmistress",
  "Teacher", "FormTeacher",
];

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
  if (userRole === "SuperAdmin") return true;
  return allowed.includes(userRole);
};

export const isStaff = (role: UserRole | undefined): boolean =>
  !!role && STAFF_ROLES.includes(role);

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

export const roleTone = (
  role: UserRole | undefined
): "emerald" | "sky" | "violet" | "amber" | "rose" | "neutral" => {
  if (!role) return "neutral";
  switch (role) {
    case "SuperAdmin":
      return "rose";
    case "Principal":
    case "Headmaster":
    case "Headmistress":
      return "emerald";
    case "VicePrincipal":
    case "SchoolAdmin":
      return "violet";
    case "Accountant":
      return "amber";
    case "Registrar":
      return "sky";
    case "Teacher":
    case "FormTeacher":
      return "sky";
    case "Student":
      return "emerald";
    case "Parent":
      return "violet";
    default:
      return "neutral";
  }
};

// ==========================================================
// RE-EXPORTS
// ==========================================================

export type { UserRole } from "../types/auth";
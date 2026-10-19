import type { UserRole } from "../types/auth";

export const STAFF_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
  "SchoolAdmin", "Accountant", "Cashier", "Registrar", "Teacher", "FormTeacher",
];

export const LEADERSHIP_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
];

export const ADMIN_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Headmaster", "Headmistress",
  "VicePrincipal", "SchoolAdmin",
];

// 👇 tightened — VicePrincipal removed from full finance
export const FINANCE_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Accountant", "Cashier",
];

// 👇 full policy — no cashier
export const FINANCE_FULL_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Accountant",
];

// 👇 cash collection — cashier allowed
export const FINANCE_CASHIER_ROLES: UserRole[] = [
  "SuperAdmin", "Principal", "Accountant", "Cashier",
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
    Cashier: "Cashier",
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
    case "Cashier":
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

export type { UserRole } from "../types/auth";
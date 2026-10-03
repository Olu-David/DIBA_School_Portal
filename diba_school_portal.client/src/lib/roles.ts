import type { UserRole } from "../types/auth";

export const ADMIN_ROLES: UserRole[] = [
  "SuperAdmin",
  "Principal",
  "Headmaster",
  "Headmistress",
  "VicePrincipal",
  "SchoolAdmin",
];

export const LEADERSHIP_ROLES: UserRole[] = [
  "SuperAdmin",
  "Principal",
  "Headmaster",
  "Headmistress",
  "VicePrincipal",
];

export const FINANCE_ROLES: UserRole[] = [
  "SuperAdmin",
  "Principal",
  "VicePrincipal",
  "Accountant",
];

export const hasRole = (userRole: UserRole | undefined, allowed: UserRole[]): boolean =>
  !!userRole && allowed.includes(userRole);

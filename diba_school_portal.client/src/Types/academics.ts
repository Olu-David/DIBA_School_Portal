export type AcademicSection =
  | "Nursery"
  | "Primary"
  | "JuniorSecondary"
  | "SeniorSecondary";

export type ClassStream = "None" | "Science" | "Arts" | "Commercial" | "General";

export interface SchoolClass {
  id: string;
  name: string;
  academicSection: AcademicSection;
  displayOrder: number;
}

export interface AcademicSession {
  id: string;
  name: string;
  isCurrentSession: boolean;
  startDate: string;
  endDate: string;
  isActive: boolean;
  terms: SchoolTerm[];
}

export interface SchoolTerm {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrentTerm: boolean;
}

export interface Subject {
  id: string;
  name: string;
  code: string;
}

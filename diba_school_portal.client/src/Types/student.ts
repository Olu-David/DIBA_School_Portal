import type { AcademicSection, ClassStream } from "./academics";

export interface StudentRow {
  id: string;
  admissionNumber: string;
  fullName: string;
  className: string;
  stream: ClassStream | null;
  profilePictureUrl: string | null;
  isActive: boolean;
  outstandingBalance: number;
  isFullyPaid: boolean;
  primaryGuardianPhone: string | null;
}

export interface StudentDetail {
  id: string;
  userId: string;
  admissionNumber: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  schoolClassId: string;
  className: string;
  section: AcademicSection;
  stream: ClassStream | null;
  isActive: boolean;
  createdAt: string;
  outstandingBalance: number;
  isFullyPaid: boolean;
  latestAverageScore: number;
  latestClassPosition: number;
  latestTermName: string | null;
}

export interface StudentFilter {
  classId?: string;
  section?: AcademicSection;
  stream?: ClassStream;
  isActive?: boolean;
  hasOutstanding?: boolean;
  searchTerm?: string;
  pageNumber: number;
  pageSize: number;
}
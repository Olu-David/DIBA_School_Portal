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

export interface StudentReportCard {
  termResultId: string;
  studentId: string;
  studentName: string;
  admissionNumber?: string | null;
  profilePictureUrl?: string | null;
  classId: string;
  className: string;
  stream: ClassStream | null;
  termId: string;
  termName: string;
  academicSessionId: string;
  sessionName: string;
  averageScore: number;
  classPosition: number;
  overallGrade: string | null;
  teacherRemark?: string | null;
  principalRemark?: string | null;
  subjectResults: {
    id: string;
    subjectId: string;
    subjectName: string;
    test1Score: number;
    test2Score: number;
    examScore: number;
    totalScore: number;
    subjectPosition: number;
    grade: string;
    remark: string;
  }[];
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

export interface StudentParentLink {
  parentUserId: string;
  fullName: string;
  phoneNumber: string | null;
  email: string | null;
  profilePictureUrl: string | null;
  relationship: string;
  isPrimaryContact: boolean;
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
  parents: StudentParentLink[];
}
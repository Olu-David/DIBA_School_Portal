export type UserRole =
  | "SuperAdmin"
  | "Principal"
  | "Headmaster"
  | "Headmistress"
  | "VicePrincipal"
  | "SchoolAdmin"
  | "Accountant"
  | "Registrar"
  | "Teacher"
  | "FormTeacher"
  | "Student"
  | "Parent";

export interface AuthUser {
  id: string;
  email: string | null;
  admissionNumber?: string | null;
  firstName: string;
  lastName: string;
  profilePictureUrl: string | null;
  role: UserRole;
  classId?: string | null;
  className?: string | null;
  mustChangePassword?: boolean;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: AuthUser;
}

export interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (data: LoginResponse) => void;
  logout: () => void;
  updateUser: (patch: Partial<AuthUser>) => void;
}

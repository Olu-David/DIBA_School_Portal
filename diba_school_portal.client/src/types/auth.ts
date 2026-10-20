export type UserRole =
  | "SuperAdmin"
  | "Principal"
  | "Headmaster"
  | "Headmistress"
  | "VicePrincipal"
  | "SchoolAdmin"
  | "Accountant"
  | "Cashier"
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
   isGraduated?: boolean;
  graduationDate?: string | null;
  graduationSession?: string | null;

  /** Bumped whenever the picture changes — used for cache-busting. */
  updatedAt?: string | null;

  /** Used on the profile page (kept so it's not wiped on edit). */
  phoneNumber?: string | null;
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
  refreshUser: () => Promise<void>;
}
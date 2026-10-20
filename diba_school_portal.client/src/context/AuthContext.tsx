import {
  createContext,
  useCallback,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import api from "../lib/api";
import type { ApiResponse } from "../types/common";
import type { AuthContextValue, AuthUser, LoginResponse } from "../types/auth";

const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredUser(): AuthUser | null {
  try {
    const stored = localStorage.getItem("diba_user");
    return stored ? (JSON.parse(stored) as AuthUser) : null;
  } catch {
    localStorage.removeItem("diba_user");
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readStoredUser);
  const [token, setToken] = useState<string | null>(() =>
    localStorage.getItem("diba_token")
  );

  useEffect(() => {
    if (token) localStorage.setItem("diba_token", token);
    else localStorage.removeItem("diba_token");
  }, [token]);

  useEffect(() => {
    if (user) localStorage.setItem("diba_user", JSON.stringify(user));
    else localStorage.removeItem("diba_user");
  }, [user]);

  const login = useCallback((d: LoginResponse) => {
    setToken(d.token);
    setUser(d.user);
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setUser(null);
  }, []);

  const updateUser = useCallback((patch: Partial<AuthUser>) => {
    setUser((prev) => (prev ? { ...prev, ...patch } : prev));
  }, []);

  // 👇 THE MISSING PIECE — declared here and passed in `value` below
  const refreshUser = useCallback(async () => {
    try {
      const { data } = await api.get<ApiResponse<AuthUser>>("/auth/me");
      if (data.success && data.data) setUser(data.data);
    } catch {
      /* swallow — don't log the user out on a transient blip */
    }
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token && !!user,
        login,
        logout,
        updateUser,
        refreshUser, // 👈 now satisfies AuthContextValue
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export default AuthContext;
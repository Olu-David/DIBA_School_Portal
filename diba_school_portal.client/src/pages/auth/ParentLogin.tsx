import { useState, type FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import api from "../../lib/api";
import { useAuth } from "../../hooks/useAuth";
import type { ApiResponse } from "../../types/common";
import type { LoginResponse } from "../../types/auth";

export default function ParentLogin() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const { data } = await api.post<ApiResponse<LoginResponse>>("/auth/parent/login", {
        identifier,
        password,
      });
      if (!data.success) {
        setError(data.message ?? "Login failed.");
        return;
      }
      login(data.data);
      navigate("/parent");
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      setError(e2?.response?.data?.message ?? "Network error");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-white rounded-2xl shadow-soft border border-surface-100 p-8">
      <h2 className="text-xl font-semibold text-surface-900">Parent sign in</h2>
      <p className="mt-1 text-[13px] text-surface-500 mb-6">
        Use your phone number to continue.
      </p>

      <form onSubmit={submit} className="space-y-4">
        <input
          type="text"
          value={identifier}
          onChange={(e) => setIdentifier(e.target.value)}
          placeholder="08012345678"
          required
          className="w-full px-4 py-2.5 text-sm rounded-xl bg-surface-50 outline-none"
        />
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Password"
          required
          className="w-full px-4 py-2.5 text-sm rounded-xl bg-surface-50 outline-none"
        />
        {error && <p className="text-[12px] text-flame-600">{error}</p>}
        <button
          type="submit"
          disabled={loading}
          className="w-full py-3 rounded-xl bg-brand-600 text-white font-medium disabled:opacity-60 hover:bg-brand-700 transition-colors"
        >
          {loading ? "Signing in..." : "Sign in"}
        </button>
      </form>

      <div className="mt-6 pt-6 border-t border-surface-100 text-center text-[12px] text-surface-400">
        <Link to="/auth/staff" className="text-brand-600 font-medium hover:text-brand-700">
          Staff sign in
        </Link>
        <span className="mx-2">-</span>
        <Link to="/auth/student" className="text-brand-600 font-medium hover:text-brand-700">
          Student sign in
        </Link>
      </div>

      <div className="mt-4 text-center">
        <Link to="/" className="text-[12px] text-surface-400 hover:text-surface-600">
          Back to home
        </Link>
      </div>
    </div>
  );
}
// layouts/StudentLayout.tsx
import { useState } from "react";
import { Outlet, NavLink, useNavigate } from "react-router-dom";
import { LayoutDashboard, Wallet, FileText, User, Menu, LogOut } from "lucide-react";
import { useAuth } from "../hooks/useAuth";

const items = [
  { to: "/student", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/student/bill", label: "My bill", icon: Wallet },
  { to: "/student/results", label: "My results", icon: FileText },
  { to: "/student/profile", label: "Profile", icon: User },
];

export default function StudentLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const Sidebar = ({ mobile }: { mobile?: boolean }) => (
    <aside className={mobile ? "w-72 flex flex-col h-full bg-white border-r border-slate-100" : "hidden lg:flex flex-col w-64 h-full bg-white border-r border-slate-100 shrink-0"}>
      <div className="h-16 px-6 flex items-center border-b border-slate-100">
        <p className="text-[13px] font-semibold text-slate-900">DIBA · Student</p>
      </div>
      <nav className="flex-1 p-3 space-y-1">
        {items.map((it) => (
          <NavLink
            key={it.to}
            to={it.to}
            end={it.end}
            onClick={() => setOpen(false)}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors ${
                isActive ? "bg-emerald-50 text-emerald-700" : "text-slate-600 hover:bg-slate-50"
              }`
            }
          >
            <it.icon className="w-4 h-4" />
            {it.label}
          </NavLink>
        ))}
      </nav>
      <div className="p-3 border-t border-slate-100">
        <button
          type="button"
          onClick={() => {
            logout();
            navigate("/auth/student");
          }}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm text-slate-600 hover:bg-rose-50"
        >
          <LogOut className="w-4 h-4" />
          Sign out
        </button>
      </div>
    </aside>
  );

  return (
    <div className="min-h-screen flex bg-slate-50">
      <Sidebar />
      {open && (
        <>
          <div className="fixed inset-0 bg-slate-900/40 z-40 lg:hidden" onClick={() => setOpen(false)} />
          <div className="fixed inset-y-0 left-0 z-50 lg:hidden"><Sidebar mobile /></div>
        </>
      )}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-slate-100 px-4 flex items-center gap-3">
          <button type="button" onClick={() => setOpen(true)} className="lg:hidden p-2 rounded-lg hover:bg-slate-100">
            <Menu className="w-5 h-5" />
          </button>
          <p className="text-[13px] text-slate-600">
            {user?.firstName} {user?.lastName} · {user?.className ?? "Student"}
          </p>
        </header>
        <main className="flex-1 p-6"><Outlet /></main>
      </div>
    </div>
  );
}
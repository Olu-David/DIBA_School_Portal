import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  Users,
  Wallet,
  GraduationCap,
  LogOut,
  ScrollText,
  type LucideIcon,
} from "lucide-react";
import { useAuth } from "../hooks/useAuth";

interface Item {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
}

const items: Item[] = [
  { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/students", label: "Students", icon: Users },
  { to: "/admin/classes", label: "Classes", icon: GraduationCap },
  { to: "/admin/finance", label: "Finance", icon: Wallet },
  { to: "/admin/audit-logs", label: "Audit Logs", icon: ScrollText },
];

interface SidebarProps {
  mobile?: boolean;
  onNavigate?: () => void;
}

export default function Sidebar({ mobile = false, onNavigate }: SidebarProps) {
  const { user, logout } = useAuth();

  return (
    <aside
      className={
        mobile
          ? "w-72 flex flex-col h-full bg-white border-r border-slate-100"
          : "hidden lg:flex flex-col w-64 xl:w-72 h-full bg-white border-r border-slate-100 shrink-0"
      }
    >
      <div className="h-16 px-6 flex items-center border-b border-slate-100">
        <p className="text-[13px] font-semibold text-slate-900">DIBA Portal</p>
      </div>

      <nav className="flex-1 p-3 space-y-1 overflow-y-auto">
        {items.map((it) => (
          <NavLink
            key={it.to}
            to={it.to}
            end={it.end}
            onClick={onNavigate}
            className={({ isActive }) =>
              [
                "flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors",
                isActive
                  ? "bg-emerald-50 text-emerald-700"
                  : "text-slate-600 hover:bg-slate-50",
              ].join(" ")
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
          onClick={logout}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm text-slate-600 hover:bg-rose-50"
        >
          <LogOut className="w-4 h-4" />
          Sign out
        </button>
        <p className="text-[11px] text-slate-400 px-3 mt-2">{user?.role}</p>
      </div>
    </aside>
  );
}

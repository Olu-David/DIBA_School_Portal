import { useEffect, useState } from "react";
import { Outlet, NavLink, useNavigate, useLocation } from "react-router-dom";
import { Menu, LogOut, Bell, type LucideIcon } from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { roleLabel } from "../lib/roles";

export interface PortalNavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
}

interface Props {
  brand: string;
  items: PortalNavItem[];
  loginPath: string;
  userLine?: string;
}

export default function PortalShell({
  brand,
  items,
  loginPath,
  userLine,
}: Props) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const [open, setOpen] = useState(false);

  useEffect(() => setOpen(false), [pathname]);

  useEffect(() => {
    document.body.style.overflow = open ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [open]);

  const handleLogout = () => {
    logout();
    navigate(loginPath);
  };

  const SidebarContent = ({ mobile = false }: { mobile?: boolean }) => (
    <aside
      className={
        mobile
          ? "w-72 flex flex-col h-full bg-white border-r border-slate-100"
          : "hidden lg:flex flex-col w-64 h-full bg-white border-r border-slate-100 shrink-0"
      }
    >
      <div className="h-16 px-6 flex items-center border-b border-slate-100 shrink-0">
        <p className="text-[13px] font-semibold text-slate-900">{brand}</p>
      </div>

      <nav className="flex-1 p-3 space-y-1 overflow-y-auto">
        {items.map((it) => (
          <NavLink
            key={it.to}
            to={it.to}
            end={it.end}
            onClick={() => setOpen(false)}
            className={({ isActive }) =>
              [
                "flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] font-medium transition-colors",
                isActive
                  ? "bg-emerald-50 text-emerald-700"
                  : "text-slate-600 hover:bg-slate-50",
              ].join(" ")
            }
          >
            <it.icon className="w-4 h-4 shrink-0" />
            {it.label}
          </NavLink>
        ))}
      </nav>

      <div className="p-3 border-t border-slate-100 shrink-0">
        <div className="flex items-center gap-3 px-3 py-2 mb-2">
          <div className="w-9 h-9 rounded-full bg-emerald-100 grid place-items-center text-[12px] font-semibold text-emerald-700 shrink-0">
            {(user?.firstName?.[0] ?? "") + (user?.lastName?.[0] ?? "")}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-[12px] font-semibold text-slate-800 truncate">
              {user?.firstName} {user?.lastName}
            </p>
            <p className="text-[10px] text-slate-400 truncate">
              {roleLabel(user?.role)}
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={handleLogout}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] text-slate-600 hover:bg-rose-50 hover:text-rose-600 transition-colors"
        >
          <LogOut className="w-4 h-4" />
          Sign out
        </button>
      </div>
    </aside>
  );

  return (
    <div className="min-h-screen flex bg-slate-50">
      <SidebarContent />

      {open && (
        <>
          <div
            className="fixed inset-0 bg-slate-900/40 z-40 lg:hidden"
            onClick={() => setOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-y-0 left-0 z-50 lg:hidden">
            <SidebarContent mobile />
          </div>
        </>
      )}

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-slate-100 px-4 lg:px-6 flex items-center gap-3 shrink-0">
          <button
            type="button"
            onClick={() => setOpen(true)}
            className="lg:hidden p-2 rounded-lg hover:bg-slate-100"
            aria-label="Open menu"
          >
            <Menu className="w-5 h-5" />
          </button>
          <p className="text-[13px] text-slate-600 truncate flex-1">
            {userLine ??
              `${user?.firstName ?? ""} ${user?.lastName ?? ""}`}
          </p>
          <button
            type="button"
            className="relative p-2 rounded-lg hover:bg-slate-100 text-slate-500"
            aria-label="Notifications"
          >
            <Bell className="w-4 h-4" />
          </button>
        </header>

        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
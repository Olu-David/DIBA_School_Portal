import { NavLink } from "react-router-dom";
import {
  LayoutDashboard, Users, Wallet, GraduationCap, LogOut, ScrollText,
  BookOpen, CalendarDays, ClipboardCheck, FileText, UserCog, Inbox,
  TrendingUp, AlertTriangle, Receipt, BarChart3, UserCog2, Home,
  Bell, CheckCircle2, School,
  type LucideIcon,
} from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { useRole } from "../hooks/useRole";
import { roleLabel, roleTone } from "../lib/roles";
import type { UserRole } from "../types/auth";
import { Badge } from "./ui";

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
  /** If absent, visible to every staff member */
  roles?: UserRole[];
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

// helper to keep the arrays readable
const R = {
  admin: ["SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal", "SchoolAdmin"] as UserRole[],
  leadership: ["SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal"] as UserRole[],
  finance: ["SuperAdmin", "Principal", "VicePrincipal", "Accountant"] as UserRole[],
  people: ["SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal", "SchoolAdmin", "Registrar"] as UserRole[],
  registrar: ["SuperAdmin", "Principal", "Registrar"] as UserRole[],
  audit: ["SuperAdmin", "Principal"] as UserRole[],
  teaching: ["Teacher", "FormTeacher"] as UserRole[],
  score: ["SuperAdmin", "Principal", "VicePrincipal", "Headmaster", "Headmistress", "Teacher", "FormTeacher"] as UserRole[],
  results: ["SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal", "SchoolAdmin", "Teacher", "FormTeacher"] as UserRole[],
};

const NAV_GROUPS: NavGroup[] = [
  {
    label: "Overview",
    items: [
      { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true },
      { to: "/admin/notifications", label: "Notifications", icon: Bell },
    ],
  },
  {
    label: "People",
    items: [
      { to: "/admin/students", label: "Students", icon: Users, roles: [...R.people, ...R.teaching] },
      { to: "/admin/teachers", label: "Teachers & Staff", icon: UserCog, roles: R.people },
      { to: "/admin/parents", label: "Parents", icon: Users, roles: R.people },
    ],
  },
  {
    label: "Academics",
    items: [
      { to: "/admin/classes", label: "Classes", icon: GraduationCap, roles: [...R.admin, ...R.teaching] },
      { to: "/admin/subjects", label: "Subjects", icon: BookOpen, roles: R.admin },
      { to: "/admin/sessions", label: "Sessions & Terms", icon: CalendarDays, roles: R.admin },
    ],
  },
  {
    label: "Results",
    items: [
      { to: "/admin/score-entry", label: "Score Entry", icon: ClipboardCheck, roles: R.score },
      { to: "/admin/my-assignments", label: "My Assignments", icon: BookOpen, roles: R.teaching },
      { to: "/admin/results", label: "School Results", icon: BarChart3, roles: R.results },
      { to: "/admin/report-cards", label: "Report Cards", icon: FileText, roles: R.results },
      { to: "/admin/results-approval", label: "Approve Results", icon: CheckCircle2, roles: R.leadership },
    ],
  },
  {
    label: "Finance",
    items: [
      { to: "/admin/finance", label: "Overview", icon: Wallet, end: true, roles: R.finance },
      { to: "/admin/finance/payments", label: "Payments", icon: Receipt, roles: R.finance },
      { to: "/admin/finance/arrears", label: "Arrears", icon: AlertTriangle, roles: R.finance },
      { to: "/admin/finance/fees", label: "Fee Structures", icon: Wallet, roles: R.finance },
      { to: "/admin/finance/record-cash", label: "Record Cash", icon: Receipt, roles: R.finance },
    ],
  },
  {
    label: "Admissions",
    items: [
      { to: "/admin/enquiries", label: "Enquiries Inbox", icon: Inbox, roles: R.registrar },
    ],
  },
  {
    label: "System",
    items: [
      { to: "/admin/audit-logs", label: "Audit Logs", icon: ScrollText, roles: R.audit },
    ],
  },
];

interface SidebarProps {
  mobile?: boolean;
  onNavigate?: () => void;
}

export default function Sidebar({ mobile = false, onNavigate }: SidebarProps) {
  const { user, logout } = useAuth();
  const { role } = useRole();

  // Filter groups → only keep items the current role can see
  const visibleGroups = NAV_GROUPS
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => {
        if (!item.roles) return true;                    // no restriction
        if (role === "SuperAdmin") return true;          // SuperAdmin sees all
        return item.roles.includes(role as UserRole);
      }),
    }))
    .filter((group) => group.items.length > 0);          // drop empty groups

  return (
    <aside
      className={
        mobile
          ? "w-72 flex flex-col h-full bg-white border-r border-slate-100"
          : "hidden lg:flex flex-col w-64 xl:w-72 h-full bg-white border-r border-slate-100 shrink-0"
      }
    >
      {/* Brand */}
      <div className="h-16 px-6 flex items-center border-b border-slate-100 shrink-0">
        <div className="w-8 h-8 rounded-lg bg-emerald-600 grid place-items-center mr-3">
          <School className="w-4 h-4 text-white" />
        </div>
        <div className="min-w-0">
          <p className="text-[13px] font-semibold text-slate-900 truncate">
            DIBA Portal
          </p>
          <p className="text-[10px] text-slate-400 truncate">
            {roleLabel(role)}
          </p>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 p-3 overflow-y-auto space-y-6">
        {visibleGroups.map((group) => (
          <div key={group.label}>
            <p className="px-3 mb-2 text-[10px] font-semibold uppercase tracking-wider text-slate-400">
              {group.label}
            </p>
            <div className="space-y-0.5">
              {group.items.map((it) => (
                <NavLink
                  key={it.to}
                  to={it.to}
                  end={it.end}
                  onClick={onNavigate}
                  className={({ isActive }) =>
                    [
                      "flex items-center gap-3 px-3 py-2 rounded-xl text-[13px] font-medium transition-colors",
                      isActive
                        ? "bg-emerald-50 text-emerald-700"
                        : "text-slate-600 hover:bg-slate-50",
                    ].join(" ")
                  }
                >
                  <it.icon className="w-4 h-4 shrink-0" />
                  <span className="truncate">{it.label}</span>
                </NavLink>
              ))}
            </div>
          </div>
        ))}
      </nav>

      {/* User footer */}
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
              {user?.email}
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={logout}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] text-slate-600 hover:bg-rose-50 hover:text-rose-600 transition-colors"
        >
          <LogOut className="w-4 h-4" />
          Sign out
        </button>
      </div>
    </aside>
  );
}
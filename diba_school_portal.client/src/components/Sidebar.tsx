import { NavLink } from "react-router-dom";
import {
  LayoutDashboard, Users, Wallet, GraduationCap, LogOut, ScrollText,
  BookOpen, CalendarDays, ClipboardCheck, FileText, UserCog, Inbox,
  AlertTriangle, Receipt, BarChart3, Bell, User, Send,
  type LucideIcon,
} from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { useRole } from "../hooks/useRole";
import { roleLabel, type UserRole } from "../lib/roles";

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
  /** Roles allowed to see this item. If omitted, all staff see it. */
  roles?: UserRole[];
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

// ---- Role groups (mirror lib/roles.ts) ----
const R = {
  // Anyone with a staff account
  all: [
    "SuperAdmin", "Principal", "Headmaster", "Headmistress",
    "VicePrincipal", "SchoolAdmin", "Accountant", "Registrar",
    "Teacher", "FormTeacher",
  ] as UserRole[],

  // Top of the hierarchy
  leadership: [
    "SuperAdmin", "Principal", "Headmaster", "Headmistress", "VicePrincipal",
  ] as UserRole[],

  // Admin + registrar (people management)
  people: [
    "SuperAdmin", "Principal", "Headmaster", "Headmistress",
    "VicePrincipal", "SchoolAdmin", "Registrar",
  ] as UserRole[],

  // Admin only (settings tier)
  admin: [
    "SuperAdmin", "Principal", "Headmaster", "Headmistress",
    "VicePrincipal", "SchoolAdmin",
  ] as UserRole[],

  // Finance
  finance: [
    "SuperAdmin", "Principal", "VicePrincipal", "Accountant",
  ] as UserRole[],

  // Enquiries
  registrar: ["SuperAdmin", "Principal", "Registrar"] as UserRole[],

  // Audit
  audit: ["SuperAdmin", "Principal"] as UserRole[],

  // Teachers + leadership (score entry)
  score: [
    "SuperAdmin", "Principal", "VicePrincipal", "Headmaster",
    "Headmistress", "Teacher", "FormTeacher",
  ] as UserRole[],

  // Teachers only (their own assignments)
  teaching: ["Teacher", "FormTeacher"] as UserRole[],

  // Anyone allowed to view results
  results: [
    "SuperAdmin", "Principal", "Headmaster", "Headmistress",
    "VicePrincipal", "SchoolAdmin", "Teacher", "FormTeacher",
  ] as UserRole[],
};

const NAV_GROUPS: NavGroup[] = [
  {
    label: "Overview",
    items: [
      { to: "/admin", label: "Dashboard", icon: LayoutDashboard, end: true, roles: R.all },
      { to: "/admin/notifications", label: "Notifications", icon: Bell, roles: R.all },
    ],
  },

  // ---- Visible only to staff with people-management rights ----
  {
    label: "People",
    items: [
      { to: "/admin/students", label: "Students", icon: Users, roles: R.people },
      { to: "/admin/teachers", label: "Teachers & Staff", icon: UserCog, roles: R.people },
      { to: "/admin/parents", label: "Parents", icon: Users, roles: R.people },
    ],
  },

  // ---- Teacher + admin view: their own classes & students ----
  {
    label: "My Classrooms",
    items: [
      { to: "/admin/classes", label: "My Classes", icon: GraduationCap, roles: R.score },
      { to: "/admin/students", label: "My Students", icon: Users, roles: R.teaching },
    ],
  },

  // ---- Academic setup (admin only) ----
  {
    label: "Academics",
    items: [
      { to: "/admin/subjects", label: "Subjects", icon: BookOpen, roles: R.admin },
      { to: "/admin/sessions", label: "Sessions & Terms", icon: CalendarDays, roles: R.admin },
    ],
  },

  // ---- Results & Scores ----
  {
    label: "Results",
    items: [
      { to: "/admin/score-entry", label: "Score Entry", icon: ClipboardCheck, roles: R.score },
      { to: "/admin/my-assignments", label: "My Assignments", icon: BookOpen, roles: R.teaching },
      { to: "/admin/results", label: "School Results", icon: BarChart3, roles: R.results },
      { to: "/admin/report-cards", label: "Report Cards", icon: FileText, roles: R.results },
    ],
  },

  // ---- Finance (finance roles only) ----
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

  // ---- Communications (leadership only) ----
  {
    label: "Communications",
    items: [
      {
        to: "/admin/notifications/send",
        label: "Send Notifications",
        icon: Send,
        roles: R.leadership,
      },
    ],
  },

  // ---- Admissions (registrar and above) ----
  {
    label: "Admissions",
    items: [
      { to: "/admin/enquiries", label: "Enquiries Inbox", icon: Inbox, roles: R.registrar },
    ],
  },

  // ---- System (audit tier only) ----
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

  // Filter: hide any group whose items are all hidden for this role
  const visibleGroups = NAV_GROUPS
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => {
        if (!item.roles) return true;
        if (role === "SuperAdmin") return true;
        return item.roles.includes(role as UserRole);
      }),
    }))
    .filter((group) => group.items.length > 0);

  return (
    <aside
      className={
        mobile
          ? "w-72 flex flex-col h-full bg-surface-950 border-r border-surface-800"
          : "hidden lg:flex flex-col w-64 xl:w-72 h-full bg-surface-950 border-r border-surface-800 shrink-0"
      }
    >
      {/* Brand */}
      <div className="h-16 px-5 flex items-center border-b border-white/10 shrink-0">
        <img
          src="/images/logo.png"
          alt="DIBA"
          className="w-9 h-9 object-contain mr-3 shrink-0"
        />
        <div className="min-w-0">
          <p className="text-[12px] font-semibold text-white truncate leading-tight">
            Dominion Intelligent Builders
          </p>
          <p className="text-[10px] text-surface-400 truncate">
            Academy - {roleLabel(role)}
          </p>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 p-3 overflow-y-auto space-y-6">
        {visibleGroups.map((group) => (
          <div key={group.label}>
            <p className="px-3 mb-2 text-[10px] font-semibold uppercase tracking-wider text-surface-500">
              {group.label}
            </p>
            <div className="space-y-0.5">
              {group.items.map((it) => (
                <NavLink
                  key={it.to + it.label}
                  to={it.to}
                  end={it.end}
                  onClick={onNavigate}
                  className={({ isActive }) =>
                    [
                      "flex items-center gap-3 px-3 py-2 rounded-xl text-[13px] font-medium transition-colors",
                      isActive
                        ? "bg-brand-600 text-white shadow-sm"
                        : "text-surface-300 hover:bg-white/5 hover:text-white",
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
      <div className="p-3 border-t border-white/10 shrink-0">
        <NavLink
          to="/admin/profile"
          onClick={onNavigate}
          className="flex items-center gap-3 px-3 py-2 mb-2 rounded-xl hover:bg-white/5 transition-colors"
        >
          <div className="w-9 h-9 rounded-full bg-brand-500 grid place-items-center text-[12px] font-semibold text-white shrink-0">
            {(user?.firstName?.[0] ?? "") + (user?.lastName?.[0] ?? "")}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-[12px] font-semibold text-white truncate">
              {user?.firstName} {user?.lastName}
            </p>
            <p className="text-[10px] text-surface-400 truncate">{user?.email}</p>
          </div>
          <User className="w-3.5 h-3.5 text-surface-500" />
        </NavLink>

        <button
          type="button"
          onClick={logout}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-[13px] text-surface-300 hover:bg-flame-500/10 hover:text-flame-400 transition-colors"
        >
          <LogOut className="w-4 h-4" />
          Sign out
        </button>
      </div>
    </aside>
  );
}
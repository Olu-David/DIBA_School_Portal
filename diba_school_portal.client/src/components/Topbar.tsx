import { useState } from "react";
import { Menu, Bell } from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { useRole } from "../hooks/useRole";
import { roleLabel, roleTone } from "../lib/roles";
import { Badge } from "./ui";
import NotificationsDrawer from "../pages/shared/NotificationsDrawer";

interface TopbarProps {
  onOpenSidebar: () => void;
}

export default function Topbar({ onOpenSidebar }: TopbarProps) {
  const { user } = useAuth();
  const { role } = useRole();
  const [notifOpen, setNotifOpen] = useState(false);

  return (
    <>
      <header className="h-16 bg-white border-b border-surface-200 px-4 lg:px-6 flex items-center gap-3 shrink-0 shadow-[0_1px_2px_rgba(15,23,42,0.03)]">
        <button
          type="button"
          onClick={onOpenSidebar}
          className="lg:hidden p-2 rounded-lg hover:bg-surface-100"
          aria-label="Open menu"
        >
          <Menu className="w-5 h-5" />
        </button>

        <div className="flex-1 min-w-0 flex items-center gap-3">
          <Badge variant={roleTone(role)}>{roleLabel(role)}</Badge>
          {user?.className && (
            <span className="text-[12px] text-surface-500 hidden sm:inline">
              {user.className}
            </span>
          )}
        </div>

        <button
          type="button"
          onClick={() => setNotifOpen(true)}
          className="relative p-2 rounded-lg hover:bg-surface-100 text-surface-500"
          aria-label="Notifications"
        >
          <Bell className="w-4 h-4" />
        </button>

        <div className="flex items-center gap-2 pl-3 ml-1 border-l border-surface-200">
          <div className="w-8 h-8 rounded-full bg-brand-500 grid place-items-center text-[11px] font-semibold text-white">
            {(user?.firstName?.[0] ?? "") + (user?.lastName?.[0] ?? "")}
          </div>
        </div>
      </header>

      <NotificationsDrawer
        open={notifOpen}
        onClose={() => setNotifOpen(false)}
      />
    </>
  );
}
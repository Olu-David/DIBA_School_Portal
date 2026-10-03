import { Menu } from "lucide-react";
import { useAuth } from "../hooks/useAuth";

interface TopbarProps {
  onOpenSidebar: () => void;
}

export default function Topbar({ onOpenSidebar }: TopbarProps) {
  const { user } = useAuth();

  return (
    <header className="h-16 bg-white border-b border-slate-100 px-4 flex items-center gap-3">
      <button
        type="button"
        onClick={onOpenSidebar}
        className="lg:hidden p-2 rounded-lg hover:bg-slate-100"
        aria-label="Open menu"
      >
        <Menu className="w-5 h-5" />
      </button>
      <p className="text-[13px] text-slate-600">
        {user ? `${user.firstName} ${user.lastName}` : "Guest"} · {user?.role}
      </p>
    </header>
  );
}

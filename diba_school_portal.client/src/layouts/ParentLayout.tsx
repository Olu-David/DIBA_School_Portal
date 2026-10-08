import {
  LayoutDashboard,
  Users,
  User,
} from "lucide-react";
import PortalShell, { type NavItem } from "../components/PortalShell";
import { useAuth } from "../hooks/useAuth";

const items: NavItem[] = [
  { to: "/parent", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/parent/children", label: "My children", icon: Users, end: false },
  { to: "/parent/profile", label: "Profile", icon: User },
];

export default function ParentLayout() {
  const { user } = useAuth();
  const line = `${user?.firstName ?? ""} ${user?.lastName ?? ""} · Parent`;

  return (
    <PortalShell
      brand="DIBA · Parent"
      items={items}
      loginPath="/auth/parent"
      userLine={line}
    />
  );
}
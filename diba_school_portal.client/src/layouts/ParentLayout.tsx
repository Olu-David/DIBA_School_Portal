import { LayoutDashboard, Users, User, Bell } from "lucide-react";
import PortalShell, { type PortalNavItem } from "../components/PortalShell";

const items: PortalNavItem[] = [
  { to: "/parent", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/parent/children", label: "My children", icon: Users, end: true },
  { to: "/parent/notifications", label: "Notifications", icon: Bell },
  { to: "/parent/profile", label: "Profile", icon: User },
];

export default function ParentLayout() {
  return (
    <PortalShell
      brand="Parent Portal - Sokoto"
      items={items}
      loginPath="/auth/parent"
    />
  );
}
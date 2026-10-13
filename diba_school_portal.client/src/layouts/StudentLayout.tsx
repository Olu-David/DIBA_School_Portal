import { LayoutDashboard, Wallet, FileText, User, Bell } from "lucide-react";
import PortalShell, { type PortalNavItem } from "../components/PortalShell";

const items: PortalNavItem[] = [
  { to: "/student", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/student/bill", label: "My bill", icon: Wallet },
  { to: "/student/results", label: "My results", icon: FileText },
  { to: "/student/notifications", label: "Notifications", icon: Bell },
  { to: "/student/profile", label: "Profile", icon: User },
];

export default function StudentLayout() {
  return (
    <PortalShell
      brand="Student Portal - Sokoto"
      items={items}
      loginPath="/auth/student"
    />
  );
}
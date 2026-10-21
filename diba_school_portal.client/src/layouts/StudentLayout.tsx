import { useEffect, useState } from "react";
import {
  LayoutDashboard, Wallet, FileText, User, Bell, BookOpen,
} from "lucide-react";
import PortalShell, { type PortalNavItem } from "../components/PortalShell";
import api from "../lib/api";
import type { ApiResponse } from "../types/common";

export default function StudentLayout() {
  const [subjectCount, setSubjectCount] = useState<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<number>>(
          "/student/me/subjects/count"
        );
        if (!cancelled && data.success) setSubjectCount(data.data ?? 0);
      } catch {
        /* silent */
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const items: PortalNavItem[] = [
    { to: "/student", label: "Dashboard", icon: LayoutDashboard, end: true },
    { to: "/student/bill", label: "My bill", icon: Wallet },
    {
      to: "/student/subjects",
      label:
        subjectCount !== null && subjectCount > 0
          ? `My subjects (${subjectCount})`
          : "My subjects",
      icon: BookOpen,
    },
    { to: "/student/results", label: "My results", icon: FileText },
    { to: "/student/notifications", label: "Notifications", icon: Bell },
    { to: "/student/profile", label: "Profile", icon: User },
  ];

  return (
    <PortalShell
      brand="Student Portal - Sokoto"
      items={items}
      loginPath="/auth/student"
    />
  );
}
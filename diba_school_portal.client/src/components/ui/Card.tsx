import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
  className?: string;
  padding?: "none" | "sm" | "md" | "lg";
}

const pad = { none: "", sm: "p-4", md: "p-6", lg: "p-8" };

export default function Card({ children, className = "", padding = "md" }: Props) {
  return (
    <div className={`bg-white rounded-2xl shadow-soft border border-slate-100 ${pad[padding]} ${className}`}>
      {children}
    </div>
  );
}

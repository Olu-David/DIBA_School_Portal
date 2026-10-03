import type { ReactNode } from "react";

type Variant = "neutral" | "emerald" | "amber" | "rose" | "sky" | "violet";

interface Props {
  children: ReactNode;
  variant?: Variant;
}

const v: Record<Variant, string> = {
  neutral: "bg-slate-100 text-slate-600",
  emerald: "bg-emerald-50 text-emerald-600",
  amber: "bg-amber-50 text-amber-600",
  rose: "bg-rose-50 text-rose-600",
  sky: "bg-sky-50 text-sky-600",
  violet: "bg-violet-50 text-violet-600",
};

export default function Badge({ children, variant = "neutral" }: Props) {
  return (
    <span className={`inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-lg ${v[variant]}`}>
      {children}
    </span>
  );
}

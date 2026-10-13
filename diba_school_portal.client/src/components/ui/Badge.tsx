import type { ReactNode } from "react";

type Variant = "neutral" | "brand" | "flame" | "emerald" | "amber" | "rose" | "sky" | "violet";

interface Props {
  children: ReactNode;
  variant?: Variant;
}

const v: Record<Variant, string> = {
  neutral: "bg-surface-100 text-surface-600",
  brand:   "bg-brand-50 text-brand-700",
  flame:   "bg-flame-50 text-flame-600",
  emerald: "bg-brand-50 text-brand-700",
  amber:   "bg-flame-50 text-flame-600",
  rose:    "bg-flame-50 text-flame-600",
  sky:     "bg-brand-50 text-brand-700",
  violet:  "bg-brand-50 text-brand-700",
};

export default function Badge({ children, variant = "neutral" }: Props) {
  return (
    <span className={`inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-lg ${v[variant]}`}>
      {children}
    </span>
  );
}
import type { LucideIcon } from "lucide-react";

type Tone = "brand" | "flame" | "sky" | "violet" | "emerald" | "amber" | "rose";

interface Props {
  label: string;
  value: string | number;
  hint?: string;
  icon?: LucideIcon;
  tone?: Tone;
}

const tones: Record<Tone, string> = {
  brand:   "bg-brand-50 text-brand-700",
  flame:   "bg-flame-50 text-flame-600",
  sky:     "bg-brand-50 text-brand-700",
  violet:  "bg-brand-50 text-brand-700",
  emerald: "bg-brand-50 text-brand-700",
  amber:   "bg-flame-50 text-flame-600",
  rose:    "bg-flame-50 text-flame-600",
};

export default function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  tone = "brand",
}: Props) {
  return (
    <div className="bg-white rounded-2xl shadow-soft border border-surface-100 p-6 hover:border-surface-200 hover:shadow-md transition-all">
      <div className={`w-10 h-10 rounded-xl grid place-items-center ${tones[tone]}`}>
        {Icon && <Icon className="w-5 h-5" strokeWidth={2} />}
      </div>
      <p className="mt-5 text-[11px] font-medium uppercase tracking-wider text-surface-400">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold text-surface-900 tracking-tight">
        {value}
      </p>
      {hint && <p className="mt-1 text-[12px] text-surface-400">{hint}</p>}
    </div>
  );
}
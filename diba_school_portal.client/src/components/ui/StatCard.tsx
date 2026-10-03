import type { LucideIcon } from "lucide-react";

type Tone = "emerald" | "sky" | "amber" | "violet" | "rose";

interface Props {
  label: string;
  value: string | number;
  hint?: string;
  icon?: LucideIcon;
  tone?: Tone;
}

const tones: Record<Tone, string> = {
  emerald: "bg-emerald-50 text-emerald-600",
  sky: "bg-sky-50 text-sky-600",
  amber: "bg-amber-50 text-amber-600",
  violet: "bg-violet-50 text-violet-600",
  rose: "bg-rose-50 text-rose-600",
};

export default function StatCard({ label, value, hint, icon: Icon, tone = "emerald" }: Props) {
  return (
    <div className="bg-white rounded-2xl shadow-soft border border-slate-100 p-6 hover:border-slate-200 hover:shadow-md transition-all">
      <div className={`w-10 h-10 rounded-xl grid place-items-center ${tones[tone]}`}>
        {Icon && <Icon className="w-5 h-5" strokeWidth={2} />}
      </div>
      <p className="mt-5 text-[11px] font-medium uppercase tracking-wider text-slate-400">{label}</p>
      <p className="mt-1 text-2xl font-semibold text-slate-900 tracking-tight">{value}</p>
      {hint && <p className="mt-1 text-[12px] text-slate-400">{hint}</p>}
    </div>
  );
}

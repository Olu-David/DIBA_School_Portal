interface Props {
  name: string;
  src?: string | null;
  size?: "sm" | "md" | "lg" | "xl";
}

const s = {
  sm: "w-8 h-8 text-[11px]",
  md: "w-10 h-10 text-[12px]",
  lg: "w-12 h-12 text-sm",
  xl: "w-20 h-20 text-xl",
};

function initials(name: string) {
  const parts = name.trim().split(" ").filter(Boolean);
  if (!parts.length) return "?";
  return parts.slice(0, 2).map((p) => p[0].toUpperCase()).join("");
}

export default function Avatar({ name, src, size = "md" }: Props) {
  if (src) {
    return <img src={src} alt={name} className={`${s[size]} rounded-full object-cover border border-slate-200`} />;
  }
  return (
    <div className={`${s[size]} rounded-full bg-gradient-to-br from-emerald-400 to-emerald-600 grid place-items-center text-white font-semibold shrink-0`}>
       {initials(name)}
     </div>
  );
}

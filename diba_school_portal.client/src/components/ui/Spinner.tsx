import { Loader2 } from "lucide-react";

interface Props {
  size?: "sm" | "md" | "lg";
  label?: string;
}

const s = { sm: "w-4 h-4", md: "w-6 h-6", lg: "w-8 h-8" };

export default function Spinner({ size = "md", label }: Props) {
  return (
    <div className="inline-flex items-center gap-2 text-surface-400">
      <Loader2 className={`${s[size]} animate-spin`} />
      {label && <span className="text-[13px]">{label}</span>}
    </div>
  );
}
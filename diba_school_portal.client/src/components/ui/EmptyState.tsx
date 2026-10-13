import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

interface Props {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: ReactNode;
}

export default function EmptyState({ icon: Icon, title, description, action }: Props) {
  return (
    <div className="flex flex-col items-center justify-center text-center py-12 px-6">
      {Icon && (
        <div className="w-14 h-14 rounded-2xl bg-surface-50 grid place-items-center mb-4">
          <Icon className="w-6 h-6 text-surface-400" strokeWidth={1.8} />
        </div>
      )}
      <p className="text-[15px] font-semibold text-surface-800">{title}</p>
      {description && <p className="mt-1 text-[13px] text-surface-400 max-w-sm">{description}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
import { forwardRef, type InputHTMLAttributes } from "react";
import type { LucideIcon } from "lucide-react";

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  icon?: LucideIcon;
  error?: boolean;
}

const Input = forwardRef<HTMLInputElement, Props>(
  ({ icon: Icon, error, className = "", ...rest }, ref) => (
    <div className="relative w-full">
      {Icon && (
        <Icon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400 pointer-events-none" />
      )}
      <input
        ref={ref}
        className={[
          "w-full text-sm rounded-xl bg-surface-50 border transition-colors outline-none",
          "placeholder:text-surface-400",
          Icon ? "pl-10 pr-4 py-2.5" : "px-4 py-2.5",
          error
            ? "border-flame-200 focus:bg-white focus:border-flame-300 focus:ring-4 focus:ring-flame-50"
            : "border-transparent focus:bg-white focus:border-surface-200 focus:ring-4 focus:ring-brand-50",
          className,
        ].join(" ")}
        {...rest}
      />
    </div>
  )
);

Input.displayName = "Input";
export default Input;
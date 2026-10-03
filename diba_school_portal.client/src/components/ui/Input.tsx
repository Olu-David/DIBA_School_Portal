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
        <Icon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none" />
      )}
      <input
        ref={ref}
        className={[
          "w-full text-sm rounded-xl bg-slate-50 border transition-colors outline-none",
          "placeholder:text-slate-400",
          Icon ? "pl-10 pr-4 py-2.5" : "px-4 py-2.5",
          error
            ? "border-rose-200 focus:bg-white focus:border-rose-300 focus:ring-4 focus:ring-rose-50"
            : "border-transparent focus:bg-white focus:border-slate-200 focus:ring-4 focus:ring-emerald-50",
          className,
        ].join(" ")}
        {...rest}
      />
    </div>
  )
);

Input.displayName = "Input";
export default Input;
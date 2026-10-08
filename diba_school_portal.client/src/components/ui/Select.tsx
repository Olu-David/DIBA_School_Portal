import { forwardRef, type SelectHTMLAttributes } from "react";
import { ChevronDown } from "lucide-react";

export interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface Props extends SelectHTMLAttributes<HTMLSelectElement> {
  options: SelectOption[];
  placeholder?: string;
  error?: boolean;
}

const Select = forwardRef<HTMLSelectElement, Props>(
  ({ options, placeholder, error, className = "", ...rest }, ref) => (
    <div className="relative w-full">
      <select
        ref={ref}
        className={[
          "w-full appearance-none px-4 py-2.5 pr-10 text-sm rounded-xl bg-slate-50 border transition-colors outline-none cursor-pointer",
          error
            ? "border-rose-200 focus:bg-white focus:border-rose-300 focus:ring-4 focus:ring-rose-50"
            : "border-transparent focus:bg-white focus:border-slate-200 focus:ring-4 focus:ring-emerald-50",
          className,
        ].join(" ")}
        {...rest}
      >
        {placeholder && (
          <option value="" disabled hidden>
            {placeholder}
          </option>
        )}
        {options.map((o) => (
          <option key={o.value} value={o.value} disabled={o.disabled}>
            {o.label}
          </option>
        ))}
      </select>
      <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400 pointer-events-none" />
    </div>
  )
);

Select.displayName = "Select";
export default Select;
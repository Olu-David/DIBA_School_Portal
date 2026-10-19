import { forwardRef, type InputHTMLAttributes } from "react";
import { Check } from "lucide-react";

interface Props extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label?: string;
}

const Checkbox = forwardRef<HTMLInputElement, Props>(
  ({ checked, className = "", label, disabled, ...rest }, ref) => (
    <label
      className={`relative inline-flex items-center cursor-pointer select-none ${
        disabled ? "cursor-not-allowed opacity-60" : ""
      }`}
    >
      <input
        ref={ref}
        type="checkbox"
        checked={checked}
        disabled={disabled}
        className="peer sr-only"
        {...rest}
      />
      <div
        className={[
          "w-5 h-5 rounded-md border transition-colors grid place-items-center",
          "peer-checked:bg-brand-600 peer-checked:border-brand-600",
          "peer-focus:ring-4 peer-focus:ring-brand-100",
          checked ? "bg-brand-600 border-brand-600" : "border-surface-300 bg-white",
          className,
        ].join(" ")}
      >
        {checked && <Check className="w-3 h-3 text-white" strokeWidth={3} />}
      </div>
      {label && <span className="ml-2 text-[13px] text-surface-700">{label}</span>}
    </label>
  )
);

Checkbox.displayName = "Checkbox";
export default Checkbox;
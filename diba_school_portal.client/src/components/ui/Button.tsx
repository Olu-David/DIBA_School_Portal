import { forwardRef, type ButtonHTMLAttributes } from "react";
import { Loader2, type LucideIcon } from "lucide-react";

type Variant = "primary" | "secondary" | "ghost" | "danger";
type Size = "sm" | "md" | "lg";

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
  icon?: LucideIcon;
  iconRight?: LucideIcon;
  fullWidth?: boolean;
}

const variants: Record<Variant, string> = {
  primary: "bg-emerald-600 text-white hover:bg-emerald-700 disabled:bg-emerald-300",
  secondary: "bg-white text-slate-700 border border-slate-200 hover:bg-slate-50 disabled:opacity-60",
  ghost: "text-slate-600 hover:bg-slate-100 hover:text-slate-900 disabled:opacity-60",
  danger: "bg-rose-600 text-white hover:bg-rose-700 disabled:bg-rose-300",
};

const rings: Record<Variant, string> = {
  primary: "focus:ring-emerald-100",
  secondary: "focus:ring-slate-100",
  ghost: "focus:ring-slate-100",
  danger: "focus:ring-rose-100",
};

const sizes: Record<Size, string> = {
  sm: "px-3 py-1.5 text-[13px] rounded-lg gap-1.5",
  md: "px-4 py-2.5 text-sm rounded-xl gap-2",
  lg: "px-5 py-3 text-[15px] rounded-xl gap-2",
};

const Button = forwardRef<HTMLButtonElement, Props>(
  (
    {
      variant = "primary",
      size = "md",
      loading,
      icon: Icon,
      iconRight: IconRight,
      fullWidth,
      className = "",
      disabled,
      children,
      ...rest
    },
    ref
  ) => (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={[
        "inline-flex items-center justify-center font-medium transition-colors",
        "focus:outline-none focus:ring-4 disabled:cursor-not-allowed",
        variants[variant],
        rings[variant],
        sizes[size],
        fullWidth ? "w-full" : "",
        className,
      ].join(" ")}
      {...rest}
    >
      {loading ? (
        <Loader2 className="w-4 h-4 animate-spin" />
      ) : (
        Icon && <Icon className="w-4 h-4" />
      )}
      {children}
      {IconRight && !loading && <IconRight className="w-4 h-4" />}
    </button>
  )
);

Button.displayName = "Button";
export default Button;
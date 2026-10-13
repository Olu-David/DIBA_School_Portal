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
  primary:   "bg-brand-600 text-white hover:bg-brand-700 disabled:bg-brand-300",
  secondary: "bg-white text-surface-700 border border-surface-200 hover:bg-surface-50 disabled:opacity-60",
  ghost:     "text-surface-600 hover:bg-surface-100 hover:text-surface-900 disabled:opacity-60",
  danger:    "bg-flame-600 text-white hover:bg-flame-700 disabled:bg-flame-300",
};

const rings: Record<Variant, string> = {
  primary:   "focus:ring-brand-100",
  secondary: "focus:ring-surface-100",
  ghost:     "focus:ring-surface-100",
  danger:    "focus:ring-flame-100",
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
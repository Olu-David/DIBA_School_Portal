import {
  forwardRef,
  type ButtonHTMLAttributes,
  type HTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import {
  ArrowLeft,
  ArrowRight,
  Check,
  ChevronDown,
  Loader2,
  Search,
  type LucideIcon,
} from "lucide-react";

/* ==========================================================
   Button
   ========================================================== */

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";
type ButtonSize = "sm" | "md" | "lg";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  icon?: LucideIcon;
  iconRight?: LucideIcon;
  fullWidth?: boolean;
}

const BUTTON_VARIANT: Record<ButtonVariant, string> = {
  primary: "bg-brand-600 text-white hover:bg-brand-700 disabled:bg-brand-300",
  secondary:
    "bg-white text-surface-700 border border-surface-200 hover:bg-surface-50 disabled:opacity-60",
  ghost:
    "text-surface-600 hover:bg-surface-100 hover:text-surface-900 disabled:opacity-60",
  danger: "bg-flame-600 text-white hover:bg-flame-700 disabled:bg-flame-300",
};

const BUTTON_FOCUS: Record<ButtonVariant, string> = {
  primary: "focus:ring-brand-100",
  secondary: "focus:ring-surface-100",
  ghost: "focus:ring-surface-100",
  danger: "focus:ring-flame-100",
};

const BUTTON_SIZE: Record<ButtonSize, string> = {
  sm: "px-3 py-1.5 text-[13px] rounded-lg gap-1.5",
  md: "px-4 py-2.5 text-sm rounded-xl gap-2",
  lg: "px-5 py-3 text-[15px] rounded-xl gap-2",
};

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
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
        BUTTON_VARIANT[variant],
        BUTTON_FOCUS[variant],
        BUTTON_SIZE[size],
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

/* ==========================================================
   Card
   ========================================================== */

type CardPadding = "none" | "sm" | "md" | "lg";

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  padding?: CardPadding;
}

const CARD_PADDING: Record<CardPadding, string> = {
  none: "",
  sm: "p-4",
  md: "p-6",
  lg: "p-8",
};

export function Card({ children, className = "", padding = "md", ...rest }: CardProps) {
  return (
    <div
      className={`bg-white rounded-2xl shadow-soft border border-surface-100 ${CARD_PADDING[padding]} ${className}`}
      {...rest}
    >
      {children}
    </div>
  );
}

/* ==========================================================
   StatCard
   ========================================================== */

type StatTone = "brand" | "flame" | "sky" | "violet" | "emerald" | "amber" | "rose";

interface StatCardProps {
  label: string;
  value: string | number;
  hint?: string;
  icon?: LucideIcon;
  tone?: StatTone;
}

const STAT_TONE: Record<StatTone, string> = {
  brand: "bg-brand-50 text-brand-700",
  flame: "bg-flame-50 text-flame-600",
  sky: "bg-brand-50 text-brand-700",
  violet: "bg-brand-50 text-brand-700",
  emerald: "bg-brand-50 text-brand-700",
  amber: "bg-flame-50 text-flame-600",
  rose: "bg-flame-50 text-flame-600",
};

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  tone = "brand",
}: StatCardProps) {
  return (
    <div className="bg-white rounded-2xl shadow-soft border border-surface-100 p-6 hover:border-surface-200 hover:shadow-md transition-all">
      <div className={`w-10 h-10 rounded-xl grid place-items-center ${STAT_TONE[tone]}`}>
        {Icon && <Icon className="w-5 h-5" strokeWidth={2} />}
      </div>
      <p className="mt-5 text-[11px] font-medium uppercase tracking-wider text-surface-400">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold text-surface-900 tracking-tight">
        {value}
      </p>
      {hint && <p className="mt-1 text-[12px] text-surface-400">{hint}</p>}
    </div>
  );
}

/* ==========================================================
   Skeleton
   ========================================================== */

export function Skeleton({ className = "" }: { className?: string }) {
  return <div className={`animate-pulse bg-surface-100 rounded-lg ${className}`} />;
}

/* ==========================================================
   Spinner
   ========================================================== */

const SPINNER_SIZE = { sm: "w-4 h-4", md: "w-6 h-6", lg: "w-8 h-8" };

export function Spinner({
  size = "md",
  label,
}: {
  size?: "sm" | "md" | "lg";
  label?: string;
}) {
  return (
    <div className="inline-flex items-center gap-2 text-surface-400">
      <Loader2 className={`${SPINNER_SIZE[size]} animate-spin`} />
      {label && <span className="text-[13px]">{label}</span>}
    </div>
  );
}

/* ==========================================================
   Badge
   ========================================================== */

type BadgeVariant =
  | "neutral"
  | "brand"
  | "flame"
  | "emerald"
  | "amber"
  | "rose"
  | "sky"
  | "violet";

const BADGE_VARIANT: Record<BadgeVariant, string> = {
  neutral: "bg-surface-100 text-surface-600",
  brand: "bg-brand-50 text-brand-700",
  flame: "bg-flame-50 text-flame-600",
  emerald: "bg-brand-50 text-brand-700",
  amber: "bg-flame-50 text-flame-600",
  rose: "bg-flame-50 text-flame-600",
  sky: "bg-brand-50 text-brand-700",
  violet: "bg-brand-50 text-brand-700",
};

export function Badge({
  children,
  variant = "neutral",
}: {
  children: ReactNode;
  variant?: BadgeVariant;
}) {
  return (
    <span
      className={`inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-lg ${BADGE_VARIANT[variant]}`}
    >
      {children}
    </span>
  );
}

/* ==========================================================
   Avatar
   ========================================================== */

const AVATAR_SIZE = {
  sm: "w-8 h-8 text-[11px]",
  md: "w-10 h-10 text-[12px]",
  lg: "w-12 h-12 text-sm",
  xl: "w-20 h-20 text-xl",
};

function initials(name: string): string {
  const parts = name.trim().split(" ").filter(Boolean);
  if (!parts.length) return "?";
  return parts
    .slice(0, 2)
    .map((p) => p[0].toUpperCase())
    .join("");
}

export function Avatar({
  name,
  src,
  size = "md",
}: {
  name: string;
  src?: string | null;
  size?: keyof typeof AVATAR_SIZE;
}) {
  const hasValidSrc =
    src && src.trim() !== "" && src !== "null" && src !== "undefined";

  return hasValidSrc ? (
    <img
      src={src}
      alt={name}
      className={`${AVATAR_SIZE[size]} rounded-full object-cover border border-surface-200`}
    />
  ) : (
    <div
      className={`${AVATAR_SIZE[size]} rounded-full bg-gradient-to-br from-brand-400 to-brand-600 grid place-items-center text-white font-semibold shrink-0`}
    >
      {initials(name)}
    </div>
  );
}

/* ==========================================================
   EmptyState
   ========================================================== */

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center text-center py-12 px-6">
      {Icon && (
        <div className="w-14 h-14 rounded-2xl bg-surface-50 grid place-items-center mb-4">
          <Icon className="w-6 h-6 text-surface-400" strokeWidth={1.8} />
        </div>
      )}
      <p className="text-[15px] font-semibold text-surface-800">{title}</p>
      {description && (
        <p className="mt-1 text-[13px] text-surface-400 max-w-sm">{description}</p>
      )}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}

/* ==========================================================
   SearchInput
   ========================================================== */

export const SearchInput = forwardRef<
  HTMLInputElement,
  InputHTMLAttributes<HTMLInputElement> & { containerClassName?: string }
>(({ containerClassName = "", className = "", ...rest }, ref) => (
  <div className={`relative w-full ${containerClassName}`}>
    <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400 pointer-events-none" />
    <input
      ref={ref}
      type="search"
      className={[
        "w-full pl-10 pr-4 py-2.5 text-sm rounded-xl bg-surface-50 border border-transparent",
        "focus:bg-white focus:border-surface-200 focus:ring-4 focus:ring-brand-50 outline-none",
        "transition-colors placeholder:text-surface-400",
        className,
      ].join(" ")}
      {...rest}
    />
  </div>
));
SearchInput.displayName = "SearchInput";

/* ==========================================================
   Pagination
   ========================================================== */

export function Pagination({
  pageNumber,
  pageSize,
  totalCount,
  onPageChange,
}: {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const from = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const to = Math.min(pageNumber * pageSize, totalCount);

  return (
    <div className="flex items-center justify-between gap-4 pt-5 mt-5 border-t border-surface-100">
      <p className="text-[12px] text-surface-400">
        Showing <span className="font-medium text-surface-700">{from}</span>-
        <span className="font-medium text-surface-700">{to}</span> of{" "}
        <span className="font-medium text-surface-700">{totalCount}</span>
      </p>
      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={() => onPageChange(pageNumber - 1)}
          disabled={pageNumber <= 1}
          className="p-2 rounded-lg text-surface-500 hover:bg-surface-100 disabled:opacity-40"
        >
          <ArrowLeft className="w-4 h-4" />
        </button>
        <span className="text-[12px] text-surface-500 px-2">
          Page {pageNumber} of {totalPages}
        </span>
        <button
          type="button"
          onClick={() => onPageChange(pageNumber + 1)}
          disabled={pageNumber >= totalPages}
          className="p-2 rounded-lg text-surface-500 hover:bg-surface-100 disabled:opacity-40"
        >
          <ArrowRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}

/* ==========================================================
   Input
   ========================================================== */

export const Input = forwardRef<
  HTMLInputElement,
  InputHTMLAttributes<HTMLInputElement> & { icon?: LucideIcon; error?: string }
>(({ icon: Icon, error, className = "", ...rest }, ref) => (
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
));
Input.displayName = "Input";

/* ==========================================================
   Select
   ========================================================== */

interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

export const Select = forwardRef<
  HTMLSelectElement,
  SelectHTMLAttributes<HTMLSelectElement> & {
    options: SelectOption[];
    placeholder?: string;
    error?: string;
  }
>(({ options, placeholder, error, className = "", ...rest }, ref) => (
  <div className="relative w-full">
    <select
      ref={ref}
      className={[
        "w-full appearance-none px-4 py-2.5 pr-10 text-sm rounded-xl bg-surface-50 border transition-colors outline-none cursor-pointer",
        error
          ? "border-flame-200 focus:bg-white focus:border-flame-300 focus:ring-4 focus:ring-flame-50"
          : "border-transparent focus:bg-white focus:border-surface-200 focus:ring-4 focus:ring-brand-50",
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
    <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400 pointer-events-none" />
  </div>
));
Select.displayName = "Select";

/* ==========================================================
   FormField
   ========================================================== */

export function FormField({
  label,
  hint,
  error,
  required,
  htmlFor,
  children,
}: {
  label?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  htmlFor?: string;
  children: ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      {label && (
        <label
          htmlFor={htmlFor}
          className="block text-[12px] font-medium text-surface-700"
        >
          {label}
          {required && <span className="text-flame-500 ml-0.5">*</span>}
        </label>
      )}
      {children}
      {error ? (
        <p className="text-[11px] text-flame-600">{error}</p>
      ) : hint ? (
        <p className="text-[11px] text-surface-400">{hint}</p>
      ) : null}
    </div>
  );
}

/* ==========================================================
   Textarea
   ========================================================== */

export const Textarea = forwardRef<
  HTMLTextAreaElement,
  TextareaHTMLAttributes<HTMLTextAreaElement> & { error?: string }
>(({ error, className = "", rows = 4, ...rest }, ref) => (
  <textarea
    ref={ref}
    rows={rows}
    className={[
      "w-full px-4 py-3 text-sm rounded-xl bg-surface-50 border transition-colors outline-none resize-none",
      "placeholder:text-surface-400",
      error
        ? "border-flame-200 focus:bg-white focus:border-flame-300 focus:ring-4 focus:ring-flame-50"
        : "border-transparent focus:bg-white focus:border-surface-200 focus:ring-4 focus:ring-brand-50",
      className,
    ].join(" ")}
    {...rest}
  />
));
Textarea.displayName = "Textarea";

/* ==========================================================
   Checkbox — clickable, keyboard-accessible
   Wrapped in <label> so clicking the visible square toggles it
   ========================================================== */

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  checked: boolean;
  label?: string;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
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
        className={`w-5 h-5 rounded-md border transition-colors grid place-items-center shrink-0
          peer-checked:bg-brand-600 peer-checked:border-brand-600
          peer-focus-visible:ring-4 peer-focus-visible:ring-brand-100
          ${checked ? "bg-brand-600 border-brand-600" : "border-surface-300 bg-white"}
          ${className}`}
      >
        {checked && <Check className="w-3 h-3 text-white" strokeWidth={3} />}
      </div>
      {label && <span className="ml-2 text-[13px] text-surface-700">{label}</span>}
    </label>
  )
);
Checkbox.displayName = "Checkbox";
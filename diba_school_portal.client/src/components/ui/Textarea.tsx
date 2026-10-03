import { forwardRef, type TextareaHTMLAttributes } from "react";

interface Props extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
}

const Textarea = forwardRef<HTMLTextAreaElement, Props>(
  ({ error, className = "", rows = 4, ...rest }, ref) => (
    <textarea
      ref={ref}
      rows={rows}
      className={[
        "w-full px-4 py-3 text-sm rounded-xl bg-slate-50 border transition-colors outline-none resize-none",
        "placeholder:text-slate-400",
        error
          ? "border-rose-200 focus:bg-white focus:border-rose-300 focus:ring-4 focus:ring-rose-50"
          : "border-transparent focus:bg-white focus:border-slate-200 focus:ring-4 focus:ring-emerald-50",
        className,
      ].join(" ")}
      {...rest}
    />
  )
);

Textarea.displayName = "Textarea";
export default Textarea;
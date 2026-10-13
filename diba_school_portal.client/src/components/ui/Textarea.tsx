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
        "w-full px-4 py-3 text-sm rounded-xl bg-surface-50 border transition-colors outline-none resize-none",
        "placeholder:text-surface-400",
        error
          ? "border-flame-200 focus:bg-white focus:border-flame-300 focus:ring-4 focus:ring-flame-50"
          : "border-transparent focus:bg-white focus:border-surface-200 focus:ring-4 focus:ring-brand-50",
        className,
      ].join(" ")}
      {...rest}
    />
  )
);

Textarea.displayName = "Textarea";
export default Textarea;
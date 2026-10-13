import type { ReactNode } from "react";

interface Props {
  label?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  htmlFor?: string;
  children: ReactNode;
}

export default function FormField({ label, hint, error, required, htmlFor, children }: Props) {
  return (
    <div className="space-y-1.5">
      {label && (
        <label htmlFor={htmlFor} className="block text-[12px] font-medium text-surface-700">
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
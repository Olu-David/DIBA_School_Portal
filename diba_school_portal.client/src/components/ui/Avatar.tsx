import { useEffect, useState } from "react";

interface Props {
  name: string;
  src?: string | null;
  size?: "sm" | "md" | "lg" | "xl";
}

const s = {
  sm: "w-8 h-8 text-[11px]",
  md: "w-10 h-10 text-[12px]",
  lg: "w-12 h-12 text-sm",
  xl: "w-20 h-20 text-xl",
};

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (!parts.length) return "?";
  return parts
    .slice(0, 2)
    .map((p) => p[0].toUpperCase())
    .join("");
}

export default function Avatar({ name, src, size = "md" }: Props) {
  const [errored, setErrored] = useState(false);

  // Reset when src changes — otherwise a bad URL (e.g. a transient 404)
  // would leave the avatar stuck on initials forever, even after re-upload.
  useEffect(() => {
    setErrored(false);
  }, [src]);

  if (src && !errored) {
    return (
      <img
        src={src}
        alt={name}
        onError={() => setErrored(true)}
        className={`${s[size]} rounded-full object-cover border border-surface-200 shrink-0`}
      />
    );
  }

  return (
    <div
      className={`${s[size]} rounded-full bg-gradient-to-br from-brand-400 to-brand-600 grid place-items-center text-white font-semibold shrink-0`}
    >
      {initials(name)}
    </div>
  );
}
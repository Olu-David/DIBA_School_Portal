interface Props {
  className?: string;
  tone?: "emerald" | "white" | "gold";
}

export default function SchoolCrest({ className = "w-10 h-10", tone = "emerald" }: Props) {
  const palette = {
    emerald: { shield: "#047857", book: "#FFFFFF", flame: "#F59E0B", ring: "#065F46" },
    white:   { shield: "#FFFFFF", book: "#047857", flame: "#F59E0B", ring: "#E5E7EB" },
    gold:    { shield: "#F59E0B", book: "#FFFFFF", flame: "#047857", ring: "#B45309" },
  }[tone];

  return (
    <svg
      viewBox="0 0 64 72"
      className={className}
      role="img"
      aria-label="Dominion Intelligent Builders Academy crest"
    >
      {/* Shield */}
      <path
        d="M32 2 L60 12 V40 C60 56 46 66 32 70 C18 66 4 56 4 40 V12 Z"
        fill={palette.shield}
        stroke={palette.ring}
        strokeWidth="1.5"
      />

      {/* Open book */}
      <path
        d="M16 30 C22 28 28 28 32 31 C36 28 42 28 48 30 V46 C42 44 36 44 32 47 C28 44 22 44 16 46 Z"
        fill={palette.book}
        opacity="0.95"
      />
      <line x1="32" y1="31" x2="32" y2="47" stroke={palette.shield} strokeWidth="1" />

      {/* Torch flame — knowledge */}
      <path
        d="M32 10 C29 14 28 17 30 20 C31 22 33 22 34 20 C36 17 35 14 32 10 Z"
        fill={palette.flame}
      />
      <rect x="30.5" y="20" width="3" height="6" rx="0.6" fill={palette.shield} />
    </svg>
  );
}
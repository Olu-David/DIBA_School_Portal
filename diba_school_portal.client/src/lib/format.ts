export const formatNaira = (amount: number): string =>
  `\u20A6${amount.toLocaleString("en-NG", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;

export const formatNairaShort = (amount: number): string => {
  if (amount >= 1_000_000) return `\u20A6${(amount / 1_000_000).toFixed(1)}M`;
  if (amount >= 1_000) return `\u20A6${(amount / 1_000).toFixed(1)}K`;
  return `\u20A6${amount.toFixed(0)}`;
};

export const formatDate = (iso: string): string =>
  new Date(iso).toLocaleDateString("en-NG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });

export const formatDateTime = (iso: string): string =>
  new Date(iso).toLocaleString("en-NG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });

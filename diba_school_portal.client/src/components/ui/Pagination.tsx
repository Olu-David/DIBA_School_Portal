import { ChevronLeft, ChevronRight } from "lucide-react";

interface Props {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({ pageNumber, pageSize, totalCount, onPageChange }: Props) {
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
          <ChevronLeft className="w-4 h-4" />
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
          <ChevronRight className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}
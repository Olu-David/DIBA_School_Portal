import { forwardRef, type InputHTMLAttributes } from "react";
import { Search } from "lucide-react";

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  containerClassName?: string;
}

const SearchInput = forwardRef<HTMLInputElement, Props>(
  ({ containerClassName = "", className = "", ...rest }, ref) => (
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
  )
);

SearchInput.displayName = "SearchInput";
export default SearchInput;
"use client";

import { ArrowDownUp, ArrowDown, ArrowUp } from "lucide-react";
import { SecondaryButton, Select } from "@/components/ui";

export type SortOption = {
  value: string;
  label: string;
};

export function SortControl({
  sortBy,
  sortDirection,
  options,
  onSortByChange,
  onSortDirectionChange
}: {
  sortBy: string;
  sortDirection: "asc" | "desc";
  options: SortOption[];
  onSortByChange: (value: string) => void;
  onSortDirectionChange: (value: "asc" | "desc") => void;
}) {
  const activeOption = options.find((o) => o.value === sortBy);

  return (
    <div className="mb-4 flex items-center gap-2">
      <span className="text-sm text-slate-400">Sort:</span>
      <Select
        value={sortBy}
        onChange={(e) => onSortByChange(e.target.value)}
        className="w-auto min-w-[130px]"
      >
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </Select>
      <SecondaryButton
        type="button"
        onClick={() =>
          onSortDirectionChange(sortDirection === "asc" ? "desc" : "asc")
        }
        className="inline-flex items-center gap-1"
        title={`Switch to ${sortDirection === "asc" ? "descending" : "ascending"} order`}
      >
        {sortDirection === "asc" ? (
          <ArrowUp aria-hidden="true" className="size-4" />
        ) : (
          <ArrowDown aria-hidden="true" className="size-4" />
        )}
        <ArrowDownUp aria-hidden="true" className="size-3 text-slate-500" />
      </SecondaryButton>
      {activeOption && (
        <span className="text-xs text-slate-500">
          {sortDirection === "asc" ? "A → Z" : "Z → A"}
        </span>
      )}
    </div>
  );
}

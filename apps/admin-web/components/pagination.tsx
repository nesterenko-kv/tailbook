"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { SecondaryButton } from "@/components/ui";

export function Pagination({
  page,
  pageSize,
  totalCount,
  onPageChange
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="mt-4 flex items-center justify-between gap-4 text-sm">
      <p className="text-slate-400">
        Page {page} of {totalPages}
        <span className="ml-2 text-slate-500">({totalCount} total)</span>
      </p>
      <div className="flex gap-2">
        <SecondaryButton
          type="button"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          className="inline-flex items-center gap-1"
        >
          <ChevronLeft aria-hidden="true" className="size-4" />
          Previous
        </SecondaryButton>
        <SecondaryButton
          type="button"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          className="inline-flex items-center gap-1"
        >
          Next
          <ChevronRight aria-hidden="true" className="size-4" />
        </SecondaryButton>
      </div>
    </div>
  );
}

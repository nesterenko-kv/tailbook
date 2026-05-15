"use client";

import { Command } from "cmdk";
import { ArrowRight, Search } from "lucide-react";
import { useRouter } from "next/navigation";

export type AdminCommandItem = {
  href: string;
  label: string;
  keywords?: string[];
};

export function AdminCommandPalette({
  open,
  onOpenChange,
  items
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  items: AdminCommandItem[];
}) {
  const router = useRouter();

  function navigateTo(href: string) {
    onOpenChange(false);
    router.push(href);
  }

  return (
    <Command.Dialog
      open={open}
      onOpenChange={onOpenChange}
      label="Admin navigation"
      loop
      overlayClassName="fixed inset-0 z-40 bg-slate-950/75 backdrop-blur-sm"
      contentClassName="fixed left-1/2 top-16 z-50 w-[min(680px,calc(100vw-2rem))] -translate-x-1/2 overflow-hidden rounded-3xl border border-slate-700 bg-slate-950 shadow-2xl shadow-black/40"
      className="bg-slate-950"
    >
      <div className="flex items-center gap-3 border-b border-slate-800 px-4 py-3">
        <Search aria-hidden="true" className="size-5 text-slate-500" />
        <Command.Input
          placeholder="Search admin"
          className="min-w-0 flex-1 bg-transparent text-sm text-white outline-none placeholder:text-slate-500"
        />
      </div>
      <Command.List className="max-h-[420px] overflow-y-auto p-2">
        <Command.Empty className="px-4 py-8 text-center text-sm text-slate-400">No matching pages</Command.Empty>
        <Command.Group heading="Pages" className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500 [&_[cmdk-group-heading]]:px-3 [&_[cmdk-group-heading]]:py-2">
          {items.map((item) => (
            <Command.Item
              key={item.href}
              value={item.label}
              keywords={[item.href, ...(item.keywords ?? [])]}
              onSelect={() => navigateTo(item.href)}
              className="flex cursor-pointer items-center justify-between rounded-2xl px-3 py-3 text-sm text-slate-200 outline-none transition data-[selected=true]:bg-emerald-500 data-[selected=true]:text-slate-950"
            >
              <span>{item.label}</span>
              <ArrowRight aria-hidden="true" className="size-4 opacity-70" />
            </Command.Item>
          ))}
        </Command.Group>
      </Command.List>
    </Command.Dialog>
  );
}

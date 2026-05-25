"use client";

import type { ReactNode } from "react";
import { useEffect, useRef } from "react";
import { PrimaryButton, SecondaryButton } from "@/components/ui";

export function ConfirmDialog({
  open,
  onOpenChange,
  onConfirm,
  title,
  description,
  confirmLabel = "Confirm",
  confirmTone = "danger",
  children
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
  title: string;
  description?: string;
  confirmLabel?: string;
  confirmTone?: "danger" | "primary";
  children?: ReactNode;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;
    if (open && !el.open) {
      el.showModal();
    } else if (!open && el.open) {
      el.close();
    }
  }, [open]);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;
    function handleClose() {
      onOpenChange(false);
    }
    el.addEventListener("close", handleClose);
    return () => el.removeEventListener("close", handleClose);
  }, [onOpenChange]);

  if (!open) return null;

  const confirmStyles =
    confirmTone === "danger"
      ? "bg-rose-500 text-white hover:bg-rose-400"
      : "";

  return (
    <dialog
      ref={dialogRef}
      onClick={(e) => { if (e.target === dialogRef.current) onOpenChange(false); }}
      className="w-[min(480px,calc(100vw-2rem))] rounded-3xl border border-slate-700 bg-slate-950 p-0 text-slate-100 shadow-2xl shadow-black/50 backdrop:bg-slate-950/75 backdrop:backdrop-blur-sm open:flex"
    >
      <div className="flex flex-col p-6">
        <h2 className="text-lg font-semibold text-white">{title}</h2>
        {description ? <p className="mt-2 text-sm text-slate-400">{description}</p> : null}
        {children ? <div className="mt-4">{children}</div> : null}
        <div className="mt-6 flex justify-end gap-3">
          <SecondaryButton type="button" onClick={() => onOpenChange(false)}>
            Cancel
          </SecondaryButton>
          <PrimaryButton
            type="button"
            className={confirmStyles}
            onClick={() => {
              onConfirm();
              onOpenChange(false);
            }}
          >
            {confirmLabel}
          </PrimaryButton>
        </div>
      </div>
    </dialog>
  );
}

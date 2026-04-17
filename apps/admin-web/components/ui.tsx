import Link from "next/link";
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";

function cn(...values: Array<string | false | null | undefined>) {
  return values.filter(Boolean).join(" ");
}

export function PageHeader({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
      <div>
        {eyebrow ? <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-400">{eyebrow}</p> : null}
        <h1 className="mt-1 text-3xl font-semibold text-white">{title}</h1>
        {description ? <p className="mt-2 max-w-3xl text-sm text-slate-400">{description}</p> : null}
      </div>
      {action ? <div>{action}</div> : null}
    </div>
  );
}

export function Card({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-5 shadow-lg shadow-black/10">
      <div className="mb-4">
        <h2 className="text-lg font-semibold text-white">{title}</h2>
        {description ? <p className="mt-1 text-sm text-slate-400">{description}</p> : null}
      </div>
      {children}
    </section>
  );
}

export function Badge({ children, tone = "default" }: { children: ReactNode; tone?: "default" | "success" | "warning" | "danger" }) {
  const classes = {
    default: "border-slate-700 bg-slate-800 text-slate-200",
    success: "border-emerald-700/40 bg-emerald-500/10 text-emerald-300",
    warning: "border-amber-700/40 bg-amber-500/10 text-amber-300",
    danger: "border-rose-700/40 bg-rose-500/10 text-rose-300"
  } as const;

  return <span className={cn("inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-medium", classes[tone])}>{children}</span>;
}

export function ErrorBanner({ message }: { message?: string | null }) {
  if (!message) return null;
  return <div className="rounded-2xl border border-rose-700/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{message}</div>;
}

export function SuccessBanner({ message }: { message?: string | null }) {
  if (!message) return null;
  return <div className="rounded-2xl border border-emerald-700/40 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-200">{message}</div>;
}

export function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <label className="block space-y-2">
      <span className="block text-sm font-medium text-slate-200">{label}</span>
      {children}
      {hint ? <span className="block text-xs text-slate-500">{hint}</span> : null}
    </label>
  );
}

export function Input(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} className={cn("w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-white outline-none transition placeholder:text-slate-500 focus:border-emerald-500", props.className)} />;
}

export function TextArea(props: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea {...props} className={cn("min-h-[96px] w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 text-sm text-white outline-none transition placeholder:text-slate-500 focus:border-emerald-500", props.className)} />;
}

export function Select(props: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select {...props} className={cn("w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm text-white outline-none transition focus:border-emerald-500", props.className)} />;
}

export function PrimaryButton(props: ButtonHTMLAttributes<HTMLButtonElement>) {
  return <button {...props} className={cn("rounded-2xl bg-emerald-500 px-4 py-2.5 text-sm font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60", props.className)} />;
}

export function SecondaryButton(props: ButtonHTMLAttributes<HTMLButtonElement>) {
  return <button {...props} className={cn("rounded-2xl border border-slate-700 bg-slate-900 px-4 py-2.5 text-sm font-medium text-slate-100 transition hover:border-slate-500", props.className)} />;
}

export function LinkButton({ href, children, className }: { href: string; children: ReactNode; className?: string }) {
  return <Link href={href} className={cn("inline-flex items-center rounded-2xl border border-slate-700 bg-slate-900 px-4 py-2.5 text-sm font-medium text-slate-100 transition hover:border-slate-500", className)}>{children}</Link>;
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-slate-700 px-5 py-8 text-center">
      <h3 className="text-sm font-medium text-slate-200">{title}</h3>
      {description ? <p className="mt-2 text-sm text-slate-400">{description}</p> : null}
    </div>
  );
}

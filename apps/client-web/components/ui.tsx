import type { ButtonHTMLAttributes, InputHTMLAttributes, PropsWithChildren, TextareaHTMLAttributes } from "react";

export function Card({ children, className }: PropsWithChildren<{ className?: string }>) {
    return <div className={`rounded-2xl border border-slate-800 bg-slate-900/70 p-5 shadow-sm ${className ?? ""}`}>{children}</div>;
}

export function Button(props: ButtonHTMLAttributes<HTMLButtonElement>) {
    const { className, ...rest } = props;
    return <button className={`rounded-xl bg-emerald-500 px-4 py-2 text-sm font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60 ${className ?? ""}`} {...rest} />;
}

export function Input(props: InputHTMLAttributes<HTMLInputElement>) {
    const { className, ...rest } = props;
    return <input className={`w-full rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm outline-none ring-0 placeholder:text-slate-500 ${className ?? ""}`} {...rest} />;
}

export function Textarea(props: TextareaHTMLAttributes<HTMLTextAreaElement>) {
    const { className, ...rest } = props;
    return <textarea className={`w-full rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm outline-none ring-0 placeholder:text-slate-500 ${className ?? ""}`} {...rest} />;
}

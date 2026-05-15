import * as React from "react";
import { cn } from "@/lib/cn";

export function Button({ className, variant = "default", size = "default", ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "default" | "outline" | "ghost" | "secondary" | "destructive"; size?: "default" | "sm" | "lg" | "icon" }) {
  const variants = {
    default: "bg-primary text-primary-foreground hover:opacity-95",
    outline: "border border-border bg-card text-foreground hover:bg-accent",
    ghost: "bg-transparent text-foreground hover:bg-accent",
    secondary: "bg-secondary text-secondary-foreground hover:bg-accent",
    destructive: "bg-destructive text-destructive-foreground hover:opacity-95"
  };
  const sizes = {
    default: "h-11 px-5 text-sm",
    sm: "h-9 px-4 text-sm",
    lg: "h-12 px-6 text-base",
    icon: "h-10 w-10"
  };

  return <button className={cn("inline-flex items-center justify-center gap-2 rounded-lg font-medium transition disabled:cursor-not-allowed disabled:opacity-50", variants[variant], sizes[size], className)} {...props} />;
}

export function Card({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("surface-card", className)} {...props} />;
}

export function Badge({ className, variant = "default", ...props }: React.HTMLAttributes<HTMLSpanElement> & { variant?: "default" | "secondary" | "outline" | "destructive" }) {
  const variants = {
    default: "bg-primary/15 text-foreground",
    secondary: "bg-secondary text-muted-foreground",
    outline: "border border-border bg-card text-foreground",
    destructive: "bg-red-100 text-red-700"
  };

  return <span className={cn("inline-flex items-center rounded-full px-3 py-1 text-xs font-medium", variants[variant], className)} {...props} />;
}

export function Input({ className, ...props }: React.InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn("flex h-12 w-full rounded-lg border border-border bg-[var(--input-background)] px-4 text-sm text-foreground outline-none placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/20", className)} {...props} />;
}

export function Textarea({ className, ...props }: React.TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cn("flex min-h-28 w-full rounded-lg border border-border bg-[var(--input-background)] px-4 py-3 text-sm text-foreground outline-none placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/20", className)} {...props} />;
}

export function Label({ className, ...props }: React.LabelHTMLAttributes<HTMLLabelElement>) {
  return <label className={cn("text-sm font-medium text-foreground", className)} {...props} />;
}

export function Checkbox({ className, checked, ...props }: Omit<React.InputHTMLAttributes<HTMLInputElement>, "type">) {
  return <input type="checkbox" checked={checked} className={cn("h-4 w-4 rounded border border-border accent-[var(--primary)]", className)} {...props} />;
}

export function Separator({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("h-px w-full bg-border", className)} {...props} />;
}

export function Avatar({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("relative flex shrink-0 overflow-hidden rounded-full bg-secondary", className)} {...props} />;
}

export function AvatarImage({ className, alt = "", ...props }: React.ImgHTMLAttributes<HTMLImageElement>) {
  return <img alt={alt} className={cn("h-full w-full object-cover", className)} {...props} />;
}

export function AvatarFallback({ className, ...props }: React.HTMLAttributes<HTMLSpanElement>) {
  return <span className={cn("flex h-full w-full items-center justify-center text-sm font-medium text-muted-foreground", className)} {...props} />;
}

export function NativeSelect({ className, children, ...props }: React.SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={cn("flex h-12 w-full rounded-lg border border-border bg-[var(--input-background)] px-4 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary/20", className)} {...props}>{children}</select>;
}

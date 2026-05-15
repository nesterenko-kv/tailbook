"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { LogOut, Search } from "lucide-react";
import { AdminCommandPalette, type AdminCommandItem } from "@/components/admin-command-palette";
import { AuthGuard } from "@/components/auth-guard";
import { getAdminDisplayName, getAdminEmail, revokeAdminSession } from "@/lib/auth";

const nav: AdminCommandItem[] = [
  { href: "/clients", label: "Clients", keywords: ["customers", "accounts"] },
  { href: "/pets", label: "Pets", keywords: ["animals", "profiles"] },
  { href: "/catalog/procedures", label: "Procedures", keywords: ["services"] },
  { href: "/catalog/offers", label: "Catalog", keywords: ["offers", "service catalog"] },
  { href: "/pricing", label: "Pricing", keywords: ["rates"] },
  { href: "/groomers", label: "Groomers", keywords: ["staff"] },
  { href: "/booking-requests", label: "Booking requests", keywords: ["requests", "leads"] },
  { href: "/appointments", label: "Appointments", keywords: ["calendar"] },
  { href: "/visits", label: "Visits", keywords: ["operations"] },
  { href: "/iam", label: "IAM", keywords: ["permissions", "roles"] },
  { href: "/security", label: "Security", keywords: ["account", "mfa", "recovery"] },
  { href: "/notifications", label: "Notifications", keywords: ["jobs", "messages"] },
  { href: "/audit", label: "Audit", keywords: ["access", "events"] }
];

export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [commandOpen, setCommandOpen] = useState(false);
  const email = getAdminEmail();
  const displayName = getAdminDisplayName();

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setCommandOpen((open) => !open);
      }
    }

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <AuthGuard>
      <div className="min-h-screen bg-slate-950 text-slate-100">
        <AdminCommandPalette open={commandOpen} onOpenChange={setCommandOpen} items={nav} />
        <div className="grid min-h-screen lg:grid-cols-[280px_1fr]">
          <aside className="border-r border-slate-800 bg-slate-950/70 p-5">
            <div className="rounded-3xl border border-slate-800 bg-slate-900/70 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-400">Tailbook</p>
              <h1 className="mt-2 text-xl font-semibold text-white">Admin Web MVP</h1>
              <p className="mt-2 text-sm text-slate-400">{displayName ?? email ?? "Authenticated admin"}</p>
            </div>

            <button
              type="button"
              className="mt-4 flex w-full items-center gap-3 rounded-2xl border border-slate-700 bg-slate-900 px-4 py-3 text-left text-sm text-slate-300 transition hover:border-slate-500 hover:bg-slate-800"
              onClick={() => setCommandOpen(true)}
            >
              <Search aria-hidden="true" className="size-4 text-slate-500" />
              <span>Search admin</span>
            </button>

            <nav className="mt-5 grid gap-2">
              {nav.map((item) => {
                const active = pathname === item.href || pathname.startsWith(`${item.href}/`);
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={`rounded-2xl px-4 py-3 text-sm transition ${active ? "bg-emerald-500 text-slate-950" : "bg-slate-900 text-slate-200 hover:bg-slate-800"}`}
                  >
                    {item.label}
                  </Link>
                );
              })}
            </nav>

            <button
              type="button"
              className="mt-6 flex w-full items-center justify-center gap-2 rounded-2xl border border-slate-700 px-4 py-3 text-sm text-slate-200 transition hover:border-slate-500"
              onClick={() => {
                void revokeAdminSession().finally(() => router.replace("/login"));
              }}
            >
              <LogOut aria-hidden="true" className="size-4" />
              <span>Sign out</span>
            </button>
          </aside>

          <main className="p-4 md:p-6 lg:p-8">{children}</main>
        </div>
      </div>
    </AuthGuard>
  );
}

"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import type { ReactNode } from "react";
import { AuthGuard } from "@/components/auth-guard";
import { clearAdminSession, getAdminDisplayName, getAdminEmail } from "@/lib/auth";

const nav = [
  { href: "/clients", label: "Clients" },
  { href: "/pets", label: "Pets" },
  { href: "/catalog/procedures", label: "Procedures" },
  { href: "/catalog/offers", label: "Catalog" },
  { href: "/pricing", label: "Pricing" },
  { href: "/groomers", label: "Groomers" },
  { href: "/booking-requests", label: "Booking requests" },
  { href: "/appointments", label: "Appointments" },
  { href: "/visits", label: "Visits" },
  { href: "/iam", label: "IAM" },
  { href: "/audit", label: "Audit" }
];

export function AdminShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const email = getAdminEmail();
  const displayName = getAdminDisplayName();

  return (
    <AuthGuard>
      <div className="min-h-screen bg-slate-950 text-slate-100">
        <div className="grid min-h-screen lg:grid-cols-[280px_1fr]">
          <aside className="border-r border-slate-800 bg-slate-950/70 p-5">
            <div className="rounded-3xl border border-slate-800 bg-slate-900/70 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-400">Tailbook</p>
              <h1 className="mt-2 text-xl font-semibold text-white">Admin Web MVP</h1>
              <p className="mt-2 text-sm text-slate-400">{displayName ?? email ?? "Authenticated admin"}</p>
            </div>

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
              className="mt-6 w-full rounded-2xl border border-slate-700 px-4 py-3 text-sm text-slate-200 transition hover:border-slate-500"
              onClick={() => {
                clearAdminSession();
                router.replace('/login');
              }}
            >
              Sign out
            </button>
          </aside>

          <main className="p-4 md:p-6 lg:p-8">{children}</main>
        </div>
      </div>
    </AuthGuard>
  );
}

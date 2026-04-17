"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { clearSession, getStoredEmail } from "@/lib/auth";

const navigation = [
    { href: "/pets", label: "My Pets" },
    { href: "/booking-request", label: "Booking Request" },
    { href: "/appointments", label: "My Appointments" },
    { href: "/profile", label: "Contact Preferences" }
];

export function ClientShell({ children }: Readonly<{ children: React.ReactNode }>) {
    const pathname = usePathname();
    const router = useRouter();
    const email = getStoredEmail();

    return (
        <div className="min-h-screen bg-slate-950 text-slate-50">
            <header className="border-b border-slate-900 bg-slate-950/90 backdrop-blur">
                <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
                    <div>
                        <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Tailbook</p>
                        <h1 className="text-lg font-semibold">Client Portal</h1>
                    </div>
                    <div className="flex items-center gap-4 text-sm text-slate-300">
                        <span>{email ?? "Client"}</span>
                        <button
                            type="button"
                            onClick={() => {
                                clearSession();
                                router.push("/login");
                            }}
                            className="rounded-lg border border-slate-700 px-3 py-2 hover:bg-slate-900"
                        >
                            Logout
                        </button>
                    </div>
                </div>
                <nav className="mx-auto flex max-w-6xl gap-2 px-6 pb-4 text-sm">
                    {navigation.map((item) => {
                        const isActive = pathname === item.href || pathname?.startsWith(`${item.href}/`);
                        return (
                            <Link
                                key={item.href}
                                href={item.href}
                                className={`rounded-full px-3 py-2 ${isActive ? "bg-emerald-500/15 text-emerald-300" : "text-slate-300 hover:bg-slate-900"}`}
                            >
                                {item.label}
                            </Link>
                        );
                    })}
                </nav>
            </header>
            <main className="mx-auto max-w-6xl px-6 py-8">{children}</main>
        </div>
    );
}

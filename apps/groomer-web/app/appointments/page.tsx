"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { GroomerShell } from "@/components/groomer-shell";
import { apiRequest, ApiError } from "@/lib/api";

type AppointmentListResponse = {
    items: AppointmentCard[];
    page: number;
    pageSize: number;
    totalCount: number;
};

type AppointmentCard = {
    id: string;
    petId: string;
    petDisplayName: string;
    breedName: string;
    startAtUtc: string;
    endAtUtc: string;
    status: string;
    reservedMinutes: number;
    serviceLabels: string[];
};

export default function AppointmentsPage() {
    const [items, setItems] = useState<AppointmentCard[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const now = new Date();
        const fromUtc = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
        const toUtc = new Date(now.getTime() + 14 * 24 * 60 * 60 * 1000).toISOString();

        apiRequest<AppointmentListResponse>(`/api/groomer/me/appointments?fromUtc=${encodeURIComponent(fromUtc)}&toUtc=${encodeURIComponent(toUtc)}&page=1&pageSize=30`)
            .then((response) => setItems(response.items))
            .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load appointments."))
            .finally(() => setIsLoading(false));
    }, []);

    return (
        <GroomerShell>
            <section className="space-y-6">
                <header className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
                    <p className="text-sm text-emerald-300">Assigned operational schedule</p>
                    <h1 className="mt-1 text-3xl font-semibold">My appointments</h1>
                    <p className="mt-2 text-sm text-slate-300">This view is privacy-safe and excludes CRM contacts by design.</p>
                </header>

                {isLoading ? <p className="text-slate-300">Loading appointments…</p> : null}
                {error ? <p className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{error}</p> : null}

                <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    {items.map((item) => (
                        <Link key={item.id} href={`/appointments/${item.id}`} className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5 transition hover:border-emerald-500/40 hover:bg-slate-900">
                            <div className="flex items-start justify-between gap-4">
                                <div>
                                    <h2 className="text-lg font-medium">{item.petDisplayName}</h2>
                                    <p className="text-sm text-slate-300">{item.breedName}</p>
                                </div>
                                <span className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-200">{item.status}</span>
                            </div>
                            <p className="mt-4 text-sm text-slate-300">{new Date(item.startAtUtc).toLocaleString()} → {new Date(item.endAtUtc).toLocaleTimeString()}</p>
                            <p className="mt-2 text-sm text-slate-400">Reserved: {item.reservedMinutes} min</p>
                            <div className="mt-4 flex flex-wrap gap-2">
                                {item.serviceLabels.map((label) => (
                                    <span key={label} className="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">{label}</span>
                                ))}
                            </div>
                        </Link>
                    ))}
                </section>
            </section>
        </GroomerShell>
    );
}

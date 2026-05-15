"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { GroomerShell } from "@/components/groomer-shell";
import { apiRequest, ApiError } from "@/lib/api";
import {
    filterAppointmentQueue,
    getAppointmentStatusDisplay,
    type AppointmentQueueRange,
    type AppointmentStatusTone
} from "@/lib/workflow-helpers";

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
    startAt: string;
    endAt: string;
    status: string;
    reservedMinutes: number;
    serviceLabels: string[];
};

const rangeOptions: Array<{ value: AppointmentQueueRange; label: string }> = [
    { value: "today", label: "Today" },
    { value: "upcoming", label: "Upcoming" },
    { value: "all", label: "All" }
];

const statusOptions = [
    { value: "all", label: "All statuses" },
    { value: "Scheduled", label: "Scheduled" },
    { value: "Confirmed", label: "Confirmed" },
    { value: "CheckedIn", label: "Checked in" },
    { value: "InProgress", label: "In progress" },
    { value: "Completed", label: "Completed" }
];

const statusToneClasses: Record<AppointmentStatusTone, string> = {
    neutral: "border-slate-700 text-slate-200",
    active: "border-emerald-500/30 bg-emerald-500/10 text-emerald-200",
    complete: "border-sky-500/30 bg-sky-500/10 text-sky-200",
    blocked: "border-rose-500/30 bg-rose-500/10 text-rose-200"
};

export default function AppointmentsPage() {
    const [items, setItems] = useState<AppointmentCard[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [range, setRange] = useState<AppointmentQueueRange>("today");
    const [status, setStatus] = useState("all");

    const loadAppointments = useCallback(async () => {
        const now = new Date();
        const from = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
        const to = new Date(now.getTime() + 14 * 24 * 60 * 60 * 1000).toISOString();

        setIsLoading(true);
        setError(null);

        try {
            const response = await apiRequest<AppointmentListResponse>(`/api/groomer/me/appointments?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&page=1&pageSize=30`);
            setItems(response.items);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load appointments.");
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadAppointments();
    }, [loadAppointments]);

    const filteredItems = useMemo(
        () => filterAppointmentQueue(items, { range, status }),
        [items, range, status]
    );

    const hasLoadedItems = items.length > 0;

    return (
        <GroomerShell>
            <section className="space-y-6">
                <header className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
                    <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                        <div>
                            <p className="text-sm text-emerald-300">Assigned operational schedule</p>
                            <h1 className="mt-1 text-3xl font-semibold">My appointments</h1>
                            <p className="mt-2 text-sm text-slate-300">This view is privacy-safe and excludes CRM contacts by design.</p>
                        </div>
                        <button
                            type="button"
                            onClick={() => void loadAppointments()}
                            disabled={isLoading}
                            className="w-fit rounded-2xl border border-slate-700 px-4 py-2 text-sm text-slate-100 transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                            {isLoading ? "Refreshing…" : "Refresh"}
                        </button>
                    </div>
                </header>

                <section className="flex flex-col gap-3 rounded-3xl border border-slate-800 bg-slate-900/50 p-4 md:flex-row md:items-center md:justify-between">
                    <div className="flex flex-wrap gap-2">
                        {rangeOptions.map((option) => (
                            <button
                                key={option.value}
                                type="button"
                                onClick={() => setRange(option.value)}
                                className={`rounded-full px-4 py-2 text-sm transition ${range === option.value ? "bg-emerald-500 text-slate-950" : "border border-slate-700 text-slate-200 hover:bg-slate-800"}`}
                            >
                                {option.label}
                            </button>
                        ))}
                    </div>
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                        <select
                            value={status}
                            onChange={(event) => setStatus(event.target.value)}
                            className="rounded-2xl border border-slate-700 bg-slate-950 px-4 py-2 text-sm text-slate-100 outline-none focus:border-emerald-500"
                        >
                            {statusOptions.map((option) => (
                                <option key={option.value} value={option.value}>{option.label}</option>
                            ))}
                        </select>
                        <span className="text-sm text-slate-400">{filteredItems.length} of {items.length}</span>
                    </div>
                </section>

                {isLoading && !hasLoadedItems ? <p className="rounded-3xl border border-slate-800 bg-slate-900/50 px-5 py-4 text-sm text-slate-300">Loading appointments…</p> : null}
                {error ? (
                    <div className="flex flex-col gap-3 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200 sm:flex-row sm:items-center sm:justify-between">
                        <p>{error}</p>
                        <button type="button" onClick={() => void loadAppointments()} className="w-fit rounded-xl border border-rose-400/40 px-3 py-2 text-rose-100 hover:bg-rose-500/10">
                            Retry
                        </button>
                    </div>
                ) : null}

                {!isLoading && !error && filteredItems.length === 0 ? (
                    <section className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6">
                        <h2 className="text-lg font-medium">No assigned appointments in this view</h2>
                        <p className="mt-2 text-sm text-slate-400">Try another range or status.</p>
                    </section>
                ) : null}

                <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                    {filteredItems.map((item) => {
                        const statusDisplay = getAppointmentStatusDisplay(item.status);
                        return (
                            <Link key={item.id} href={`/appointments/${item.id}`} className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5 transition hover:border-emerald-500/40 hover:bg-slate-900">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <h2 className="text-lg font-medium">{item.petDisplayName}</h2>
                                        <p className="text-sm text-slate-300">{item.breedName}</p>
                                    </div>
                                    <span className={`rounded-full border px-3 py-1 text-xs ${statusToneClasses[statusDisplay.tone]}`}>{statusDisplay.label}</span>
                                </div>
                                <p className="mt-4 text-sm text-slate-300">{new Date(item.startAt).toLocaleString()} → {new Date(item.endAt).toLocaleTimeString()}</p>
                                <p className="mt-2 text-sm text-slate-400">Reserved: {item.reservedMinutes} min</p>
                                <div className="mt-4 flex flex-wrap gap-2">
                                    {item.serviceLabels.map((label) => (
                                        <span key={label} className="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">{label}</span>
                                    ))}
                                </div>
                            </Link>
                        );
                    })}
                </section>
            </section>
        </GroomerShell>
    );
}

"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { apiRequest } from "@/lib/api";
import type { ClientAppointmentSummary } from "@/lib/types";
import { Card } from "@/components/ui";

export default function ClientAppointmentsPage() {
    const [appointments, setAppointments] = useState<ClientAppointmentSummary[]>([]);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        apiRequest<ClientAppointmentSummary[]>("/api/client/appointments")
            .then(setAppointments)
            .catch((err: Error) => setError(err.message));
    }, []);

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Appointments</p>
                <h2 className="mt-2 text-3xl font-semibold">My appointments</h2>
                <p className="mt-2 text-sm text-slate-400">Only your own appointments are visible here.</p>
            </div>
            {error ? <p className="text-sm text-rose-300">{error}</p> : null}
            <div className="grid gap-4">
                {appointments.map((appointment) => (
                    <Link key={appointment.id} href={`/appointments/${appointment.id}`}>
                        <Card className="space-y-2 hover:border-emerald-500/40">
                            <div className="flex items-center justify-between gap-4">
                                <h3 className="text-lg font-semibold">{appointment.petName}</h3>
                                <span className="rounded-full bg-slate-800 px-3 py-1 text-xs text-slate-300">{appointment.status}</span>
                            </div>
                            <p className="text-sm text-slate-300">{new Date(appointment.startAtUtc).toLocaleString()} → {new Date(appointment.endAtUtc).toLocaleString()}</p>
                            <p className="text-sm text-slate-400">{appointment.itemLabels.join(", ")}</p>
                        </Card>
                    </Link>
                ))}
                {appointments.length === 0 ? <Card><p className="text-sm text-slate-400">No appointments yet.</p></Card> : null}
            </div>
        </section>
    );
}

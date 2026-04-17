"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest } from "@/lib/api";
import type { ClientAppointmentDetail } from "@/lib/types";
import { Card } from "@/components/ui";

export default function ClientAppointmentDetailPage() {
    const params = useParams<{ appointmentId: string }>();
    const [appointment, setAppointment] = useState<ClientAppointmentDetail | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        apiRequest<ClientAppointmentDetail>(`/api/client/appointments/${params.appointmentId}`)
            .then(setAppointment)
            .catch((err: Error) => setError(err.message));
    }, [params.appointmentId]);

    if (error) {
        return <p className="text-sm text-rose-300">{error}</p>;
    }

    if (!appointment) {
        return <p className="text-sm text-slate-400">Loading appointment…</p>;
    }

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Appointment detail</p>
                <h2 className="mt-2 text-3xl font-semibold">{appointment.breedName}</h2>
                <p className="mt-2 text-sm text-slate-300">{new Date(appointment.startAtUtc).toLocaleString()} → {new Date(appointment.endAtUtc).toLocaleString()}</p>
            </div>
            <Card className="space-y-3">
                {appointment.items.map((item) => (
                    <div key={item.id} className="rounded-xl border border-slate-800 p-3">
                        <p className="font-medium">{item.offerDisplayName}</p>
                        <p className="text-sm text-slate-400">{item.itemType} • {item.priceAmount} UAH • {item.reservedMinutes} reserved min</p>
                    </div>
                ))}
                <p className="text-sm text-slate-300">Total: {appointment.totalAmount} UAH</p>
                <p className="text-sm text-slate-400">Status: {appointment.status}</p>
            </Card>
        </section>
    );
}

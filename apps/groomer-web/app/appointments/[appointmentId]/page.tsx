"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { GroomerShell } from "@/components/groomer-shell";
import { apiRequest, ApiError } from "@/lib/api";
import { createActionGuard, getAppointmentStatusDisplay, type AppointmentStatusTone } from "@/lib/workflow-helpers";

type AppointmentDetail = {
    id: string;
    pet: {
        id: string;
        displayName: string;
        animalTypeCode: string;
        animalTypeName: string;
        breedName: string;
        coatTypeCode: string | null;
        sizeCategoryCode: string | null;
    };
    startAt: string;
    endAt: string;
    status: string;
    reservedMinutes: number;
    handlingNotes: string[];
    items: Array<{
        appointmentItemId: string;
        itemType: string;
        offerId: string;
        offerVersionId: string;
        offerCode: string;
        offerDisplayName: string;
        quantity: number;
        serviceMinutes: number;
        reservedMinutes: number;
        executionPlanSummary: string[];
    }>;
};

type VisitDetail = {
    id: string;
    status: string;
};

const statusToneClasses: Record<AppointmentStatusTone, string> = {
    neutral: "border-slate-700 text-slate-200",
    active: "border-emerald-500/30 bg-emerald-500/10 text-emerald-200",
    complete: "border-sky-500/30 bg-sky-500/10 text-sky-200",
    blocked: "border-rose-500/30 bg-rose-500/10 text-rose-200"
};

export default function AppointmentDetailPage() {
    const params = useParams<{ appointmentId: string }>();
    const appointmentId = String(params.appointmentId);
    const checkInGuard = useRef(createActionGuard());
    const [appointment, setAppointment] = useState<AppointmentDetail | null>(null);
    const [visit, setVisit] = useState<VisitDetail | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isStarting, setIsStarting] = useState(false);

    const loadAppointment = useCallback(async (isCancelled: () => boolean = () => false) => {
        setIsLoading(true);
        setError(null);

        const results = await Promise.allSettled([
            apiRequest<AppointmentDetail>(`/api/groomer/appointments/${appointmentId}`),
            apiRequest<VisitDetail>(`/api/groomer/appointments/${appointmentId}/visit`)
        ]);

        if (isCancelled()) {
            return;
        }

        const appointmentResult = results[0];
        if (appointmentResult.status === "fulfilled") {
            setAppointment(appointmentResult.value);
        } else {
            const reason = appointmentResult.reason;
            setError(reason instanceof ApiError ? reason.message : "Failed to load appointment.");
        }

        const visitResult = results[1];
        if (visitResult.status === "fulfilled") {
            setVisit(visitResult.value);
        } else {
            setVisit(null);
        }

        setIsLoading(false);
    }, [appointmentId]);

    useEffect(() => {
        let cancelled = false;

        void loadAppointment(() => cancelled);

        return () => {
            cancelled = true;
        };
    }, [loadAppointment]);

    async function startVisit() {
        if (!checkInGuard.current.tryAcquire()) {
            return;
        }

        setIsStarting(true);
        setActionError(null);

        try {
            const response = await apiRequest<VisitDetail>(`/api/groomer/appointments/${appointmentId}/check-in`, {
                method: "POST",
                body: JSON.stringify({ appointmentId })
            });

            setVisit(response);
        } catch (err) {
            setActionError(err instanceof ApiError ? err.message : "Failed to start visit.");
        } finally {
            checkInGuard.current.release();
            setIsStarting(false);
        }
    }

    const statusDisplay = appointment ? getAppointmentStatusDisplay(appointment.status) : null;

    return (
        <GroomerShell>
            {isLoading ? <div className="rounded-3xl border border-slate-800 bg-slate-900/50 px-5 py-4 text-sm text-slate-300">Loading appointment…</div> : null}
            {!isLoading ? (
                <section className="flex flex-col gap-6">
                    <Link href="/appointments" className="text-sm text-emerald-300">← Back to appointments</Link>
                    {error ? (
                        <div className="flex flex-col gap-3 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200 sm:flex-row sm:items-center sm:justify-between">
                            <p>{error}</p>
                            <button type="button" onClick={() => void loadAppointment()} className="w-fit rounded-xl border border-rose-400/40 px-3 py-2 text-rose-100 hover:bg-rose-500/10">
                                Retry
                            </button>
                        </div>
                    ) : null}

                    {appointment ? (
                        <>
                            <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
                                <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                                    <div>
                                        <h1 className="text-3xl font-semibold">{appointment.pet.displayName}</h1>
                                        <p className="mt-2 text-sm text-slate-300">{appointment.pet.animalTypeName} • {appointment.pet.breedName}</p>
                                        <p className="mt-2 text-sm text-slate-400">{new Date(appointment.startAt).toLocaleString()} → {new Date(appointment.endAt).toLocaleTimeString()}</p>
                                    </div>
                                    <div className="flex flex-col items-start gap-3 md:items-end">
                                        {statusDisplay ? <span className={`rounded-full border px-3 py-1 text-xs ${statusToneClasses[statusDisplay.tone]}`}>{statusDisplay.label}</span> : null}
                                        {visit ? (
                                            <Link href={`/visits/${visit.id}`} className="rounded-2xl bg-emerald-500 px-4 py-2 text-sm font-medium text-slate-950">Open visit</Link>
                                        ) : (
                                            <button type="button" onClick={startVisit} disabled={isStarting} className="rounded-2xl bg-emerald-500 px-4 py-2 text-sm font-medium text-slate-950 disabled:cursor-not-allowed disabled:opacity-60">
                                                {isStarting ? "Starting…" : "Start visit"}
                                            </button>
                                        )}
                                    </div>
                                </div>
                                {actionError ? <p className="mt-4 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{actionError}</p> : null}
                                {appointment.handlingNotes.length > 0 ? (
                                    <div className="mt-5 rounded-2xl border border-amber-500/20 bg-amber-500/10 px-4 py-3 text-sm text-amber-100">
                                        <p className="font-medium">Handling notes</p>
                                        <ul className="mt-2 list-disc pl-5">
                                            {appointment.handlingNotes.map((note) => <li key={note}>{note}</li>)}
                                        </ul>
                                    </div>
                                ) : null}
                            </section>

                            <section className="grid gap-4">
                                {appointment.items.map((item) => (
                                    <article key={item.appointmentItemId} className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                        <div className="flex items-start justify-between gap-3">
                                            <div>
                                                <h2 className="text-lg font-medium">{item.offerDisplayName}</h2>
                                                <p className="text-sm text-slate-400">{item.itemType} • Reserved {item.reservedMinutes} min</p>
                                            </div>
                                            <span className="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">Qty {item.quantity}</span>
                                        </div>
                                        <div className="mt-4 flex flex-wrap gap-2">
                                            {item.executionPlanSummary.map((step) => (
                                                <span key={step} className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-200">{step}</span>
                                            ))}
                                        </div>
                                    </article>
                                ))}
                            </section>
                        </>
                    ) : null}
                </section>
            ) : null}
        </GroomerShell>
    );
}

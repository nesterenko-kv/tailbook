"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { GroomerShell } from "@/components/groomer-shell";
import { apiRequest, ApiError } from "@/lib/api";
import {
    createActionGuard,
    getAppointmentStatusDisplay,
    getSkipReasonLabel,
    getVisitChecklistSummary,
    SKIP_REASON_OPTIONS,
    type AppointmentStatusTone
} from "@/lib/workflow-helpers";

type VisitDetail = {
    id: string;
    appointmentId: string;
    pet: {
        id: string;
        name: string;
        animalTypeCode: string;
        animalTypeName: string;
        breedName: string;
        coatTypeCode: string | null;
        sizeCategoryCode: string | null;
    };
    status: string;
    checkedInAt: string;
    startedAt: string | null;
    completedAt: string | null;
    closedAt: string | null;
    serviceMinutes: number;
    reservedMinutes: number;
    items: VisitItem[];
};

type VisitItem = {
    id: string;
    appointmentItemId: string;
    itemType: string;
    offerId: string;
    offerVersionId: string;
    offerCode: string;
    offerDisplayName: string;
    quantity: number;
    serviceMinutes: number;
    reservedMinutes: number;
    expectedComponents: Array<{
        id: string;
        procedureId: string;
        procedureCode: string;
        procedureName: string;
        componentRole: string;
        sequenceNo: number;
        defaultExpected: boolean;
        isSkipped: boolean;
    }>;
    performedProcedures: Array<{
        id: string;
        procedureId: string;
        procedureCode: string;
        procedureName: string;
        status: string;
        note: string | null;
        recordedAt: string;
    }>;
    skippedComponents: Array<{
        id: string;
        offerVersionComponentId: string;
        procedureId: string;
        procedureCode: string;
        procedureName: string;
        omissionReasonCode: string;
        note: string | null;
        recordedAt: string;
    }>;
};

const statusToneClasses: Record<AppointmentStatusTone, string> = {
    neutral: "border-slate-700 text-slate-200",
    active: "border-emerald-500/30 bg-emerald-500/10 text-emerald-200",
    complete: "border-sky-500/30 bg-sky-500/10 text-sky-200",
    blocked: "border-rose-500/30 bg-rose-500/10 text-rose-200"
};

export default function VisitDetailPage() {
    const params = useParams<{ visitId: string }>();
    const visitId = String(params.visitId);
    const actionGuard = useRef(createActionGuard());
    const [visit, setVisit] = useState<VisitDetail | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [busyActionKey, setBusyActionKey] = useState<string | null>(null);
    const [noteByProcedure, setNoteByProcedure] = useState<Record<string, string>>({});
    const [skipReasonByComponent, setSkipReasonByComponent] = useState<Record<string, string>>({});

    const loadVisit = useCallback(async (isCancelled: () => boolean = () => false) => {
        setIsLoading(true);
        setError(null);

        try {
            const response = await apiRequest<VisitDetail>(`/api/groomer/visits/${visitId}`);
            if (isCancelled()) {
                return;
            }

            setVisit(response);
        } catch (err) {
            if (isCancelled()) {
                return;
            }

            setError(err instanceof ApiError ? err.message : "Failed to load visit.");
        } finally {
            if (!isCancelled()) {
                setIsLoading(false);
            }
        }
    }, [visitId]);

    useEffect(() => {
        let cancelled = false;

        void loadVisit(() => cancelled);

        return () => {
            cancelled = true;
        };
    }, [loadVisit]);

    const checklistSummary = useMemo(
        () => getVisitChecklistSummary(visit?.items ?? []),
        [visit]
    );

    async function markPerformed(executionItemId: string, procedureId: string, noteKey: string) {
        const actionKey = `performed:${executionItemId}:${procedureId}`;
        if (!actionGuard.current.tryAcquire()) {
            return;
        }

        setBusyActionKey(actionKey);
        setError(null);

        try {
            const response = await apiRequest<VisitDetail>(`/api/groomer/visits/${visitId}/performed-procedures`, {
                method: "POST",
                body: JSON.stringify({
                    visitId,
                    visitExecutionItemId: executionItemId,
                    procedureId,
                    note: noteByProcedure[noteKey] || null
                })
            });
            setVisit(response);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to record performed procedure.");
        } finally {
            actionGuard.current.release();
            setBusyActionKey(null);
        }
    }

    async function markSkipped(executionItemId: string, componentId: string, noteKey: string, reasonCode: string) {
        const actionKey = `skipped:${executionItemId}:${componentId}`;
        if (!actionGuard.current.tryAcquire()) {
            return;
        }

        setBusyActionKey(actionKey);
        setError(null);

        try {
            const response = await apiRequest<VisitDetail>(`/api/groomer/visits/${visitId}/skipped-components`, {
                method: "POST",
                body: JSON.stringify({
                    visitId,
                    visitExecutionItemId: executionItemId,
                    offerVersionComponentId: componentId,
                    omissionReasonCode: reasonCode,
                    note: noteByProcedure[noteKey] || null
                })
            });
            setVisit(response);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to record skipped component.");
        } finally {
            actionGuard.current.release();
            setBusyActionKey(null);
        }
    }

    const statusDisplay = visit ? getAppointmentStatusDisplay(visit.status) : null;

    return (
        <GroomerShell>
            <section className="flex flex-col gap-6">
                <Link href={visit ? `/appointments/${visit.appointmentId}` : "/appointments"} className="text-sm text-emerald-300">← Back</Link>
                {isLoading ? <p className="rounded-3xl border border-slate-800 bg-slate-900/50 px-5 py-4 text-sm text-slate-300">Loading visit…</p> : null}
                {error ? (
                    <div className="flex flex-col gap-3 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200 sm:flex-row sm:items-center sm:justify-between">
                        <p>{error}</p>
                        <button type="button" onClick={() => void loadVisit()} className="w-fit rounded-xl border border-rose-400/40 px-3 py-2 text-rose-100 hover:bg-rose-500/10">
                            Retry
                        </button>
                    </div>
                ) : null}
                {visit ? (
                    <>
                        <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
                            <div className="flex flex-wrap items-start justify-between gap-4">
                                <div>
                                    <h1 className="text-3xl font-semibold">Visit • {visit.pet.name}</h1>
                                    <p className="mt-2 text-sm text-slate-300">{visit.pet.animalTypeName} • {visit.pet.breedName}</p>
                                    <p className="mt-2 text-sm text-slate-400">Checked in {new Date(visit.checkedInAt).toLocaleString()}</p>
                                </div>
                                {statusDisplay ? <span className={`rounded-full border px-3 py-1 text-xs ${statusToneClasses[statusDisplay.tone]}`}>{statusDisplay.label}</span> : null}
                            </div>
                        </section>

                        <section className="grid gap-4 md:grid-cols-4">
                            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                <p className="text-sm text-slate-400">Expected</p>
                                <p className="mt-1 text-2xl font-semibold">{checklistSummary.expectedCount}</p>
                            </div>
                            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                <p className="text-sm text-slate-400">Performed</p>
                                <p className="mt-1 text-2xl font-semibold">{checklistSummary.performedCount}</p>
                            </div>
                            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                <p className="text-sm text-slate-400">Skipped</p>
                                <p className="mt-1 text-2xl font-semibold">{checklistSummary.skippedCount}</p>
                            </div>
                            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                <p className="text-sm text-slate-400">Remaining</p>
                                <p className="mt-1 text-2xl font-semibold">{checklistSummary.remainingCount}</p>
                            </div>
                        </section>

                        {checklistSummary.isReadyForAdminHandoff ? (
                            <section className="rounded-3xl border border-emerald-500/30 bg-emerald-500/10 p-5 text-sm text-emerald-100">
                                <h2 className="text-lg font-medium">Ready for admin review</h2>
                                <p className="mt-2 text-emerald-50/80">All expected work is accounted for. Admin can review completion, pricing, adjustments, and closeout.</p>
                            </section>
                        ) : null}

                        {visit.items.map((item) => (
                            <section key={item.id} className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <h2 className="text-xl font-medium">{item.offerDisplayName}</h2>
                                        <p className="mt-2 text-sm text-slate-400">Reserved {item.reservedMinutes} min • Service {item.serviceMinutes} min</p>
                                    </div>
                                    <span className="rounded-full border border-emerald-500/20 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">{item.itemType}</span>
                                </div>

                                <div className="mt-6 grid gap-4">
                                    {item.expectedComponents.map((component) => {
                                        const noteKey = `${item.id}:${component.id}`;
                                        const skipReason = skipReasonByComponent[noteKey] ?? SKIP_REASON_OPTIONS[0].code;
                                        const performedActionKey = `performed:${item.id}:${component.procedureId}`;
                                        const skippedActionKey = `skipped:${item.id}:${component.id}`;
                                        const performed = item.performedProcedures.some((x) => x.procedureId === component.procedureId);
                                        const skipped = component.isSkipped || item.skippedComponents.some((x) => x.offerVersionComponentId === component.id);
                                        return (
                                            <article key={component.id} className="rounded-2xl border border-slate-800 bg-slate-950/70 p-4">
                                                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                                                    <div>
                                                        <p className="font-medium">{component.procedureName}</p>
                                                        <p className="text-sm text-slate-400">{component.procedureCode} • #{component.sequenceNo}</p>
                                                    </div>
                                                    <div className="flex flex-wrap gap-2">
                                                        <button type="button" disabled={performed || busyActionKey !== null} onClick={() => markPerformed(item.id, component.procedureId, noteKey)} className="rounded-2xl bg-emerald-500 px-3 py-2 text-sm font-medium text-slate-950 disabled:cursor-not-allowed disabled:opacity-50">
                                                            {busyActionKey === performedActionKey ? "Saving…" : "Performed"}
                                                        </button>
                                                        <button type="button" disabled={skipped || busyActionKey !== null} onClick={() => markSkipped(item.id, component.id, noteKey, skipReason)} className="rounded-2xl border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-100 disabled:cursor-not-allowed disabled:opacity-50">
                                                            {busyActionKey === skippedActionKey ? "Saving…" : "Skipped"}
                                                        </button>
                                                    </div>
                                                </div>
                                                <label className="mt-3 block text-xs font-medium uppercase text-slate-500" htmlFor={`skip-reason-${noteKey}`}>
                                                    Skip reason
                                                </label>
                                                <select
                                                    id={`skip-reason-${noteKey}`}
                                                    value={skipReason}
                                                    onChange={(event) => setSkipReasonByComponent((current) => ({ ...current, [noteKey]: event.target.value }))}
                                                    disabled={skipped || busyActionKey !== null}
                                                    className="mt-2 w-full rounded-2xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm text-slate-100 outline-none focus:border-emerald-500 disabled:opacity-60"
                                                >
                                                    {SKIP_REASON_OPTIONS.map((option) => (
                                                        <option key={option.code} value={option.code}>{option.label}</option>
                                                    ))}
                                                </select>
                                                <textarea value={noteByProcedure[noteKey] ?? ""} onChange={(event) => setNoteByProcedure((current) => ({ ...current, [noteKey]: event.target.value }))} placeholder="Optional note" className="mt-3 min-h-20 w-full rounded-2xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm outline-none focus:border-emerald-500" />
                                            </article>
                                        );
                                    })}
                                </div>
                            </section>
                        ))}

                        {visit.items.length > 0 ? (
                            <section className="grid gap-4 md:grid-cols-2">
                                <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                    <h3 className="text-lg font-medium">Performed</h3>
                                    <ul className="mt-3 space-y-2 text-sm text-slate-300">
                                        {visit.items.flatMap((item) => item.performedProcedures).map((entry) => (
                                            <li key={entry.id}>{entry.procedureName}</li>
                                        ))}
                                        {visit.items.every((item) => item.performedProcedures.length === 0) ? <li className="text-slate-500">Nothing recorded yet.</li> : null}
                                    </ul>
                                </div>
                                <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-5">
                                    <h3 className="text-lg font-medium">Skipped</h3>
                                    <ul className="mt-3 space-y-2 text-sm text-slate-300">
                                        {visit.items.flatMap((item) => item.skippedComponents).map((entry) => (
                                            <li key={entry.id}>{entry.procedureName} — {getSkipReasonLabel(entry.omissionReasonCode)}</li>
                                        ))}
                                        {visit.items.every((item) => item.skippedComponents.length === 0) ? <li className="text-slate-500">Nothing skipped yet.</li> : null}
                                    </ul>
                                </div>
                            </section>
                        ) : null}
                    </>
                ) : null}
            </section>
        </GroomerShell>
    );
}

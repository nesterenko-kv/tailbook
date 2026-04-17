"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";

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
    checkedInAtUtc: string;
    startedAtUtc: string | null;
    completedAtUtc: string | null;
    closedAtUtc: string | null;
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
        recordedAtUtc: string;
    }>;
    skippedComponents: Array<{
        id: string;
        offerVersionComponentId: string;
        procedureId: string;
        procedureCode: string;
        procedureName: string;
        omissionReasonCode: string;
        note: string | null;
        recordedAtUtc: string;
    }>;
};

export default function VisitDetailPage() {
    const params = useParams<{ visitId: string }>();
    const visitId = String(params.visitId);
    const [visit, setVisit] = useState<VisitDetail | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [noteByProcedure, setNoteByProcedure] = useState<Record<string, string>>({});

    async function loadVisit() {
        try {
            const response = await apiRequest<VisitDetail>(`/api/groomer/visits/${visitId}`);
            setVisit(response);
            setError(null);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load visit.");
        }
    }

    useEffect(() => {
        void loadVisit();
    }, [visitId]);

    const firstExecutionItem = useMemo(() => visit?.items[0] ?? null, [visit]);

    async function markPerformed(executionItemId: string, procedureId: string, noteKey: string) {
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
        }
    }

    async function markSkipped(executionItemId: string, componentId: string, noteKey: string) {
        try {
            const response = await apiRequest<VisitDetail>(`/api/groomer/visits/${visitId}/skipped-components`, {
                method: "POST",
                body: JSON.stringify({
                    visitId,
                    visitExecutionItemId: executionItemId,
                    offerVersionComponentId: componentId,
                    omissionReasonCode: "OPERATIONAL_DECISION",
                    note: noteByProcedure[noteKey] || null
                })
            });
            setVisit(response);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to record skipped component.");
        }
    }

    return (
        <main className="mx-auto flex max-w-6xl flex-col gap-6 px-6 py-10">
            <Link href={visit ? `/appointments/${visit.appointmentId}` : "/appointments"} className="text-sm text-emerald-300">← Back</Link>
            {error ? <p className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{error}</p> : null}
            {visit ? (
                <>
                    <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
                        <div className="flex flex-wrap items-start justify-between gap-4">
                            <div>
                                <h1 className="text-3xl font-semibold">Visit • {visit.pet.name}</h1>
                                <p className="mt-2 text-sm text-slate-300">{visit.pet.animalTypeName} • {visit.pet.breedName}</p>
                                <p className="mt-2 text-sm text-slate-400">Checked in {new Date(visit.checkedInAtUtc).toLocaleString()}</p>
                            </div>
                            <span className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-200">{visit.status}</span>
                        </div>
                    </section>

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
                                    const performed = item.performedProcedures.some((x) => x.procedureId === component.procedureId);
                                    return (
                                        <article key={component.id} className="rounded-2xl border border-slate-800 bg-slate-950/70 p-4">
                                            <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                                                <div>
                                                    <p className="font-medium">{component.procedureName}</p>
                                                    <p className="text-sm text-slate-400">{component.procedureCode} • #{component.sequenceNo}</p>
                                                </div>
                                                <div className="flex flex-wrap gap-2">
                                                    <button disabled={performed} onClick={() => markPerformed(item.id, component.procedureId, noteKey)} className="rounded-2xl bg-emerald-500 px-3 py-2 text-sm font-medium text-slate-950 disabled:opacity-50">Performed</button>
                                                    <button disabled={component.isSkipped} onClick={() => markSkipped(item.id, component.id, noteKey)} className="rounded-2xl border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-100 disabled:opacity-50">Skipped</button>
                                                </div>
                                            </div>
                                            <textarea value={noteByProcedure[noteKey] ?? ""} onChange={(event) => setNoteByProcedure((current) => ({ ...current, [noteKey]: event.target.value }))} placeholder="Optional note" className="mt-3 min-h-20 w-full rounded-2xl border border-slate-700 bg-slate-900 px-4 py-3 text-sm outline-none focus:border-emerald-500" />
                                        </article>
                                    );
                                })}
                            </div>
                        </section>
                    ))}

                    {firstExecutionItem ? (
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
                                        <li key={entry.id}>{entry.procedureName} — {entry.omissionReasonCode}</li>
                                    ))}
                                    {visit.items.every((item) => item.skippedComponents.length === 0) ? <li className="text-slate-500">Nothing skipped yet.</li> : null}
                                </ul>
                            </div>
                        </section>
                    ) : null}
                </>
            ) : null}
        </main>
    );
}

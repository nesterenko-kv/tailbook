"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import type { Offer, ProcedureItem } from "@/lib/types";
import { Badge, Card, ErrorBanner, LinkButton, Field, Input, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

export default function OfferDetailPage() {
    const params = useParams<{ offerId: string }>();
    const offerId = String(params.offerId);
    const [offer, setOffer] = useState<Offer | null>(null);
    const [procedures, setProcedures] = useState<ProcedureItem[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [versionForm, setVersionForm] = useState({ validFromUtc: "", validToUtc: "", policyText: "", changeNote: "" });
    const [componentForm, setComponentForm] = useState({ versionId: "", procedureId: "", componentRole: "Included", sequenceNo: "1", defaultExpected: true });

    async function load() {
        try {
            const [offerResponse, procedureResponse] = await Promise.all([
                apiRequest<Offer>(`/api/admin/catalog/offers/${offerId}`),
                apiRequest<ProcedureItem[]>("/api/admin/catalog/procedures")
            ]);
            setOffer(offerResponse);
            setProcedures(procedureResponse);
            const latestVersionId = offerResponse.versions[0]?.id ?? "";
            const firstProcedureId = procedureResponse[0]?.id ?? "";
            setComponentForm((current) => ({
                ...current,
                versionId: current.versionId || latestVersionId,
                procedureId: current.procedureId || firstProcedureId
            }));
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load offer.");
        }
    }

    useEffect(() => {
        void load();
    }, [offerId]);

    async function createVersion(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/catalog/offers/${offerId}/versions`, {
                method: "POST",
                body: JSON.stringify({
                    offerId,
                    validFromUtc: versionForm.validFromUtc ? new Date(versionForm.validFromUtc).toISOString() : null,
                    validToUtc: versionForm.validToUtc ? new Date(versionForm.validToUtc).toISOString() : null,
                    policyText: versionForm.policyText || null,
                    changeNote: versionForm.changeNote || null
                })
            });
            setVersionForm({ validFromUtc: "", validToUtc: "", policyText: "", changeNote: "" });
            setSuccess("Offer version created.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create version.");
        }
    }

    async function addComponent(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/catalog/offer-versions/${componentForm.versionId}/components`, {
                method: "POST",
                body: JSON.stringify({
                    versionId: componentForm.versionId,
                    procedureId: componentForm.procedureId,
                    componentRole: componentForm.componentRole,
                    sequenceNo: Number(componentForm.sequenceNo),
                    defaultExpected: componentForm.defaultExpected
                })
            });
            setSuccess("Component added.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to add component.");
        }
    }

    async function publishVersion(versionId: string) {
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/catalog/offer-versions/${versionId}/publish`, {
                method: "POST",
                body: JSON.stringify({ versionId })
            });
            setSuccess("Offer version published.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to publish version.");
        }
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader eyebrow="Catalog detail" title={offer?.displayName ?? "Offer detail"} description="Version package composition and publish immutable offer versions." action={<LinkButton href="/catalog/offers">Back to offers</LinkButton>} />
            <ErrorBanner message={error} />
            <SuccessBanner message={success} />
            {offer ? (
                <>
                    <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
                        <Card title="Offer summary">
                            <div className="grid gap-2 text-sm text-slate-300">
                                <div className="flex items-center gap-2"><Badge>{offer.offerType}</Badge>{offer.isActive ? <Badge tone="success">Active</Badge> : <Badge>Inactive</Badge>}</div>
                                <p>Code: {offer.code}</p>
                                <p>Version count: {offer.versions.length}</p>
                            </div>
                        </Card>
                        <Card title="Create version">
                            <form className="space-y-4" onSubmit={createVersion}>
                                <Field label="Valid from"><Input type="datetime-local" value={versionForm.validFromUtc} onChange={(event) => setVersionForm((current) => ({ ...current, validFromUtc: event.target.value }))} /></Field>
                                <Field label="Valid to"><Input type="datetime-local" value={versionForm.validToUtc} onChange={(event) => setVersionForm((current) => ({ ...current, validToUtc: event.target.value }))} /></Field>
                                <Field label="Policy text"><TextArea value={versionForm.policyText} onChange={(event) => setVersionForm((current) => ({ ...current, policyText: event.target.value }))} /></Field>
                                <Field label="Change note"><Input value={versionForm.changeNote} onChange={(event) => setVersionForm((current) => ({ ...current, changeNote: event.target.value }))} /></Field>
                                <PrimaryButton type="submit" className="w-full">Create version</PrimaryButton>
                            </form>
                        </Card>
                    </div>

                    <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
                        <Card title="Versions and components">
                            <div className="grid gap-4">
                                {offer.versions.map((version) => (
                                    <article key={version.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                        <div className="flex items-start justify-between gap-4">
                                            <div>
                                                <h3 className="font-medium">Version {version.versionNo}</h3>
                                                <p className="text-sm text-slate-400">{version.status}</p>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <Badge tone={version.status === "Published" ? "success" : "default"}>{version.status}</Badge>
                                                {version.status !== "Published" ? <PrimaryButton type="button" onClick={() => void publishVersion(version.id)}>Publish</PrimaryButton> : null}
                                            </div>
                                        </div>
                                        {version.policyText ? <p className="mt-3 text-sm text-slate-300">{version.policyText}</p> : null}
                                        <div className="mt-4 grid gap-2">
                                            {version.components.map((component) => (
                                                <div key={component.id} className="rounded-2xl border border-slate-800 px-3 py-3 text-sm text-slate-300">
                                                    #{component.sequenceNo} • {component.procedureName} • {component.componentRole}
                                                </div>
                                            ))}
                                            {version.components.length === 0 ? <p className="text-sm text-slate-400">No components yet.</p> : null}
                                        </div>
                                    </article>
                                ))}
                            </div>
                        </Card>

                        <Card title="Add version component">
                            <form className="space-y-4" onSubmit={addComponent}>
                                <Field label="Version"><Select value={componentForm.versionId} onChange={(event) => setComponentForm((current) => ({ ...current, versionId: event.target.value }))}>{offer.versions.map((version) => <option key={version.id} value={version.id}>Version {version.versionNo} ({version.status})</option>)}</Select></Field>
                                <Field label="Procedure"><Select value={componentForm.procedureId} onChange={(event) => setComponentForm((current) => ({ ...current, procedureId: event.target.value }))}>{procedures.map((procedure) => <option key={procedure.id} value={procedure.id}>{procedure.name}</option>)}</Select></Field>
                                <Field label="Role"><Select value={componentForm.componentRole} onChange={(event) => setComponentForm((current) => ({ ...current, componentRole: event.target.value }))}><option value="Included">Included</option><option value="OptionalOperational">OptionalOperational</option><option value="Recommended">Recommended</option></Select></Field>
                                <Field label="Sequence"><Input type="number" min="1" value={componentForm.sequenceNo} onChange={(event) => setComponentForm((current) => ({ ...current, sequenceNo: event.target.value }))} /></Field>
                                <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={componentForm.defaultExpected} onChange={(event) => setComponentForm((current) => ({ ...current, defaultExpected: event.target.checked }))} /> Default expected</label>
                                <PrimaryButton type="submit" className="w-full">Add component</PrimaryButton>
                            </form>
                        </Card>
                    </div>
                </>
            ) : null}
        </div>
    );
}

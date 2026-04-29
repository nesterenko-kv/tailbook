"use client";

import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime, formatMoney } from "@/lib/format";
import { unwrapItems } from "@/lib/contracts";
import type {
    ClientDetail,
    ClientListItem,
    DurationRuleSet,
    DurationRuleSetListResponse,
    OfferListItem,
    PagedResult,
    PriceRuleSet,
    PriceRuleSetListResponse,
    QuotePreview,
    GroomerListItem,
    GroomerListResponse
} from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, Select, SuccessBanner } from "@/components/ui";

export default function PricingPage() {
    const [offers, setOffers] = useState<OfferListItem[]>([]);
    const [clients, setClients] = useState<ClientListItem[]>([]);
    const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
    const [selectedClient, setSelectedClient] = useState<ClientDetail | null>(null);
    const [priceRuleSets, setPriceRuleSets] = useState<PriceRuleSet[]>([]);
    const [durationRuleSets, setDurationRuleSets] = useState<DurationRuleSet[]>([]);
    const [quotePreview, setQuotePreview] = useState<QuotePreview | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [priceSetForm, setPriceSetForm] = useState({ validFromUtc: "", validToUtc: "" });
    const [durationSetForm, setDurationSetForm] = useState({ validFromUtc: "", validToUtc: "" });
    const [priceRuleForm, setPriceRuleForm] = useState({ ruleSetId: "", offerId: "", priority: "100", fixedAmount: "1400", currency: "UAH", animalTypeId: "", breedId: "", breedGroupId: "", coatTypeId: "", sizeCategoryId: "" });
    const [durationRuleForm, setDurationRuleForm] = useState({ ruleSetId: "", offerId: "", priority: "100", baseMinutes: "120", bufferBeforeMinutes: "10", bufferAfterMinutes: "20", animalTypeId: "", breedId: "", breedGroupId: "", coatTypeId: "", sizeCategoryId: "" });
    const [quoteForm, setQuoteForm] = useState({ clientId: "", petId: "", groomerId: "", offerIds: [] as string[] });

    async function loadAll() {
        try {
            const [offerResponse, clientResponse, groomerResponse, priceResponse, durationResponse] = await Promise.all([
                apiRequest<OfferListItem[]>("/api/admin/catalog/offers"),
                apiRequest<PagedResult<ClientListItem>>("/api/admin/clients?page=1&pageSize=100"),
                apiRequest<GroomerListResponse>("/api/admin/groomers"),
                apiRequest<PriceRuleSetListResponse>("/api/admin/pricing/rule-sets"),
                apiRequest<DurationRuleSetListResponse>("/api/admin/duration/rule-sets")
            ]);
    
            const groomerItems = unwrapItems(groomerResponse);
            const priceRuleSetItems = unwrapItems(priceResponse);
            const durationRuleSetItems = unwrapItems(durationResponse);
    
            setOffers(offerResponse);
            setClients(clientResponse.items);
            setGroomers(groomerItems);
            setPriceRuleSets(priceRuleSetItems);
            setDurationRuleSets(durationRuleSetItems);
    
            setPriceRuleForm((current) => ({
                ...current,
                ruleSetId: current.ruleSetId || priceRuleSetItems[0]?.id || "",
                offerId: current.offerId || offerResponse[0]?.id || ""
            }));
    
            setDurationRuleForm((current) => ({
                ...current,
                ruleSetId: current.ruleSetId || durationRuleSetItems[0]?.id || "",
                offerId: current.offerId || offerResponse[0]?.id || ""
            }));
    
            setQuoteForm((current) => ({
                ...current,
                clientId: current.clientId || clientResponse.items[0]?.id || "",
                groomerId: current.groomerId || groomerItems[0]?.id || "",
                offerIds: current.offerIds.length > 0 ? current.offerIds : offerResponse[0] ? [offerResponse[0].id] : []
            }));
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load pricing data.");
        }
    }

    useEffect(() => {
        void loadAll();
    }, []);

    useEffect(() => {
        if (!quoteForm.clientId) {
            setSelectedClient(null);
            return;
        }

        void apiRequest<ClientDetail>(`/api/admin/clients/${quoteForm.clientId}`)
            .then((response) => {
                setSelectedClient(response);
                if (response.pets[0] && !response.pets.some((pet) => pet.id === quoteForm.petId)) {
                    setQuoteForm((current) => ({ ...current, petId: response.pets[0]?.id ?? "" }));
                }
            })
            .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load client pets for quote preview."));
    }, [quoteForm.clientId]);

    async function createPriceRuleSet(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        try {
            await apiRequest("/api/admin/pricing/rule-sets", { method: "POST", body: JSON.stringify({ validFromUtc: priceSetForm.validFromUtc ? new Date(priceSetForm.validFromUtc).toISOString() : null, validToUtc: priceSetForm.validToUtc ? new Date(priceSetForm.validToUtc).toISOString() : null }) });
            setSuccess("Price rule set created.");
            setPriceSetForm({ validFromUtc: "", validToUtc: "" });
            await loadAll();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create price rule set.");
        }
    }

    async function createDurationRuleSet(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        try {
            await apiRequest("/api/admin/duration/rule-sets", { method: "POST", body: JSON.stringify({ validFromUtc: durationSetForm.validFromUtc ? new Date(durationSetForm.validFromUtc).toISOString() : null, validToUtc: durationSetForm.validToUtc ? new Date(durationSetForm.validToUtc).toISOString() : null }) });
            setSuccess("Duration rule set created.");
            setDurationSetForm({ validFromUtc: "", validToUtc: "" });
            await loadAll();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create duration rule set.");
        }
    }

    async function createPriceRule(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        try {
            await apiRequest(`/api/admin/pricing/rule-sets/${priceRuleForm.ruleSetId}/rules`, {
                method: "POST",
                body: JSON.stringify({
                    ruleSetId: priceRuleForm.ruleSetId,
                    offerId: priceRuleForm.offerId,
                    priority: Number(priceRuleForm.priority),
                    fixedAmount: Number(priceRuleForm.fixedAmount),
                    currency: priceRuleForm.currency,
                    animalTypeId: priceRuleForm.animalTypeId || null,
                    breedId: priceRuleForm.breedId || null,
                    breedGroupId: priceRuleForm.breedGroupId || null,
                    coatTypeId: priceRuleForm.coatTypeId || null,
                    sizeCategoryId: priceRuleForm.sizeCategoryId || null
                })
            });
            setSuccess("Price rule created.");
            await loadAll();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create price rule.");
        }
    }

    async function createDurationRule(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        try {
            await apiRequest(`/api/admin/duration/rule-sets/${durationRuleForm.ruleSetId}/rules`, {
                method: "POST",
                body: JSON.stringify({
                    ruleSetId: durationRuleForm.ruleSetId,
                    offerId: durationRuleForm.offerId,
                    priority: Number(durationRuleForm.priority),
                    baseMinutes: Number(durationRuleForm.baseMinutes),
                    bufferBeforeMinutes: Number(durationRuleForm.bufferBeforeMinutes),
                    bufferAfterMinutes: Number(durationRuleForm.bufferAfterMinutes),
                    animalTypeId: durationRuleForm.animalTypeId || null,
                    breedId: durationRuleForm.breedId || null,
                    breedGroupId: durationRuleForm.breedGroupId || null,
                    coatTypeId: durationRuleForm.coatTypeId || null,
                    sizeCategoryId: durationRuleForm.sizeCategoryId || null
                })
            });
            setSuccess("Duration rule created.");
            await loadAll();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create duration rule.");
        }
    }

    async function publishRuleSet(kind: "price" | "duration", ruleSetId: string) {
        setError(null);
        try {
            await apiRequest(kind === "price" ? `/api/admin/pricing/rule-sets/${ruleSetId}/publish` : `/api/admin/duration/rule-sets/${ruleSetId}/publish`, {
                method: "POST",
                body: JSON.stringify({ ruleSetId })
            });
            setSuccess(`${kind === "price" ? "Price" : "Duration"} rule set published.`);
            await loadAll();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to publish rule set.");
        }
    }

    async function previewQuote(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        try {
            const response = await apiRequest<QuotePreview>("/api/admin/quotes/preview", {
                method: "POST",
                body: JSON.stringify({
                    petId: quoteForm.petId,
                    groomerId: quoteForm.groomerId || null,
                    items: quoteForm.offerIds.map((offerId) => ({ offerId }))
                })
            });
            setQuotePreview(response);
            setSuccess("Quote preview generated.");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to preview quote.");
        }
    }

    function toggleQuoteOffer(offerId: string) {
        setQuoteForm((current) => ({
            ...current,
            offerIds: current.offerIds.includes(offerId)
                ? current.offerIds.filter((item) => item !== offerId)
                : [...current.offerIds, offerId]
        }));
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader eyebrow="Pricing and duration" title="Pricing" description="Maintain rule-table pricing, duration rule sets, and quote previews with frozen snapshots." />
            <ErrorBanner message={error} />
            <SuccessBanner message={success} />

            <div className="grid gap-6 xl:grid-cols-2">
                <Card title="Price rule sets">
                    <form className="grid gap-4 md:grid-cols-2" onSubmit={createPriceRuleSet}>
                        <Field label="Valid from"><Input type="datetime-local" value={priceSetForm.validFromUtc} onChange={(event) => setPriceSetForm((current) => ({ ...current, validFromUtc: event.target.value }))} /></Field>
                        <Field label="Valid to"><Input type="datetime-local" value={priceSetForm.validToUtc} onChange={(event) => setPriceSetForm((current) => ({ ...current, validToUtc: event.target.value }))} /></Field>
                        <PrimaryButton type="submit" className="md:col-span-2">Create price rule set</PrimaryButton>
                    </form>
                    <div className="mt-4 grid gap-3">
                        {priceRuleSets.map((item) => (
                            <article key={item.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <div className="flex items-start justify-between gap-3">
                                    <div>
                                        <h3 className="font-medium">Version {item.versionNo}</h3>
                                        <p className="text-sm text-slate-400">{item.rules.length} rules • {formatDateTime(item.validFromUtc)}</p>
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Badge tone={item.status === "Published" ? "success" : "default"}>{item.status}</Badge>
                                        {item.status !== "Published" ? <PrimaryButton type="button" onClick={() => void publishRuleSet("price", item.id)}>Publish</PrimaryButton> : null}
                                    </div>
                                </div>
                            </article>
                        ))}
                    </div>
                </Card>

                <Card title="Duration rule sets">
                    <form className="grid gap-4 md:grid-cols-2" onSubmit={createDurationRuleSet}>
                        <Field label="Valid from"><Input type="datetime-local" value={durationSetForm.validFromUtc} onChange={(event) => setDurationSetForm((current) => ({ ...current, validFromUtc: event.target.value }))} /></Field>
                        <Field label="Valid to"><Input type="datetime-local" value={durationSetForm.validToUtc} onChange={(event) => setDurationSetForm((current) => ({ ...current, validToUtc: event.target.value }))} /></Field>
                        <PrimaryButton type="submit" className="md:col-span-2">Create duration rule set</PrimaryButton>
                    </form>
                    <div className="mt-4 grid gap-3">
                        {durationRuleSets.map((item) => (
                            <article key={item.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <div className="flex items-start justify-between gap-3">
                                    <div>
                                        <h3 className="font-medium">Version {item.versionNo}</h3>
                                        <p className="text-sm text-slate-400">{item.rules.length} rules • {formatDateTime(item.validFromUtc)}</p>
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Badge tone={item.status === "Published" ? "success" : "default"}>{item.status}</Badge>
                                        {item.status !== "Published" ? <PrimaryButton type="button" onClick={() => void publishRuleSet("duration", item.id)}>Publish</PrimaryButton> : null}
                                    </div>
                                </div>
                            </article>
                        ))}
                    </div>
                </Card>
            </div>

            <div className="grid gap-6 xl:grid-cols-2">
                <Card title="Create price rule">
                    <form className="grid gap-4 md:grid-cols-2" onSubmit={createPriceRule}>
                        <Field label="Rule set"><Select value={priceRuleForm.ruleSetId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, ruleSetId: event.target.value }))}>{priceRuleSets.map((item) => <option key={item.id} value={item.id}>Version {item.versionNo}</option>)}</Select></Field>
                        <Field label="Offer"><Select value={priceRuleForm.offerId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, offerId: event.target.value }))}>{offers.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</Select></Field>
                        <Field label="Priority"><Input type="number" value={priceRuleForm.priority} onChange={(event) => setPriceRuleForm((current) => ({ ...current, priority: event.target.value }))} /></Field>
                        <Field label="Fixed amount"><Input type="number" step="0.01" value={priceRuleForm.fixedAmount} onChange={(event) => setPriceRuleForm((current) => ({ ...current, fixedAmount: event.target.value }))} /></Field>
                        <Field label="Currency"><Input value={priceRuleForm.currency} onChange={(event) => setPriceRuleForm((current) => ({ ...current, currency: event.target.value }))} /></Field>
                        <Field label="Breed id"><Input value={priceRuleForm.breedId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, breedId: event.target.value }))} /></Field>
                        <Field label="Animal type id"><Input value={priceRuleForm.animalTypeId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, animalTypeId: event.target.value }))} /></Field>
                        <Field label="Breed group id"><Input value={priceRuleForm.breedGroupId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, breedGroupId: event.target.value }))} /></Field>
                        <Field label="Coat type id"><Input value={priceRuleForm.coatTypeId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, coatTypeId: event.target.value }))} /></Field>
                        <Field label="Size category id"><Input value={priceRuleForm.sizeCategoryId} onChange={(event) => setPriceRuleForm((current) => ({ ...current, sizeCategoryId: event.target.value }))} /></Field>
                        <PrimaryButton type="submit" className="md:col-span-2">Create price rule</PrimaryButton>
                    </form>
                </Card>

                <Card title="Create duration rule">
                    <form className="grid gap-4 md:grid-cols-2" onSubmit={createDurationRule}>
                        <Field label="Rule set"><Select value={durationRuleForm.ruleSetId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, ruleSetId: event.target.value }))}>{durationRuleSets.map((item) => <option key={item.id} value={item.id}>Version {item.versionNo}</option>)}</Select></Field>
                        <Field label="Offer"><Select value={durationRuleForm.offerId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, offerId: event.target.value }))}>{offers.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</Select></Field>
                        <Field label="Priority"><Input type="number" value={durationRuleForm.priority} onChange={(event) => setDurationRuleForm((current) => ({ ...current, priority: event.target.value }))} /></Field>
                        <Field label="Base minutes"><Input type="number" value={durationRuleForm.baseMinutes} onChange={(event) => setDurationRuleForm((current) => ({ ...current, baseMinutes: event.target.value }))} /></Field>
                        <Field label="Buffer before"><Input type="number" value={durationRuleForm.bufferBeforeMinutes} onChange={(event) => setDurationRuleForm((current) => ({ ...current, bufferBeforeMinutes: event.target.value }))} /></Field>
                        <Field label="Buffer after"><Input type="number" value={durationRuleForm.bufferAfterMinutes} onChange={(event) => setDurationRuleForm((current) => ({ ...current, bufferAfterMinutes: event.target.value }))} /></Field>
                        <Field label="Breed id"><Input value={durationRuleForm.breedId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, breedId: event.target.value }))} /></Field>
                        <Field label="Animal type id"><Input value={durationRuleForm.animalTypeId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, animalTypeId: event.target.value }))} /></Field>
                        <Field label="Breed group id"><Input value={durationRuleForm.breedGroupId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, breedGroupId: event.target.value }))} /></Field>
                        <Field label="Coat type id"><Input value={durationRuleForm.coatTypeId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, coatTypeId: event.target.value }))} /></Field>
                        <Field label="Size category id"><Input value={durationRuleForm.sizeCategoryId} onChange={(event) => setDurationRuleForm((current) => ({ ...current, sizeCategoryId: event.target.value }))} /></Field>
                        <PrimaryButton type="submit" className="md:col-span-2">Create duration rule</PrimaryButton>
                    </form>
                </Card>
            </div>

            <Card title="Quote preview" description="Preview price and duration snapshots exactly through the admin API.">
                <form className="grid gap-4 md:grid-cols-2 xl:grid-cols-4" onSubmit={previewQuote}>
                    <Field label="Client"><Select value={quoteForm.clientId} onChange={(event) => setQuoteForm((current) => ({ ...current, clientId: event.target.value }))}>{clients.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</Select></Field>
                    <Field label="Pet"><Select value={quoteForm.petId} onChange={(event) => setQuoteForm((current) => ({ ...current, petId: event.target.value }))}>{selectedClient?.pets.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</Select></Field>
                    <Field label="Groomer"><Select value={quoteForm.groomerId} onChange={(event) => setQuoteForm((current) => ({ ...current, groomerId: event.target.value }))}><option value="">No groomer</option>{groomers.map((item) => <option key={item.id} value={item.id}>{item.displayName}</option>)}</Select></Field>
                    <div className="xl:col-span-4 grid gap-2 text-sm text-slate-300">
                        <span>Offers</span>
                        <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3">
                            {offers.map((offer) => (
                                <label key={offer.id} className="flex items-center gap-2 rounded-2xl border border-slate-800 bg-slate-950/60 px-3 py-3">
                                    <input type="checkbox" checked={quoteForm.offerIds.includes(offer.id)} onChange={() => toggleQuoteOffer(offer.id)} />
                                    <span>{offer.displayName}</span>
                                </label>
                            ))}
                        </div>
                    </div>
                    <PrimaryButton type="submit">Preview quote</PrimaryButton>
                </form>

                {quotePreview ? (
                    <div className="mt-6 grid gap-6 xl:grid-cols-2">
                        <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                            <h3 className="font-medium">Price snapshot</h3>
                            <p className="mt-2 text-sm text-slate-300">Total: {formatMoney(quotePreview.priceSnapshot.totalAmount, quotePreview.priceSnapshot.currency)}</p>
                            <div className="mt-4 grid gap-2 text-sm text-slate-300">
                                {quotePreview.priceSnapshot.lines.map((line) => <div key={`${line.label}-${line.sequenceNo}`} className="flex items-center justify-between gap-4"><span>{line.label}</span><span>{formatMoney(line.amount, quotePreview.priceSnapshot.currency)}</span></div>)}
                            </div>
                        </div>
                        <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                            <h3 className="font-medium">Duration snapshot</h3>
                            <p className="mt-2 text-sm text-slate-300">Service: {quotePreview.durationSnapshot.serviceMinutes} min • Reserved: {quotePreview.durationSnapshot.reservedMinutes} min</p>
                            <div className="mt-4 grid gap-2 text-sm text-slate-300">
                                {quotePreview.durationSnapshot.lines.map((line) => <div key={`${line.label}-${line.sequenceNo}`} className="flex items-center justify-between gap-4"><span>{line.label}</span><span>{line.minutes} min</span></div>)}
                            </div>
                        </div>
                    </div>
                ) : null}
            </Card>
        </div>
    );
}

"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import type { OfferListItem } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, Select, SuccessBanner } from "@/components/ui";

export default function OffersPage() {
    const [offers, setOffers] = useState<OfferListItem[]>([]);
    const [form, setForm] = useState({ code: "", offerType: "Package", displayName: "" });
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    async function load() {
        try {
            setOffers(await apiRequest<OfferListItem[]>("/api/admin/catalog/offers"));
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load offers.");
        }
    }

    useEffect(() => {
        void load();
    }, []);

    async function createOffer(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest("/api/admin/catalog/offers", {
                method: "POST",
                body: JSON.stringify(form)
            });
            setForm({ code: "", offerType: "Package", displayName: "" });
            setSuccess("Offer created.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create offer.");
        }
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader eyebrow="Catalog" title="Offers" description="Packages, standalone services, and add-ons stay as commercial offers with immutable published versions." />
            <ErrorBanner message={error} />
            <SuccessBanner message={success} />
            <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
                <Card title="Offer list">
                    <div className="grid gap-3">
                        {offers.map((offer) => (
                            <Link key={offer.id} href={`/catalog/offers/${offer.id}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <h3 className="font-medium">{offer.displayName}</h3>
                                        <p className="text-sm text-slate-400">{offer.code} • {offer.offerType}</p>
                                    </div>
                                    {offer.hasPublishedVersion ? <Badge tone="success">Published</Badge> : <Badge>Draft only</Badge>}
                                </div>
                                <p className="mt-3 text-sm text-slate-300">Versions: {offer.versionCount}</p>
                            </Link>
                        ))}
                    </div>
                </Card>
                <Card title="Create offer">
                    <form className="space-y-4" onSubmit={createOffer}>
                        <Field label="Code"><Input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} required /></Field>
                        <Field label="Offer type"><Select value={form.offerType} onChange={(event) => setForm((current) => ({ ...current, offerType: event.target.value }))}><option value="Package">Package</option><option value="StandaloneService">StandaloneService</option><option value="AddOn">AddOn</option></Select></Field>
                        <Field label="Display name"><Input value={form.displayName} onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))} required /></Field>
                        <PrimaryButton type="submit" className="w-full">Create offer</PrimaryButton>
                    </form>
                </Card>
            </div>
        </div>
    );
}

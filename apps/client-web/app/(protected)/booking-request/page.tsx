"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import type { ClientBookableOffer, ClientPetSummary } from "@/lib/types";
import { Button, Card, Input, Textarea } from "@/components/ui";

function toUtcIsoString(localDateTime: string) {
    if (!localDateTime) {
        return null;
    }

    const parsed = new Date(localDateTime);
    return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString();
}

export default function BookingRequestPage() {
    const [pets, setPets] = useState<ClientPetSummary[]>([]);
    const [offers, setOffers] = useState<ClientBookableOffer[]>([]);
    const [petId, setPetId] = useState("");
    const [offerId, setOfferId] = useState("");
    const [notes, setNotes] = useState("");
    const [preferredStartAtLocal, setPreferredStartAtLocal] = useState("");
    const [preferredEndAtLocal, setPreferredEndAtLocal] = useState("");
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isLoadingPets, setIsLoadingPets] = useState(true);
    const [isLoadingOffers, setIsLoadingOffers] = useState(false);

    const selectedOffer = useMemo(() => offers.find((item) => item.id === offerId) ?? null, [offers, offerId]);

    useEffect(() => {
        let isMounted = true;

        async function loadPets() {
            setIsLoadingPets(true);
            setError(null);
            try {
                const result = await apiRequest<ClientPetSummary[]>("/api/client/me/pets");
                if (!isMounted) {
                    return;
                }

                setPets(result);
                const firstPetId = result[0]?.id ?? "";
                setPetId(firstPetId);
            } catch (err) {
                if (isMounted) {
                    setError(err instanceof ApiError ? err.message : "Unable to load your pets.");
                }
            } finally {
                if (isMounted) {
                    setIsLoadingPets(false);
                }
            }
        }

        void loadPets();
        return () => {
            isMounted = false;
        };
    }, []);

    useEffect(() => {
        let isMounted = true;

        async function loadOffers(currentPetId: string) {
            if (!currentPetId) {
                setOffers([]);
                setOfferId("");
                return;
            }

            setIsLoadingOffers(true);
            setError(null);
            try {
                const result = await apiRequest<ClientBookableOffer[]>(`/api/client/booking-offers?petId=${currentPetId}`);
                if (!isMounted) {
                    return;
                }

                setOffers(result);
                setOfferId((current) => result.some((item) => item.id === current) ? current : (result[0]?.id ?? ""));
            } catch (err) {
                if (isMounted) {
                    setOffers([]);
                    setOfferId("");
                    setError(err instanceof ApiError ? err.message : "Unable to load available offers for this pet.");
                }
            } finally {
                if (isMounted) {
                    setIsLoadingOffers(false);
                }
            }
        }

        void loadOffers(petId);
        return () => {
            isMounted = false;
        };
    }, [petId]);

    async function onSubmit(event: FormEvent) {
        event.preventDefault();
        setError(null);
        setMessage(null);

        const preferredStartAtUtc = toUtcIsoString(preferredStartAtLocal);
        const preferredEndAtUtc = toUtcIsoString(preferredEndAtLocal);

        if ((preferredStartAtLocal && !preferredStartAtUtc) || (preferredEndAtLocal && !preferredEndAtUtc)) {
            setError("Preferred window must contain valid local date and time values.");
            return;
        }

        if ((preferredStartAtUtc && !preferredEndAtUtc) || (!preferredStartAtUtc && preferredEndAtUtc)) {
            setError("Provide both preferred start and preferred end, or leave both empty.");
            return;
        }

        setIsSubmitting(true);
        try {
            const payload = await apiRequest<{ id: string }>("/api/client/booking-requests", {
                method: "POST",
                body: JSON.stringify({
                    petId,
                    notes,
                    preferredTimes: preferredStartAtUtc && preferredEndAtUtc
                        ? [{ startAtUtc: preferredStartAtUtc, endAtUtc: preferredEndAtUtc, label: "Preferred window" }]
                        : [],
                    items: offerId ? [{ offerId }] : []
                })
            });
            setMessage(`Booking request submitted: ${payload.id}`);
            setNotes("");
            setPreferredStartAtLocal("");
            setPreferredEndAtLocal("");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Unable to submit booking request.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Booking request</p>
                <h2 className="mt-2 text-3xl font-semibold">Request a new appointment</h2>
                <p className="mt-2 text-sm text-slate-400">Choose one of your pets, review currently bookable offers, and optionally leave a preferred local time window.</p>
            </div>
            <Card>
                <form className="grid gap-4" onSubmit={onSubmit}>
                    <label className="grid gap-2 text-sm text-slate-300">
                        Pet
                        <select value={petId} onChange={(event) => setPetId(event.target.value)} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm" disabled={isLoadingPets || pets.length === 0}>
                            {pets.map((pet) => <option key={pet.id} value={pet.id}>{pet.name} · {pet.breedName}</option>)}
                        </select>
                    </label>

                    <label className="grid gap-2 text-sm text-slate-300">
                        Offer
                        <select value={offerId} onChange={(event) => setOfferId(event.target.value)} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm" disabled={isLoadingOffers || offers.length === 0}>
                            {offers.map((offer) => <option key={offer.id} value={offer.id}>{offer.displayName}</option>)}
                        </select>
                    </label>

                    {selectedOffer ? (
                        <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-300">
                            <div className="flex flex-wrap items-center justify-between gap-3">
                                <div>
                                    <p className="font-medium text-white">{selectedOffer.displayName}</p>
                                    <p className="mt-1 text-slate-400">{selectedOffer.offerType} · Estimated reservation {selectedOffer.reservedMinutes} min</p>
                                </div>
                                <div className="text-right">
                                    <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Estimate</p>
                                    <p className="mt-1 text-lg font-semibold text-emerald-300">{selectedOffer.priceAmount} {selectedOffer.currency}</p>
                                </div>
                            </div>
                            <div className="mt-3 grid gap-2 text-xs text-slate-400 md:grid-cols-2">
                                <p>Service time: {selectedOffer.serviceMinutes} min</p>
                                <p>Reserved slot: {selectedOffer.reservedMinutes} min</p>
                            </div>
                        </div>
                    ) : null}

                    <div className="grid gap-4 md:grid-cols-2">
                        <label className="grid gap-2 text-sm text-slate-300">
                            Preferred start (your local time)
                            <Input value={preferredStartAtLocal} onChange={(event) => setPreferredStartAtLocal(event.target.value)} type="datetime-local" />
                        </label>
                        <label className="grid gap-2 text-sm text-slate-300">
                            Preferred end (your local time)
                            <Input value={preferredEndAtLocal} onChange={(event) => setPreferredEndAtLocal(event.target.value)} type="datetime-local" />
                        </label>
                    </div>
                    <label className="grid gap-2 text-sm text-slate-300">
                        Notes
                        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} rows={4} placeholder="Anything important for the salon to know" />
                    </label>
                    {isLoadingPets ? <p className="text-sm text-slate-400">Loading pets…</p> : null}
                    {isLoadingOffers && petId ? <p className="text-sm text-slate-400">Loading offers for the selected pet…</p> : null}
                    {!isLoadingOffers && petId && offers.length === 0 ? <p className="text-sm text-amber-300">No bookable offers are currently available for this pet. Please contact the salon.</p> : null}
                    {message ? <p className="text-sm text-emerald-300">{message}</p> : null}
                    {error ? <p className="text-sm text-rose-300">{error}</p> : null}
                    <div className="flex justify-end">
                        <Button type="submit" disabled={isSubmitting || !petId || !offerId}>
                            {isSubmitting ? "Submitting…" : "Submit request"}
                        </Button>
                    </div>
                </form>
            </Card>
        </section>
    );
}

"use client";

import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import type { ClientPetSummary } from "@/lib/types";
import { Button, Card, Input, Textarea } from "@/components/ui";

export default function BookingRequestPage() {
    const [pets, setPets] = useState<ClientPetSummary[]>([]);
    const [petId, setPetId] = useState("");
    const [offerId, setOfferId] = useState("");
    const [notes, setNotes] = useState("");
    const [preferredStartAtUtc, setPreferredStartAtUtc] = useState("");
    const [preferredEndAtUtc, setPreferredEndAtUtc] = useState("");
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        apiRequest<ClientPetSummary[]>("/api/client/me/pets")
            .then((result) => {
                setPets(result);
                if (result[0]) {
                    setPetId(result[0].id);
                }
            })
            .catch((err: Error) => setError(err.message));
    }, []);

    async function onSubmit(event: FormEvent) {
        event.preventDefault();
        setError(null);
        setMessage(null);

        try {
            const payload = await apiRequest<{ id: string }>("/api/client/booking-requests", {
                method: "POST",
                body: JSON.stringify({
                    petId,
                    notes,
                    preferredTimes: preferredStartAtUtc && preferredEndAtUtc ? [{ startAtUtc: preferredStartAtUtc, endAtUtc: preferredEndAtUtc, label: "Preferred" }] : [],
                    items: [{ offerId }]
                })
            });
            setMessage(`Booking request submitted: ${payload.id}`);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Unable to submit booking request.");
        }
    }

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Booking request</p>
                <h2 className="mt-2 text-3xl font-semibold">Request a new appointment</h2>
                <p className="mt-2 text-sm text-slate-400">For MVP, enter an offer ID from the salon catalog and a preferred window.</p>
            </div>
            <Card>
                <form className="grid gap-4" onSubmit={onSubmit}>
                    <label className="grid gap-2 text-sm text-slate-300">
                        Pet
                        <select value={petId} onChange={(event) => setPetId(event.target.value)} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm">
                            {pets.map((pet) => <option key={pet.id} value={pet.id}>{pet.name}</option>)}
                        </select>
                    </label>
                    <label className="grid gap-2 text-sm text-slate-300">
                        Offer ID
                        <Input value={offerId} onChange={(event) => setOfferId(event.target.value)} placeholder="Paste offer GUID from salon/admin for MVP" />
                    </label>
                    <div className="grid gap-4 md:grid-cols-2">
                        <label className="grid gap-2 text-sm text-slate-300">
                            Preferred start (UTC)
                            <Input value={preferredStartAtUtc} onChange={(event) => setPreferredStartAtUtc(event.target.value)} type="datetime-local" />
                        </label>
                        <label className="grid gap-2 text-sm text-slate-300">
                            Preferred end (UTC)
                            <Input value={preferredEndAtUtc} onChange={(event) => setPreferredEndAtUtc(event.target.value)} type="datetime-local" />
                        </label>
                    </div>
                    <label className="grid gap-2 text-sm text-slate-300">
                        Notes
                        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} rows={4} placeholder="Anything important for the salon to know" />
                    </label>
                    {message ? <p className="text-sm text-emerald-300">{message}</p> : null}
                    {error ? <p className="text-sm text-rose-300">{error}</p> : null}
                    <div className="flex justify-end">
                        <Button type="submit">Submit request</Button>
                    </div>
                </form>
            </Card>
        </section>
    );
}

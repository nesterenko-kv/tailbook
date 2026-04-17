"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { apiRequest } from "@/lib/api";
import type { ClientPetSummary } from "@/lib/types";
import { Card } from "@/components/ui";

export default function MyPetsPage() {
    const [pets, setPets] = useState<ClientPetSummary[]>([]);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        apiRequest<ClientPetSummary[]>("/api/client/me/pets")
            .then(setPets)
            .catch((err: Error) => setError(err.message));
    }, []);

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Client portal</p>
                <h2 className="mt-2 text-3xl font-semibold">My pets</h2>
                <p className="mt-2 text-sm text-slate-400">View pets already linked to your client account.</p>
            </div>
            {error ? <p className="text-sm text-rose-300">{error}</p> : null}
            <div className="grid gap-4 md:grid-cols-2">
                {pets.map((pet) => (
                    <Link key={pet.id} href={`/pets/${pet.id}`}>
                        <Card className="space-y-2 hover:border-emerald-500/40">
                            <h3 className="text-lg font-semibold">{pet.name}</h3>
                            <p className="text-sm text-slate-300">{pet.animalTypeName} • {pet.breedName}</p>
                            <p className="text-sm text-slate-400">{pet.notes ?? "No extra notes yet."}</p>
                        </Card>
                    </Link>
                ))}
                {pets.length === 0 ? <Card><p className="text-sm text-slate-400">No pets are linked yet. An administrator can register them first for MVP.</p></Card> : null}
            </div>
        </section>
    );
}

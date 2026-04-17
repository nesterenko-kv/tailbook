"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest } from "@/lib/api";
import type { ClientPetDetail } from "@/lib/types";
import { Card } from "@/components/ui";

export default function ClientPetDetailPage() {
    const params = useParams<{ petId: string }>();
    const [pet, setPet] = useState<ClientPetDetail | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        apiRequest<ClientPetDetail>(`/api/client/me/pets/${params.petId}`)
            .then(setPet)
            .catch((err: Error) => setError(err.message));
    }, [params.petId]);

    if (error) {
        return <p className="text-sm text-rose-300">{error}</p>;
    }

    if (!pet) {
        return <p className="text-sm text-slate-400">Loading pet…</p>;
    }

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Pet profile</p>
                <h2 className="mt-2 text-3xl font-semibold">{pet.name}</h2>
                <p className="mt-2 text-sm text-slate-300">{pet.animalTypeName} • {pet.breedName}</p>
            </div>
            <Card className="space-y-2">
                <p className="text-sm text-slate-400">Coat: {pet.coatTypeCode ?? "—"}</p>
                <p className="text-sm text-slate-400">Size: {pet.sizeCategoryCode ?? "—"}</p>
                <p className="text-sm text-slate-400">Birth date: {pet.birthDate ?? "—"}</p>
                <p className="text-sm text-slate-400">Weight: {pet.weightKg ?? "—"}</p>
                <p className="text-sm text-slate-300">{pet.notes ?? "No extra notes."}</p>
            </Card>
        </section>
    );
}

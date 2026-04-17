"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { addRecentPetId, getRecentPetIds } from "@/lib/recent";
import type { ClientListItem, PagedResult, PetCatalog, PetDetail } from "@/lib/types";
import { Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

export default function PetsPage() {
    const router = useRouter();
    const [catalog, setCatalog] = useState<PetCatalog | null>(null);
    const [clients, setClients] = useState<ClientListItem[]>([]);
    const [recentPetIds, setRecentPetIds] = useState<string[]>([]);
    const [lookupPetId, setLookupPetId] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const previousBreedIdRef = useRef<string>("");
    const [form, setForm] = useState({
        clientId: "",
        name: "",
        animalTypeCode: "DOG",
        breedId: "",
        coatTypeCode: "",
        sizeCategoryCode: "",
        birthDate: "",
        weightKg: "",
        notes: ""
    });

    useEffect(() => {
        Promise.all([
            apiRequest<PetCatalog>("/api/admin/pets/catalog"),
            apiRequest<PagedResult<ClientListItem>>("/api/admin/clients?page=1&pageSize=100")
        ])
            .then(([catalogResponse, clientsResponse]) => {
                setCatalog(catalogResponse);
                setClients(clientsResponse.items);
                setRecentPetIds(getRecentPetIds());
                if (catalogResponse.animalTypes[0] && !form.breedId) {
                    const firstAnimalType = catalogResponse.animalTypes[0].code;
                    const firstBreed = catalogResponse.breeds.find((breed) => breed.animalTypeId === catalogResponse.animalTypes[0].id)?.id ?? "";
                    setForm((current) => ({ ...current, animalTypeCode: firstAnimalType, breedId: firstBreed }));
                }
            })
            .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load pet setup data."));
    }, []);

    const selectedAnimalType = useMemo(() => catalog?.animalTypes.find((item) => item.code === form.animalTypeCode) ?? null, [catalog, form.animalTypeCode]);
    const breedOptions = useMemo(() => catalog?.breeds.filter((breed) => breed.animalTypeId === selectedAnimalType?.id) ?? [], [catalog, selectedAnimalType]);
    const selectedBreed = useMemo(() => breedOptions.find((breed) => breed.id === form.breedId) ?? null, [breedOptions, form.breedId]);
    const coatOptions = useMemo(() => {
        const allowedCoatTypeIds = new Set(selectedBreed?.allowedCoatTypeIds ?? []);
        return catalog?.coatTypes.filter((coat) => allowedCoatTypeIds.has(coat.id)) ?? [];
    }, [catalog, selectedBreed]);
    const sizeOptions = useMemo(() => {
        const allowedSizeCategoryIds = new Set(selectedBreed?.allowedSizeCategoryIds ?? []);
        return catalog?.sizeCategories.filter((size) => allowedSizeCategoryIds.has(size.id) && (!size.animalTypeId || size.animalTypeId === selectedAnimalType?.id)) ?? [];
    }, [catalog, selectedAnimalType, selectedBreed]);

    useEffect(() => {
        if (breedOptions.length === 0) {
            if (form.breedId) {
                setForm((current) => ({ ...current, breedId: "" }));
            }
            return;
        }

        if (!breedOptions.some((breed) => breed.id === form.breedId)) {
            setForm((current) => ({ ...current, breedId: breedOptions[0].id }));
        }
    }, [breedOptions, form.breedId]);

    useEffect(() => {
        const breedIdChanged = previousBreedIdRef.current !== form.breedId;
        if (!breedIdChanged) {
            return;
        }

        previousBreedIdRef.current = form.breedId;

        setForm((current) => {
            const nextCoatTypeCode = coatOptions.some((coat) => coat.code === current.coatTypeCode)
                ? current.coatTypeCode
                : coatOptions.length === 1
                    ? coatOptions[0].code
                    : "";
            const nextSizeCategoryCode = sizeOptions.some((size) => size.code === current.sizeCategoryCode)
                ? current.sizeCategoryCode
                : sizeOptions.length === 1
                    ? sizeOptions[0].code
                    : "";

            if (nextCoatTypeCode === current.coatTypeCode && nextSizeCategoryCode === current.sizeCategoryCode) {
                return current;
            }

            return {
                ...current,
                coatTypeCode: nextCoatTypeCode,
                sizeCategoryCode: nextSizeCategoryCode
            };
        });
    }, [form.breedId, coatOptions, sizeOptions]);

    async function registerPet(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);

        try {
            const result = await apiRequest<PetDetail>("/api/admin/pets", {
                method: "POST",
                body: JSON.stringify({
                    clientId: form.clientId || null,
                    name: form.name,
                    animalTypeCode: form.animalTypeCode,
                    breedId: form.breedId,
                    coatTypeCode: form.coatTypeCode || null,
                    sizeCategoryCode: form.sizeCategoryCode || null,
                    birthDate: form.birthDate || null,
                    weightKg: form.weightKg ? Number(form.weightKg) : null,
                    notes: form.notes || null
                })
            });

            addRecentPetId(result.id);
            setRecentPetIds(getRecentPetIds());
            setSuccess(`Pet ${result.name} registered.`);
            router.push(`/pets/${result.id}`);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to register pet.");
        }
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader eyebrow="Pet registry" title="Pets" description="Register pets, inspect catalog taxonomy, and reopen recent pet records." />
            <ErrorBanner message={error} />
            <SuccessBanner message={success} />

            <div className="grid gap-6 xl:grid-cols-[1.35fr_1fr]">
                <Card title="Register pet" description="This MVP view creates a pet and then opens the full detail page.">
                    <form className="grid gap-4 md:grid-cols-2" onSubmit={registerPet}>
                        <Field label="Client"><Select value={form.clientId} onChange={(event) => setForm((current) => ({ ...current, clientId: event.target.value }))}><option value="">No client yet</option>{clients.map((client) => <option key={client.id} value={client.id}>{client.displayName}</option>)}</Select></Field>
                        <Field label="Pet name"><Input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required /></Field>
                        <Field label="Animal type"><Select value={form.animalTypeCode} onChange={(event) => setForm((current) => ({ ...current, animalTypeCode: event.target.value }))}>{catalog?.animalTypes.map((item) => <option key={item.id} value={item.code}>{item.name}</option>)}</Select></Field>
                        <Field label="Breed"><Select value={form.breedId} onChange={(event) => setForm((current) => ({ ...current, breedId: event.target.value }))}>{breedOptions.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</Select></Field>
                        <Field label="Coat type"><Select value={form.coatTypeCode} onChange={(event) => setForm((current) => ({ ...current, coatTypeCode: event.target.value }))}><option value="">None</option>{coatOptions.map((item) => <option key={item.id} value={item.code}>{item.name}</option>)}</Select></Field>
                        <Field label="Size category"><Select value={form.sizeCategoryCode} onChange={(event) => setForm((current) => ({ ...current, sizeCategoryCode: event.target.value }))}><option value="">None</option>{sizeOptions.map((item) => <option key={item.id} value={item.code}>{item.name}</option>)}</Select></Field>
                        <Field label="Birth date"><Input type="date" value={form.birthDate} onChange={(event) => setForm((current) => ({ ...current, birthDate: event.target.value }))} /></Field>
                        <Field label="Weight (kg)"><Input type="number" step="0.1" value={form.weightKg} onChange={(event) => setForm((current) => ({ ...current, weightKg: event.target.value }))} /></Field>
                        <div className="md:col-span-2"><Field label="Notes"><TextArea value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} /></Field></div>
                        <PrimaryButton type="submit" className="md:col-span-2">Register pet</PrimaryButton>
                    </form>
                </Card>

                <div className="grid gap-6">
                    <Card title="Open pet by id" description="There is no dedicated pet list endpoint yet, so this MVP uses lookup + recent records.">
                        <div className="flex gap-3">
                            <Input value={lookupPetId} onChange={(event) => setLookupPetId(event.target.value)} placeholder="Paste pet id" />
                            <PrimaryButton type="button" onClick={() => router.push(`/pets/${lookupPetId}`)} disabled={!lookupPetId}>Open</PrimaryButton>
                        </div>
                        <div className="mt-4 grid gap-2">
                            {recentPetIds.map((petId) => (
                                <Link key={petId} href={`/pets/${petId}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3 text-sm text-slate-200 transition hover:border-emerald-500/40">{petId}</Link>
                            ))}
                            {recentPetIds.length === 0 ? <p className="text-sm text-slate-400">No recent pets yet.</p> : null}
                        </div>
                    </Card>

                    <Card title="Catalog snapshot">
                        <div className="grid gap-2 text-sm text-slate-300">
                            <p>Animal types: {catalog?.animalTypes.length ?? 0}</p>
                            <p>Breeds: {catalog?.breeds.length ?? 0}</p>
                            <p>Coat types: {catalog?.coatTypes.length ?? 0}</p>
                            <p>Size categories: {catalog?.sizeCategories.length ?? 0}</p>
                        </div>
                    </Card>
                </div>
            </div>
        </div>
    );
}

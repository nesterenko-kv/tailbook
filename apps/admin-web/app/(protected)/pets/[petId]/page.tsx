"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { addRecentPetId } from "@/lib/recent";
import type { ClientDetail, PetCatalog, PetDetail } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, LinkButton, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

const roleOptions = ["owner", "primary_contact", "pickup_allowed", "payer", "emergency_contact", "booking_requester", "notification_recipient"];

export default function PetDetailPage() {
    const params = useParams<{ petId: string }>();
    const petId = String(params.petId);
    const [pet, setPet] = useState<PetDetail | null>(null);
    const [catalog, setCatalog] = useState<PetCatalog | null>(null);
    const [client, setClient] = useState<ClientDetail | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const previousBreedIdRef = useRef<string>("");
    const [updateForm, setUpdateForm] = useState({ animalTypeCode: "", breedId: "", coatTypeCode: "", sizeCategoryCode: "", birthDate: "", weightKg: "", notes: "" });
    const [linkForm, setLinkForm] = useState({ contactId: "", roleCodes: ["owner"] as string[], isPrimary: false, canPickUp: true, canPay: false, receivesNotifications: true });

    async function load() {
        setError(null);
        try {
            const [petResponse, catalogResponse] = await Promise.all([
                apiRequest<PetDetail>(`/api/admin/pets/${petId}`),
                apiRequest<PetCatalog>("/api/admin/pets/catalog")
            ]);

            setPet(petResponse);
            setCatalog(catalogResponse);
            addRecentPetId(petId);
            setUpdateForm({
                animalTypeCode: petResponse.animalType.code,
                breedId: petResponse.breed.id,
                coatTypeCode: petResponse.coatType?.code ?? "",
                sizeCategoryCode: petResponse.sizeCategory?.code ?? "",
                birthDate: petResponse.birthDate ?? "",
                weightKg: petResponse.weightKg?.toString() ?? "",
                notes: petResponse.notes ?? ""
            });

            if (petResponse.clientId) {
                const clientResponse = await apiRequest<ClientDetail>(`/api/admin/clients/${petResponse.clientId}`);
                setClient(clientResponse);
                if (clientResponse.contacts[0]) {
                    setLinkForm((current) => ({ ...current, contactId: current.contactId || clientResponse.contacts[0].id }));
                }
            } else {
                setClient(null);
            }
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load pet.");
        }
    }

    useEffect(() => {
        void load();
    }, [petId]);

    const selectedAnimalType = useMemo(() => catalog?.animalTypes.find((item) => item.code === updateForm.animalTypeCode) ?? null, [catalog, updateForm.animalTypeCode]);
    const breedOptions = useMemo(() => catalog?.breeds.filter((item) => item.animalTypeId === selectedAnimalType?.id) ?? [], [catalog, selectedAnimalType]);
    const selectedBreed = useMemo(() => breedOptions.find((item) => item.id === updateForm.breedId) ?? null, [breedOptions, updateForm.breedId]);
    const coatOptions = useMemo(() => {
        const allowedCoatTypeIds = new Set(selectedBreed?.allowedCoatTypeIds ?? []);
        return catalog?.coatTypes.filter((item) => allowedCoatTypeIds.has(item.id)) ?? [];
    }, [catalog, selectedBreed]);
    const sizeOptions = useMemo(() => {
        const allowedSizeCategoryIds = new Set(selectedBreed?.allowedSizeCategoryIds ?? []);
        return catalog?.sizeCategories.filter((item) => allowedSizeCategoryIds.has(item.id) && (!item.animalTypeId || item.animalTypeId === selectedAnimalType?.id)) ?? [];
    }, [catalog, selectedAnimalType, selectedBreed]);

    useEffect(() => {
        if (breedOptions.length === 0) {
            if (updateForm.breedId) {
                setUpdateForm((current) => ({ ...current, breedId: "" }));
            }
            return;
        }

        if (!breedOptions.some((breed) => breed.id === updateForm.breedId)) {
            setUpdateForm((current) => ({ ...current, breedId: breedOptions[0].id }));
        }
    }, [breedOptions, updateForm.breedId]);

    useEffect(() => {
        const breedIdChanged = previousBreedIdRef.current !== updateForm.breedId;
        if (!breedIdChanged) {
            return;
        }

        previousBreedIdRef.current = updateForm.breedId;

        setUpdateForm((current) => {
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
    }, [updateForm.breedId, coatOptions, sizeOptions]);

    async function updatePet(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/pets/${petId}`, {
                method: "PATCH",
                body: JSON.stringify({
                    id: petId,
                    name: pet?.name,
                    animalTypeCode: updateForm.animalTypeCode,
                    breedId: updateForm.breedId,
                    coatTypeCode: updateForm.coatTypeCode || null,
                    sizeCategoryCode: updateForm.sizeCategoryCode || null,
                    birthDate: updateForm.birthDate || null,
                    weightKg: updateForm.weightKg ? Number(updateForm.weightKg) : null,
                    notes: updateForm.notes || null
                })
            });
            setSuccess("Pet updated.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to update pet.");
        }
    }

    async function linkContact(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/pets/${petId}/contacts/${linkForm.contactId}`, {
                method: "POST",
                body: JSON.stringify({
                    petId,
                    contactId: linkForm.contactId,
                    roleCodes: linkForm.roleCodes,
                    isPrimary: linkForm.isPrimary,
                    canPickUp: linkForm.canPickUp,
                    canPay: linkForm.canPay,
                    receivesNotifications: linkForm.receivesNotifications
                })
            });
            setSuccess("Contact linked to pet.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to link contact.");
        }
    }

    function toggleRole(role: string) {
        setLinkForm((current) => ({
            ...current,
            roleCodes: current.roleCodes.includes(role)
                ? current.roleCodes.filter((item) => item !== role)
                : [...current.roleCodes, role]
        }));
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader
                eyebrow="Pet detail"
                title={pet?.name ?? "Pet detail"}
                description="Optional photos stay non-blocking. Use this page to maintain pet traits and CRM links."
                action={<LinkButton href="/pets">Back to pets</LinkButton>}
            />
            <ErrorBanner message={error} />
            <SuccessBanner message={success} />

            {pet ? (
                <>
                    <div className="grid gap-6 xl:grid-cols-[1.35fr_1fr]">
                        <Card title="Pet profile">
                            <div className="grid gap-2 text-sm text-slate-300">
                                <div className="flex flex-wrap items-center gap-2">
                                    <Badge>{pet.animalType.name}</Badge>
                                    <Badge>{pet.breed.name}</Badge>
                                    {pet.coatType ? <Badge>{pet.coatType.name}</Badge> : null}
                                    {pet.sizeCategory ? <Badge>{pet.sizeCategory.name}</Badge> : null}
                                </div>
                                <p>Client: {pet.clientId ? <Link className="text-emerald-300" href={`/clients/${pet.clientId}`}>{pet.clientId}</Link> : "None"}</p>
                                <p>Birth date: {pet.birthDate ?? "—"}</p>
                                <p>Weight: {pet.weightKg ?? "—"}</p>
                                <p>Updated: {formatDateTime(pet.updatedAtUtc)}</p>
                                <p>{pet.notes || "No notes."}</p>
                            </div>
                        </Card>

                        <Card title="Update pet traits">
                            <form className="space-y-4" onSubmit={updatePet}>
                                <Field label="Animal type"><Select value={updateForm.animalTypeCode} onChange={(event) => setUpdateForm((current) => ({ ...current, animalTypeCode: event.target.value }))}>{catalog?.animalTypes.map((item) => <option key={item.id} value={item.code}>{item.name}</option>)}</Select></Field>
                                <Field label="Breed"><Select value={updateForm.breedId} onChange={(event) => setUpdateForm((current) => ({ ...current, breedId: event.target.value }))}>{breedOptions.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</Select></Field>
                                <Field label="Coat type"><Select value={updateForm.coatTypeCode} onChange={(event) => setUpdateForm((current) => ({ ...current, coatTypeCode: event.target.value }))}><option value="">None</option>{coatOptions.map((item) => <option key={item.id} value={item.code}>{item.name}</option>)}</Select></Field>
                                <Field label="Size category"><Select value={updateForm.sizeCategoryCode} onChange={(event) => setUpdateForm((current) => ({ ...current, sizeCategoryCode: event.target.value }))}><option value="">None</option>{sizeOptions.map((item) => <option key={item.id} value={item.code}>{item.name}</option>)}</Select></Field>
                                <Field label="Birth date"><Input type="date" value={updateForm.birthDate} onChange={(event) => setUpdateForm((current) => ({ ...current, birthDate: event.target.value }))} /></Field>
                                <Field label="Weight (kg)"><Input type="number" step="0.1" value={updateForm.weightKg} onChange={(event) => setUpdateForm((current) => ({ ...current, weightKg: event.target.value }))} /></Field>
                                <Field label="Notes"><TextArea value={updateForm.notes} onChange={(event) => setUpdateForm((current) => ({ ...current, notes: event.target.value }))} /></Field>
                                <PrimaryButton type="submit" className="w-full">Save pet</PrimaryButton>
                            </form>
                        </Card>
                    </div>

                    <div className="grid gap-6 xl:grid-cols-[1.35fr_1fr]">
                        <Card title="Linked contacts">
                            <div className="grid gap-3">
                                {pet.contacts.map((contact) => (
                                    <article key={contact.contactId} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                        <div className="flex items-start justify-between gap-3">
                                            <div>
                                                <h3 className="font-medium">{contact.fullName}</h3>
                                                <p className="text-sm text-slate-400">{contact.roleCodes.join(", ") || "No roles"}</p>
                                            </div>
                                            {contact.isPrimary ? <Badge tone="success">Primary</Badge> : null}
                                        </div>
                                        <div className="mt-3 flex flex-wrap gap-2">
                                            {contact.methods.map((method) => (
                                                <span key={method.id} className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-200">{method.methodType}: {method.displayValue}</span>
                                            ))}
                                        </div>
                                    </article>
                                ))}
                                {pet.contacts.length === 0 ? <p className="text-sm text-slate-400">No contacts linked.</p> : null}
                            </div>
                        </Card>

                        <Card title="Link client contact" description="Only contacts from the same client are offered here.">
                            {client ? (
                                <form className="space-y-4" onSubmit={linkContact}>
                                    <Field label="Contact"><Select value={linkForm.contactId} onChange={(event) => setLinkForm((current) => ({ ...current, contactId: event.target.value }))}>{client.contacts.map((contact) => <option key={contact.id} value={contact.id}>{contact.firstName} {contact.lastName ?? ""}</option>)}</Select></Field>
                                    <div className="grid gap-2 text-sm text-slate-300">
                                        <span>Roles</span>
                                        {roleOptions.map((role) => (
                                            <label key={role} className="flex items-center gap-2"><input type="checkbox" checked={linkForm.roleCodes.includes(role)} onChange={() => toggleRole(role)} /> {role}</label>
                                        ))}
                                    </div>
                                    <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={linkForm.isPrimary} onChange={(event) => setLinkForm((current) => ({ ...current, isPrimary: event.target.checked }))} /> Primary contact</label>
                                    <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={linkForm.canPickUp} onChange={(event) => setLinkForm((current) => ({ ...current, canPickUp: event.target.checked }))} /> Can pick up</label>
                                    <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={linkForm.canPay} onChange={(event) => setLinkForm((current) => ({ ...current, canPay: event.target.checked }))} /> Can pay</label>
                                    <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={linkForm.receivesNotifications} onChange={(event) => setLinkForm((current) => ({ ...current, receivesNotifications: event.target.checked }))} /> Receives notifications</label>
                                    <PrimaryButton type="submit" className="w-full">Link contact</PrimaryButton>
                                </form>
                            ) : (
                                <p className="text-sm text-slate-400">This pet has no client-linked contact source yet.</p>
                            )}
                        </Card>
                    </div>
                </>
            ) : null}
        </div>
    );
}

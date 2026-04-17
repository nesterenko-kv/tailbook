"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { getStoredAccessToken } from "@/lib/auth";
import type {
    BookingRequestDetail,
    ClientMeResponse,
    ClientPetSummary,
    PetCatalog,
    PublicBookableOffer,
    PublicBookingPlanner,
    PublicPetPayload
} from "@/lib/types";
import { Button, Card, Input, Textarea } from "@/components/ui";

type BookingMode = "any" | "specific" | "preferred";
type PetMode = "saved" | "custom";

type RequesterState = {
    displayName: string;
    phone: string;
    instagramHandle: string;
    email: string;
    preferredContactMethodCode: string;
};

type PetFormState = {
    animalTypeId: string;
    breedId: string;
    coatTypeId: string;
    sizeCategoryId: string;
    weightKg: string;
    petName: string;
    notes: string;
};

function todayLocalDate() {
    return new Intl.DateTimeFormat("sv-SE", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    }).format(new Date());
}

function toUtcIsoString(localDateTime: string) {
    if (!localDateTime) {
        return null;
    }

    const parsed = new Date(localDateTime);
    return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString();
}

function toLocalDateTimeValue(utcValue: string) {
    const parsed = new Date(utcValue);
    if (Number.isNaN(parsed.getTime())) {
        return "";
    }

    const offset = parsed.getTimezoneOffset();
    const local = new Date(parsed.getTime() - offset * 60000);
    return local.toISOString().slice(0, 16);
}

function formatSlotRange(startAtUtc: string, endAtUtc: string) {
    const start = new Date(startAtUtc);
    const end = new Date(endAtUtc);
    return new Intl.DateTimeFormat("uk-UA", {
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit"
    }).format(start) + " – " + new Intl.DateTimeFormat("uk-UA", {
        hour: "2-digit",
        minute: "2-digit"
    }).format(end);
}

function slotKey(slot: { startAtUtc: string; endAtUtc: string }) {
    return `${slot.startAtUtc}|${slot.endAtUtc}`;
}

export function BookingRequestFlow({ variant }: { variant: "public" | "portal" }) {
    const isPortalVariant = variant ==="portal";
    const [me, setMe] = useState<ClientMeResponse | null>(null);
    const [pets, setPets] = useState<ClientPetSummary[]>([]);
    const [catalog, setCatalog] = useState<PetCatalog | null>(null);
    const [petMode, setPetMode] = useState<PetMode>("custom");
    const [selectedPetId, setSelectedPetId] = useState("");
    const [petForm, setPetForm] = useState<PetFormState>({
        animalTypeId: "",
        breedId: "",
        coatTypeId: "",
        sizeCategoryId: "",
        weightKg: "",
        petName: "",
        notes: ""
    });
    const [offers, setOffers] = useState<PublicBookableOffer[]>([]);
    const [offerId, setOfferId] = useState("");
    const [planner, setPlanner] = useState<PublicBookingPlanner | null>(null);
    const [selectedDate, setSelectedDate] = useState(todayLocalDate());
    const [bookingMode, setBookingMode] = useState<BookingMode>("any");
    const [selectedGroomerId, setSelectedGroomerId] = useState("");
    const [selectedSlotKey, setSelectedSlotKey] = useState("");
    const [preferredStartLocal, setPreferredStartLocal] = useState("");
    const [preferredEndLocal, setPreferredEndLocal] = useState("");
    const [requester, setRequester] = useState<RequesterState>({
        displayName: "",
        phone: "",
        instagramHandle: "",
        email: "",
        preferredContactMethodCode: "Phone"
    });
    const [notes, setNotes] = useState("");
    const [busy, setBusy] = useState(false);
    const [offersLoading, setOffersLoading] = useState(false);
    const [plannerLoading, setPlannerLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    const authenticated = me !== null;

    useEffect(() => {
        let isMounted = true;

        async function loadBase() {
            try {
                const petCatalogPromise = apiRequest<PetCatalog>("/api/public/pets/catalog");
                const token = getStoredAccessToken();
                const mePromise = token ? apiRequest<ClientMeResponse>("/api/client/me").catch(() => null) : Promise.resolve(null);
                const [petCatalog, meResponse] = await Promise.all([petCatalogPromise, mePromise]);
                if (!isMounted) {
                    return;
                }

                setCatalog(petCatalog);
                if (meResponse) {
                    setMe(meResponse);
                    setRequester((current) => ({
                        ...current,
                        email: current.email || meResponse.email,
                        displayName: current.displayName || meResponse.displayName
                    }));

                    const petList = await apiRequest<ClientPetSummary[]>("/api/client/me/pets").catch(() => []);
                    if (!isMounted) {
                        return;
                    }

                    setPets(petList);
                    if (petList.length > 0) {
                        setPetMode("saved");
                        setSelectedPetId(petList[0].id);
                    }
                }
            } catch (err) {
                if (isMounted) {
                    setError(err instanceof ApiError ? err.message : "Unable to load booking form data.");
                }
            }
        }

        void loadBase();
        return () => {
            isMounted = false;
        };
    }, []);

    const visibleBreeds = useMemo(() => {
        if (!catalog || !petForm.animalTypeId) {
            return [];
        }

        return catalog.breeds.filter((breed) => breed.animalTypeId === petForm.animalTypeId);
    }, [catalog, petForm.animalTypeId]);

    const selectedBreed = useMemo(
        () => visibleBreeds.find((breed) => breed.id === petForm.breedId) ?? null,
        [visibleBreeds, petForm.breedId]
    );

    const visibleCoatTypes = useMemo(() => {
        if (!catalog || !selectedBreed) {
            return [];
        }

        return catalog.coatTypes.filter((coatType) => selectedBreed.allowedCoatTypeIds.includes(coatType.id));
    }, [catalog, selectedBreed]);

    const visibleSizeCategories = useMemo(() => {
        if (!catalog || !selectedBreed) {
            return [];
        }

        return catalog.sizeCategories.filter((sizeCategory) => selectedBreed.allowedSizeCategoryIds.includes(sizeCategory.id));
    }, [catalog, selectedBreed]);

    useEffect(() => {
        if (!catalog || petMode === "saved") {
            return;
        }

        setPetForm((current) => {
            let next = { ...current };
            if (!next.animalTypeId) {
                next.animalTypeId = catalog.animalTypes[0]?.id ?? "";
            }

            const breedsForAnimal = catalog.breeds.filter((breed) => breed.animalTypeId === next.animalTypeId);
            if (!breedsForAnimal.some((breed) => breed.id === next.breedId)) {
                next.breedId = breedsForAnimal[0]?.id ?? "";
            }

            const breed = breedsForAnimal.find((item) => item.id === next.breedId);
            const allowedCoats = breed ? catalog.coatTypes.filter((coat) => breed.allowedCoatTypeIds.includes(coat.id)) : [];
            if (!allowedCoats.some((coat) => coat.id === next.coatTypeId)) {
                next.coatTypeId = allowedCoats.length === 1 ? allowedCoats[0].id : "";
            }

            const allowedSizes = breed ? catalog.sizeCategories.filter((size) => breed.allowedSizeCategoryIds.includes(size.id)) : [];
            if (!allowedSizes.some((size) => size.id === next.sizeCategoryId)) {
                next.sizeCategoryId = allowedSizes.length === 1 ? allowedSizes[0].id : "";
            }

            return next;
        });
    }, [catalog, petMode]);

    useEffect(() => {
        if (bookingMode !== "specific") {
            return;
        }

        const groomerIds = new Set((planner?.groomers ?? []).filter((groomer) => groomer.canTakeRequest).map((groomer) => groomer.groomerId));
        if (selectedGroomerId && !groomerIds.has(selectedGroomerId)) {
            setSelectedGroomerId("");
        }
    }, [bookingMode, planner, selectedGroomerId]);

    const activePetPayload = useMemo<PublicPetPayload | null>(() => {
        if (petMode === "saved") {
            return selectedPetId ? { petId: selectedPetId } : null;
        }

        if (!petForm.animalTypeId || !petForm.breedId) {
            return null;
        }

        return {
            animalTypeId: petForm.animalTypeId,
            breedId: petForm.breedId,
            coatTypeId: petForm.coatTypeId || null,
            sizeCategoryId: petForm.sizeCategoryId || null,
            weightKg: petForm.weightKg ? Number(petForm.weightKg) : null,
            petName: petForm.petName || null,
            notes: petForm.notes || null
        };
    }, [petMode, petForm, selectedPetId]);

    useEffect(() => {
        let isMounted = true;

        async function loadOffers() {
            if (!activePetPayload) {
                setOffers([]);
                setOfferId("");
                return;
            }

            setOffersLoading(true);
            setError(null);
            try {
                const result = await apiRequest<PublicBookableOffer[]>("/api/public/booking-offers", {
                    method: "POST",
                    body: JSON.stringify({ pet: activePetPayload })
                });
                if (!isMounted) {
                    return;
                }

                setOffers(result);
                setOfferId((current) => result.some((item) => item.id === current) ? current : (result[0]?.id ?? ""));
            } catch (err) {
                if (isMounted) {
                    setOffers([]);
                    setOfferId("");
                    setError(err instanceof ApiError ? err.message : "Unable to load available services.");
                }
            } finally {
                if (isMounted) {
                    setOffersLoading(false);
                }
            }
        }

        void loadOffers();
        return () => {
            isMounted = false;
        };
    }, [activePetPayload]);

    useEffect(() => {
        let isMounted = true;

        async function loadPlanner() {
            if (!activePetPayload || !offerId || !selectedDate) {
                setPlanner(null);
                return;
            }

            setPlannerLoading(true);
            setError(null);
            try {
                const result = await apiRequest<PublicBookingPlanner>("/api/public/booking-planner", {
                    method: "POST",
                    body: JSON.stringify({
                        pet: activePetPayload,
                        localDate: selectedDate,
                        items: [{ offerId }]
                    })
                });
                if (!isMounted) {
                    return;
                }

                setPlanner(result);
                setSelectedSlotKey((current) => {
                    const allSlots = result.anySuitableSlots.concat(result.groomers.flatMap((groomer) => groomer.slots));
                    return allSlots.some((slot) => slotKey(slot) === current) ? current : "";
                });
                if (bookingMode === "specific" && !selectedGroomerId) {
                    const firstAvailableGroomer = result.groomers.find((groomer) => groomer.canTakeRequest && groomer.slots.length > 0);
                    if (firstAvailableGroomer) {
                        setSelectedGroomerId(firstAvailableGroomer.groomerId);
                    }
                }
            } catch (err) {
                if (isMounted) {
                    setPlanner(null);
                    setError(err instanceof ApiError ? err.message : "Unable to calculate availability.");
                }
            } finally {
                if (isMounted) {
                    setPlannerLoading(false);
                }
            }
        }

        void loadPlanner();
        return () => {
            isMounted = false;
        };
    }, [activePetPayload, offerId, selectedDate]);

    const selectedOffer = useMemo(() => offers.find((item) => item.id === offerId) ?? null, [offerId, offers]);

    const selectedGroomer = useMemo(
        () => planner?.groomers.find((groomer) => groomer.groomerId === selectedGroomerId) ?? null,
        [planner, selectedGroomerId]
    );

    const visibleSlots = useMemo(() => {
        if (!planner) {
            return [];
        }

        if (bookingMode === "specific") {
            return selectedGroomer?.slots ?? [];
        }

        return planner.anySuitableSlots;
    }, [bookingMode, planner, selectedGroomer]);

    const selectedSlot = useMemo(() => {
        const allSlots = bookingMode === "specific"
            ? (selectedGroomer?.slots ?? [])
            : (planner?.anySuitableSlots ?? []);
        return allSlots.find((slot) => slotKey(slot) === selectedSlotKey) ?? null;
    }, [bookingMode, planner, selectedGroomer, selectedSlotKey]);

    async function onSubmit(event: FormEvent) {
        event.preventDefault();
        if (!activePetPayload || !offerId) {
            setError("Choose a pet profile and service first.");
            return;
        }

        setBusy(true);
        setError(null);
        setMessage(null);

        let preferredTimes: Array<{ startAtUtc: string; endAtUtc: string; label: string }> = [];
        let selectionMode: string = bookingMode === "preferred" ? "PreferredWindow" : "ExactSlot";
        let preferredGroomerId: string | null = bookingMode === "specific" ? (selectedGroomerId || null) : null;

        if (bookingMode === "preferred") {
            const preferredStartAtUtc = toUtcIsoString(preferredStartLocal);
            const preferredEndAtUtc = toUtcIsoString(preferredEndLocal);
            if (!preferredStartAtUtc || !preferredEndAtUtc) {
                setBusy(false);
                setError("For preferred time mode, provide both preferred start and preferred end.");
                return;
            }

            preferredTimes = [{
                startAtUtc: preferredStartAtUtc,
                endAtUtc: preferredEndAtUtc,
                label: preferredGroomerId ? "Preferred window with selected groomer" : "Preferred window"
            }];
        } else {
            if (!selectedSlot) {
                setBusy(false);
                setError("Choose an exact slot, or switch to preferred time request mode.");
                return;
            }

            if (bookingMode === "specific" && !preferredGroomerId) {
                setBusy(false);
                setError("Choose a groomer before submitting a groomer-specific request.");
                return;
            }

            preferredTimes = [{
                startAtUtc: selectedSlot.startAtUtc,
                endAtUtc: selectedSlot.endAtUtc,
                label: bookingMode === "specific" ? "Requested exact slot with preferred groomer" : "Requested exact slot"
            }];
        }

        try {
            const response = await apiRequest<BookingRequestDetail>("/api/public/booking-requests", {
                method: "POST",
                body: JSON.stringify({
                    pet: activePetPayload,
                    requester: requester.displayName || requester.phone || requester.instagramHandle || requester.email
                        ? requester
                        : null,
                    preferredGroomerId,
                    selectionMode,
                    notes,
                    preferredTimes,
                    items: [{ offerId }]
                })
            });

            setMessage(`Booking request submitted: ${response.id}`);
            setNotes("");
            setSelectedSlotKey("");
            setPreferredStartLocal("");
            setPreferredEndLocal("");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Unable to submit booking request.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <section className="space-y-6">
            <div className="space-y-3">
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Tailbook booking</p>
                <h1 className="text-3xl font-semibold">{isPortalVariant ? "Portal-assisted booking" : "Book first. Account optional."}</h1>
                <p className="max-w-3xl text-sm text-slate-400">
                    Choose a service, describe the pet, review groomers and live slots on one screen, then leave a request.
                    The salon confirms the details before the appointment is finalized.
                </p>
                {authenticated ? (
                    <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-4 text-sm text-emerald-100">
                        You are signed in. You can reuse saved pets, and your portal history remains available in
                        <Link href="/appointments" className="ml-1 underline">appointments</Link>
                        <span> and </span>
                        <Link href="/pets" className="underline">pets</Link>.
                    </div>
                ) : (
                    <div className="rounded-2xl border border-slate-800 bg-slate-900/60 p-4 text-sm text-slate-300">
                        No registration is required for the first request.
                        <Link href="/login" className="ml-1 text-emerald-300 underline">Sign in</Link>
                        <span> to reuse pets and see visit history, or </span>
                        <Link href="/register" className="text-emerald-300 underline">create an account later</Link>.
                    </div>
                )}
            </div>

            <form className="space-y-6" onSubmit={onSubmit}>
                <Card className="space-y-4">
                    <div>
                        <h2 className="text-xl font-semibold">1. Pet and service</h2>
                        <p className="mt-1 text-sm text-slate-400">Pick a saved pet or describe one ad hoc for this request.</p>
                    </div>

                    {authenticated && pets.length > 0 ? (
                        <div className="flex flex-wrap gap-2">
                            <button type="button" onClick={() => setPetMode("saved")} className={`rounded-full px-3 py-2 text-sm ${petMode === "saved" ? "bg-emerald-500/20 text-emerald-200" : "bg-slate-950 text-slate-300"}`}>
                                Use saved pet
                            </button>
                            <button type="button" onClick={() => setPetMode("custom")} className={`rounded-full px-3 py-2 text-sm ${petMode === "custom" ? "bg-emerald-500/20 text-emerald-200" : "bg-slate-950 text-slate-300"}`}>
                                Describe another pet
                            </button>
                        </div>
                    ) : null}

                    {petMode === "saved" && pets.length > 0 ? (
                        <label className="grid gap-2 text-sm text-slate-300">
                            Saved pet
                            <select value={selectedPetId} onChange={(event) => setSelectedPetId(event.target.value)} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm">
                                {pets.map((pet) => <option key={pet.id} value={pet.id}>{pet.name} · {pet.breedName}</option>)}
                            </select>
                        </label>
                    ) : (
                        <div className="grid gap-4 md:grid-cols-2">
                            <label className="grid gap-2 text-sm text-slate-300">
                                Animal type
                                <select value={petForm.animalTypeId} onChange={(event) => setPetForm((current) => ({ ...current, animalTypeId: event.target.value }))} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm">
                                    <option value="">Select animal type</option>
                                    {catalog?.animalTypes.map((animalType) => <option key={animalType.id} value={animalType.id}>{animalType.name}</option>)}
                                </select>
                            </label>
                            <label className="grid gap-2 text-sm text-slate-300">
                                Breed
                                <select value={petForm.breedId} onChange={(event) => setPetForm((current) => ({ ...current, breedId: event.target.value }))} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm" disabled={!petForm.animalTypeId}>
                                    <option value="">Select breed</option>
                                    {visibleBreeds.map((breed) => <option key={breed.id} value={breed.id}>{breed.name}</option>)}
                                </select>
                            </label>
                            <label className="grid gap-2 text-sm text-slate-300">
                                Coat type
                                <select value={petForm.coatTypeId} onChange={(event) => setPetForm((current) => ({ ...current, coatTypeId: event.target.value }))} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm" disabled={!selectedBreed}>
                                    <option value="">Not sure / let staff confirm</option>
                                    {visibleCoatTypes.map((coatType) => <option key={coatType.id} value={coatType.id}>{coatType.name}</option>)}
                                </select>
                            </label>
                            <label className="grid gap-2 text-sm text-slate-300">
                                Size category
                                <select value={petForm.sizeCategoryId} onChange={(event) => setPetForm((current) => ({ ...current, sizeCategoryId: event.target.value }))} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm" disabled={!selectedBreed}>
                                    <option value="">Not sure / let staff estimate</option>
                                    {visibleSizeCategories.map((sizeCategory) => <option key={sizeCategory.id} value={sizeCategory.id}>{sizeCategory.name}</option>)}
                                </select>
                            </label>
                            <label className="grid gap-2 text-sm text-slate-300">
                                Pet name
                                <Input value={petForm.petName} onChange={(event) => setPetForm((current) => ({ ...current, petName: event.target.value }))} placeholder="Optional, but helpful" />
                            </label>
                            <label className="grid gap-2 text-sm text-slate-300">
                                Weight (kg)
                                <Input value={petForm.weightKg} onChange={(event) => setPetForm((current) => ({ ...current, weightKg: event.target.value }))} inputMode="decimal" placeholder="Optional" />
                            </label>
                            <label className="grid gap-2 text-sm text-slate-300 md:col-span-2">
                                Pet notes
                                <Textarea value={petForm.notes} onChange={(event) => setPetForm((current) => ({ ...current, notes: event.target.value }))} rows={3} placeholder="Temperament, matting, medical notes, handling concerns" />
                            </label>
                        </div>
                    )}

                    <label className="grid gap-2 text-sm text-slate-300">
                        Service / package
                        <select value={offerId} onChange={(event) => setOfferId(event.target.value)} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm" disabled={offersLoading || offers.length === 0}>
                            <option value="">Select service</option>
                            {offers.map((offer) => <option key={offer.id} value={offer.id}>{offer.displayName}</option>)}
                        </select>
                    </label>

                    {selectedOffer ? (
                        <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-300">
                            <div className="flex flex-wrap items-start justify-between gap-4">
                                <div>
                                    <p className="font-medium text-white">{selectedOffer.displayName}</p>
                                    <p className="mt-1 text-slate-400">{selectedOffer.offerType} · Typical reserved slot {selectedOffer.reservedMinutes} min</p>
                                </div>
                                <div className="text-right">
                                    <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Estimate</p>
                                    <p className="mt-1 text-lg font-semibold text-emerald-300">{selectedOffer.priceAmount} {selectedOffer.currency}</p>
                                </div>
                            </div>
                        </div>
                    ) : null}
                </Card>

                <Card className="space-y-4">
                    <div>
                        <h2 className="text-xl font-semibold">2. Groomer and time — one coordinated view</h2>
                        <p className="mt-1 text-sm text-slate-400">Switch between any suitable groomer, a specific groomer, or a preferred window request.</p>
                    </div>

                    <div className="flex flex-wrap gap-2">
                        <button type="button" onClick={() => { setBookingMode("any"); setSelectedGroomerId(""); }} className={`rounded-full px-3 py-2 text-sm ${bookingMode === "any" ? "bg-emerald-500/20 text-emerald-200" : "bg-slate-950 text-slate-300"}`}>
                            Any suitable groomer
                        </button>
                        <button type="button" onClick={() => setBookingMode("specific")} className={`rounded-full px-3 py-2 text-sm ${bookingMode === "specific" ? "bg-emerald-500/20 text-emerald-200" : "bg-slate-950 text-slate-300"}`}>
                            Specific groomer
                        </button>
                        <button type="button" onClick={() => setBookingMode("preferred")} className={`rounded-full px-3 py-2 text-sm ${bookingMode === "preferred" ? "bg-emerald-500/20 text-emerald-200" : "bg-slate-950 text-slate-300"}`}>
                            Preferred window only
                        </button>
                    </div>

                    <label className="grid gap-2 text-sm text-slate-300 md:max-w-xs">
                        Preferred day
                        <Input value={selectedDate} onChange={(event) => setSelectedDate(event.target.value)} type="date" />
                    </label>

                    {planner?.quote ? (
                        <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-300">
                            <div className="flex flex-wrap items-start justify-between gap-4">
                                <div>
                                    <p className="font-medium text-white">Current quote preview</p>
                                    <div className="mt-2 space-y-1 text-slate-400">
                                        {planner.quote.priceLines.map((line, index) => <p key={`${line.label}-${index}`}>{line.label}: {line.amount} {planner.quote.currency}</p>)}
                                    </div>
                                </div>
                                <div className="text-right">
                                    <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Reserved time</p>
                                    <p className="mt-1 text-lg font-semibold text-emerald-300">{planner.quote.reservedMinutes} min</p>
                                </div>
                            </div>
                        </div>
                    ) : null}

                    <div className="grid gap-4 xl:grid-cols-[1.1fr_1.4fr]">
                        <div className="space-y-3">
                            <p className="text-sm font-medium text-slate-200">Available groomers</p>
                            <div className="grid gap-3">
                                {planner?.groomers.map((groomer) => {
                                    const selected = selectedGroomerId === groomer.groomerId;
                                    return (
                                        <button
                                            key={groomer.groomerId}
                                            type="button"
                                            onClick={() => {
                                                setSelectedGroomerId(groomer.groomerId);
                                                if (bookingMode !== "specific") {
                                                    setBookingMode("specific");
                                                }
                                            }}
                                            className={`rounded-2xl border p-4 text-left ${selected ? "border-emerald-500/50 bg-emerald-500/10" : "border-slate-800 bg-slate-950/60"}`}
                                        >
                                            <div className="flex items-start justify-between gap-3">
                                                <div>
                                                    <p className="font-medium text-white">{groomer.displayName}</p>
                                                    <p className="mt-1 text-xs text-slate-500">Slot length {groomer.reservedMinutes} min</p>
                                                </div>
                                                <span className={`rounded-full px-2 py-1 text-xs ${groomer.canTakeRequest ? "bg-emerald-500/15 text-emerald-200" : "bg-amber-500/15 text-amber-200"}`}>
                                                    {groomer.slots.length} slots
                                                </span>
                                            </div>
                                            {groomer.reasons.length > 0 ? <p className="mt-2 text-xs text-slate-400">{groomer.reasons[0]}</p> : null}
                                        </button>
                                    );
                                })}
                            </div>
                        </div>

                        <div className="space-y-3">
                            <div className="flex items-center justify-between gap-3">
                                <p className="text-sm font-medium text-slate-200">Exact slots</p>
                                {plannerLoading ? <span className="text-xs text-slate-500">Refreshing…</span> : null}
                            </div>
                            <div className="grid gap-3 sm:grid-cols-2">
                                {visibleSlots.map((slot) => {
                                    const selected = slotKey(slot) === selectedSlotKey;
                                    return (
                                        <button
                                            key={slotKey(slot)}
                                            type="button"
                                            onClick={() => {
                                                setSelectedSlotKey(slotKey(slot));
                                                setPreferredStartLocal(toLocalDateTimeValue(slot.startAtUtc));
                                                setPreferredEndLocal(toLocalDateTimeValue(slot.endAtUtc));
                                            }}
                                            className={`rounded-2xl border p-4 text-left ${selected ? "border-emerald-500/50 bg-emerald-500/10" : "border-slate-800 bg-slate-950/60"}`}
                                        >
                                            <p className="font-medium text-white">{formatSlotRange(slot.startAtUtc, slot.endAtUtc)}</p>
                                            <p className="mt-1 text-xs text-slate-400">
                                                {bookingMode === "specific" ? "Exact slot with selected groomer" : `${slot.groomerIds.length} suitable groomer(s)`}
                                            </p>
                                        </button>
                                    );
                                })}
                            </div>
                            {!plannerLoading && visibleSlots.length === 0 ? (
                                <div className="rounded-2xl border border-amber-500/20 bg-amber-500/10 p-4 text-sm text-amber-100">
                                    No exact slots are available for this configuration on the selected date. Use preferred window mode below and the salon will follow up.
                                </div>
                            ) : null}
                        </div>
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                        <label className="grid gap-2 text-sm text-slate-300">
                            Preferred start (optional fallback)
                            <Input value={preferredStartLocal} onChange={(event) => setPreferredStartLocal(event.target.value)} type="datetime-local" />
                        </label>
                        <label className="grid gap-2 text-sm text-slate-300">
                            Preferred end (optional fallback)
                            <Input value={preferredEndLocal} onChange={(event) => setPreferredEndLocal(event.target.value)} type="datetime-local" />
                        </label>
                    </div>
                </Card>

                <Card className="space-y-4">
                    <div>
                        <h2 className="text-xl font-semibold">3. Contact and notes</h2>
                        <p className="mt-1 text-sm text-slate-400">Capture contact details late in the flow, after booking intent is already clear.</p>
                    </div>

                    <div className="grid gap-4 md:grid-cols-2">
                        <label className="grid gap-2 text-sm text-slate-300">
                            Your name
                            <Input value={requester.displayName} onChange={(event) => setRequester((current) => ({ ...current, displayName: event.target.value }))} placeholder="Who should the salon contact?" />
                        </label>
                        <label className="grid gap-2 text-sm text-slate-300">
                            Phone
                            <Input value={requester.phone} onChange={(event) => setRequester((current) => ({ ...current, phone: event.target.value }))} placeholder="Strongly recommended" />
                        </label>
                        <label className="grid gap-2 text-sm text-slate-300">
                            Instagram
                            <Input value={requester.instagramHandle} onChange={(event) => setRequester((current) => ({ ...current, instagramHandle: event.target.value }))} placeholder="@handle" />
                        </label>
                        <label className="grid gap-2 text-sm text-slate-300">
                            Email
                            <Input value={requester.email} onChange={(event) => setRequester((current) => ({ ...current, email: event.target.value }))} placeholder="Optional" type="email" />
                        </label>
                    </div>

                    <label className="grid gap-2 text-sm text-slate-300 md:max-w-sm">
                        Preferred contact method
                        <select value={requester.preferredContactMethodCode} onChange={(event) => setRequester((current) => ({ ...current, preferredContactMethodCode: event.target.value }))} className="rounded-xl border border-slate-800 bg-slate-950 px-3 py-2 text-sm">
                            <option value="Phone">Phone</option>
                            <option value="Instagram">Instagram</option>
                            <option value="Email">Email</option>
                        </select>
                    </label>

                    <label className="grid gap-2 text-sm text-slate-300">
                        Additional comments
                        <Textarea value={notes} onChange={(event) => setNotes(event.target.value)} rows={4} placeholder="Anything staff should know before they call you back" />
                    </label>

                    {offersLoading ? <p className="text-sm text-slate-400">Loading available services…</p> : null}
                    {plannerLoading ? <p className="text-sm text-slate-400">Calculating live availability…</p> : null}
                    {message ? <p className="text-sm text-emerald-300">{message}</p> : null}
                    {error ? <p className="text-sm text-rose-300">{error}</p> : null}

                    <div className="flex flex-wrap items-center justify-between gap-3">
                        <p className="text-xs text-slate-500">
                            Submission creates a booking request for staff review. The final appointment is confirmed by the salon after follow-up.
                        </p>
                        <Button type="submit" disabled={busy || !activePetPayload || !offerId}>
                            {busy ? "Submitting…" : "Submit booking request"}
                        </Button>
                    </div>
                </Card>
            </form>
        </section>
    );
}

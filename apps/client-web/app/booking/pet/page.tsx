"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import type { CatalogBreed, ClientMeResponse, ClientPetSummary, PetCatalog } from "@/lib/types";
import { BottomMobileCTA } from "@/components/bottom-mobile-cta";
import { ClientHeader } from "@/components/client-header";
import { ProgressSteps } from "@/components/progress-steps";
import { ArrowLeftIcon, ArrowRightIcon, PawIcon, UploadIcon } from "@/components/icons";
import { Button, Card, Checkbox, Input, Label, NativeSelect, Textarea } from "@/components/ui";
import { useClientBooking } from "@/lib/client-booking-context";
import { mapPetSummaryToBookingPet } from "@/lib/booking-helpers";

function inferAnimalTypeCode(breed: CatalogBreed | null, catalog: PetCatalog | null) {
  if (!breed || !catalog) return undefined;
  return catalog.animalTypes.find((item) => item.id === breed.animalTypeId)?.code;
}

export default function BookingPetPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { booking, patchBooking } = useClientBooking();
  const [catalog, setCatalog] = useState<PetCatalog | null>(null);
  const [pets, setPets] = useState<ClientPetSummary[]>([]);
  const [mode, setMode] = useState<"saved" | "custom">(booking.pet?.source === "saved" ? "saved" : "custom");
  const [selectedPetId, setSelectedPetId] = useState(booking.pet?.savedPetId ?? searchParams.get("pet") ?? "");
  const [petName, setPetName] = useState(booking.pet?.source === "custom" ? booking.pet.name : "");
  const [animalTypeId, setAnimalTypeId] = useState(booking.pet?.source === "custom" ? booking.pet.animalTypeId ?? "" : "");
  const [breedId, setBreedId] = useState(booking.pet?.source === "custom" ? booking.pet.breedId ?? "" : "");
  const [coatTypeId, setCoatTypeId] = useState(booking.pet?.source === "custom" ? booking.pet.coatTypeId ?? "" : "");
  const [sizeCategoryId, setSizeCategoryId] = useState(booking.pet?.source === "custom" ? booking.pet.sizeCategoryId ?? "" : "");
  const [mixedBreed, setMixedBreed] = useState(Boolean(booking.pet?.mixedBreed));
  const [weightKg, setWeightKg] = useState(booking.pet?.source === "custom" && booking.pet.weightKg ? String(booking.pet.weightKg) : "");
  const [notes, setNotes] = useState(booking.pet?.notes ?? "");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const loadedCatalog = await apiRequest<PetCatalog>("/api/public/pets/catalog");
        if (cancelled) return;
        setCatalog(loadedCatalog);
      } catch (err) {
        if (!cancelled) setError(err instanceof ApiError ? err.message : "Не вдалося завантажити каталог вихованців.");
      }
      try {
        const me = await apiRequest<ClientMeResponse>("/api/client/me");
        if (cancelled || !me) return;
        const myPets = await apiRequest<ClientPetSummary[]>("/api/client/me/pets");
        if (cancelled) return;
        setPets(myPets);
        if ((searchParams.get("pet") || booking.pet?.savedPetId) && myPets.some((item) => item.id === (searchParams.get("pet") || booking.pet?.savedPetId))) {
          setMode("saved");
          setSelectedPetId(searchParams.get("pet") || booking.pet?.savedPetId || "");
        }
      } catch {
        // guest flow is allowed
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [booking.pet?.savedPetId, searchParams]);

  const availableBreeds = useMemo(() => catalog?.breeds.filter((breed) => !animalTypeId || breed.animalTypeId === animalTypeId) ?? [], [catalog, animalTypeId]);
  const activeBreed = useMemo(() => availableBreeds.find((breed) => breed.id === breedId) ?? null, [availableBreeds, breedId]);
  const availableCoats = useMemo(() => {
    if (!catalog) return [];
    if (activeBreed?.allowedCoatTypeIds?.length) return catalog.coatTypes.filter((item) => activeBreed.allowedCoatTypeIds.includes(item.id));
    return catalog.coatTypes.filter((item) => !animalTypeId || !item.animalTypeId || item.animalTypeId === animalTypeId);
  }, [activeBreed, animalTypeId, catalog]);
  const availableSizes = useMemo(() => {
    if (!catalog) return [];
    if (activeBreed?.allowedSizeCategoryIds?.length) return catalog.sizeCategories.filter((item) => activeBreed.allowedSizeCategoryIds.includes(item.id));
    return catalog.sizeCategories.filter((item) => !animalTypeId || !item.animalTypeId || item.animalTypeId === animalTypeId);
  }, [activeBreed, animalTypeId, catalog]);

  useEffect(() => {
    if (availableCoats.length === 1) setCoatTypeId((current) => current || availableCoats[0].id);
  }, [availableCoats]);
  useEffect(() => {
    if (availableSizes.length === 1) setSizeCategoryId((current) => current || availableSizes[0].id);
  }, [availableSizes]);

  function handleContinue() {
    if (mode === "saved") {
      const pet = pets.find((item) => item.id === selectedPetId);
      if (!pet) {
        setError("Оберіть вихованця з профілю або перемкніться на ручне заповнення.");
        return;
      }
      patchBooking({ pet: mapPetSummaryToBookingPet(pet), quote: null, planner: null, resolvedOffers: [], preferredDate: booking.preferredDate, preferredSlot: booking.preferredSlot });
      router.push("/booking/schedule");
      return;
    }

    if (!petName || !animalTypeId) {
      setError("Для продовження вкажіть тип та ім'я вихованця.");
      return;
    }

    const animalTypeCode = catalog?.animalTypes.find((item) => item.id === animalTypeId)?.code;
    const breed = catalog?.breeds.find((item) => item.id === breedId) ?? null;
    const coat = catalog?.coatTypes.find((item) => item.id === coatTypeId);
    const size = catalog?.sizeCategories.find((item) => item.id === sizeCategoryId);

    patchBooking({
      pet: {
        source: "custom",
        name: petName,
        animalTypeId,
        animalTypeCode: animalTypeCode ?? inferAnimalTypeCode(breed, catalog),
        breedId: breed?.id,
        breedName: mixedBreed ? `${breed?.name ?? ""} / метис`.trim() : breed?.name,
        coatTypeId: coat?.id,
        coatTypeName: coat?.name,
        sizeCategoryId: size?.id,
        sizeCategoryName: size?.name,
        weightKg: weightKg ? Number(weightKg) : null,
        notes,
        mixedBreed
      },
      quote: null,
      planner: null,
      resolvedOffers: []
    });
    router.push("/booking/schedule");
  }

  const isValid = mode === "saved" ? Boolean(selectedPetId) : Boolean(petName && animalTypeId);

  return (
    <div className="min-h-screen bg-background pb-24 lg:pb-8">
      <ClientHeader />
      <div className="container py-8">
        <ProgressSteps steps={[{ label: "Послуги", completed: true }, { label: "Вихованець", active: true }, { label: "Майстер та час" }, { label: "Контакти" }]} />
        <div className="mx-auto max-w-2xl">
          <div className="mb-8"><h1 className="mb-2 text-3xl font-bold">Розкажіть про вихованця</h1><p className="text-muted-foreground">Це допоможе нам підготуватися та запропонувати найкращий догляд</p></div>

          {pets.length > 0 ? (
            <div className="mb-6 grid grid-cols-2 rounded-xl bg-secondary p-1">
              <button type="button" onClick={() => setMode("saved")} className={mode === "saved" ? "rounded-lg bg-card px-4 py-2 text-sm font-medium shadow-sm" : "rounded-lg px-4 py-2 text-sm text-muted-foreground"}>Мій вихованець</button>
              <button type="button" onClick={() => setMode("custom")} className={mode === "custom" ? "rounded-lg bg-card px-4 py-2 text-sm font-medium shadow-sm" : "rounded-lg px-4 py-2 text-sm text-muted-foreground"}>Новий профіль</button>
            </div>
          ) : null}

          <div className="space-y-6">
            {mode === "saved" ? (
              <div className="space-y-4">
                {pets.map((pet) => (
                  <Card key={pet.id} className={selectedPetId === pet.id ? "cursor-pointer border-primary bg-accent/30 p-4 ring-2 ring-primary" : "cursor-pointer p-4"} onClick={() => setSelectedPetId(pet.id)}>
                    <div className="flex items-start gap-3"><div className="flex h-12 w-12 items-center justify-center rounded-full bg-accent"><PawIcon className="h-6 w-6 text-primary" /></div><div><h3 className="font-medium">{pet.name}</h3><p className="text-sm text-muted-foreground">{pet.breedName}</p><p className="text-sm text-muted-foreground">{pet.animalTypeName}</p></div></div>
                  </Card>
                ))}
              </div>
            ) : (
              <>
                <div>
                  <Label className="mb-3 block">Тип вихованця *</Label>
                  <div className="grid grid-cols-2 gap-4">
                    {(catalog?.animalTypes ?? []).map((item) => (
                      <button key={item.id} type="button" onClick={() => { setAnimalTypeId(item.id); setBreedId(""); setCoatTypeId(""); setSizeCategoryId(""); }} className={animalTypeId === item.id ? "rounded-lg border-2 border-primary bg-accent p-4 text-center" : "rounded-lg border-2 border-border p-4 text-center"}>
                        <span className="mb-2 block text-2xl">{item.code === "DOG" ? "🐕" : "🐈"}</span>
                        <span className="font-medium">{item.name}</span>
                      </button>
                    ))}
                  </div>
                </div>

                <div><Label htmlFor="petName">Ім'я вихованця *</Label><Input id="petName" value={petName} onChange={(event) => setPetName(event.target.value)} placeholder="Наприклад, Джек" className="mt-2" /></div>

                <div>
                  <Label htmlFor="breed">Порода</Label>
                  <NativeSelect id="breed" value={breedId} onChange={(event) => setBreedId(event.target.value)} className="mt-2">
                    <option value="">Оберіть породу</option>
                    {availableBreeds.map((breed) => <option key={breed.id} value={breed.id}>{breed.name}</option>)}
                  </NativeSelect>
                  <div className="mt-2 flex items-center gap-2"><Checkbox id="mixedBreed" checked={mixedBreed} onChange={(event) => setMixedBreed(event.target.checked)} /><Label htmlFor="mixedBreed" className="cursor-pointer text-sm font-normal">Метис / змішана порода</Label></div>
                </div>

                <div>
                  <Label htmlFor="coatType">Тип шерсті</Label>
                  <NativeSelect id="coatType" value={coatTypeId} onChange={(event) => setCoatTypeId(event.target.value)} className="mt-2">
                    <option value="">Оберіть тип шерсті</option>
                    {availableCoats.map((coat) => <option key={coat.id} value={coat.id}>{coat.name}</option>)}
                  </NativeSelect>
                </div>

                <div>
                  <Label htmlFor="sizeCategory">Розмір</Label>
                  <NativeSelect id="sizeCategory" value={sizeCategoryId} onChange={(event) => setSizeCategoryId(event.target.value)} className="mt-2">
                    <option value="">Оберіть розмір</option>
                    {availableSizes.map((size) => <option key={size.id} value={size.id}>{size.name}</option>)}
                  </NativeSelect>
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div><Label htmlFor="weightKg">Вага (необов'язково)</Label><Input id="weightKg" value={weightKg} onChange={(event) => setWeightKg(event.target.value)} placeholder="Наприклад, 15" className="mt-2" /></div>
                  <div><Label htmlFor="notes">Особливості поведінки</Label><Textarea id="notes" value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Наприклад, боїться гучних звуків…" className="mt-2 min-h-12" /></div>
                </div>

                <div>
                  <Label>Фото вихованця (необов'язково)</Label>
                  <div className="mt-2 rounded-lg border-2 border-dashed border-border p-8 text-center"><UploadIcon className="mx-auto mb-2 h-8 w-8 text-muted-foreground" /><p className="text-sm text-muted-foreground">Фото у поточному потоці не обов'язкове і не блокує запис.</p></div>
                </div>
              </>
            )}
          </div>

          {error ? <div className="mt-6 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div> : null}

          <div className="mt-8 hidden justify-between lg:flex">
            <Link href="/booking/services"><Button variant="outline"><ArrowLeftIcon className="h-4 w-4" /> Назад</Button></Link>
            <Button onClick={handleContinue} disabled={!isValid} size="lg">Продовжити <ArrowRightIcon className="h-4 w-4" /></Button>
          </div>
        </div>
      </div>
      <BottomMobileCTA onClick={handleContinue} disabled={!isValid}>Продовжити <ArrowRightIcon className="h-4 w-4" /></BottomMobileCTA>
    </div>
  );
}

"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import type { PublicBookingPlanner, PublicBookableOffer, PublicPlannerSlot } from "@/lib/types";
import { BookingSummaryPanel } from "@/components/booking-summary-panel";
import { BottomMobileCTA } from "@/components/bottom-mobile-cta";
import { ClientHeader } from "@/components/client-header";
import { GroomerCard } from "@/components/groomer-card";
import { ProgressSteps } from "@/components/progress-steps";
import { SlotButton } from "@/components/slot-button";
import { ArrowLeftIcon, ArrowRightIcon, CalendarIcon, SparklesIcon } from "@/components/icons";
import { Button, Card } from "@/components/ui";
import { useClientBooking } from "@/lib/client-booking-context";
import { extractUniqueSlots, formatDateLong, mergeGroomerCardData, resolveOffersFromTemplates, slotKey, slotLabelFromUtc, toPublicPetPayload } from "@/lib/booking-helpers";
import { groomerProfiles, serviceTemplates } from "@/lib/display-data";

function todayLocalDate() {
  return new Intl.DateTimeFormat("sv-SE", { year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date());
}

export default function BookingSchedulePage() {
  const router = useRouter();
  const { booking, patchBooking } = useClientBooking();
  const [selectedDate, setSelectedDate] = useState(booking.preferredDate ?? todayLocalDate());
  const [selectedGroomerId, setSelectedGroomerId] = useState(booking.preferredGroomerId ?? "");
  const [selectedSlotValue, setSelectedSlotValue] = useState("");
  const [planner, setPlanner] = useState<PublicBookingPlanner | null>(booking.planner);
  const [resolvedOfferIds, setResolvedOfferIds] = useState(booking.resolvedOffers);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedTemplates = useMemo(() => serviceTemplates.filter((item) => booking.selectedTemplateIds.includes(item.id)), [booking.selectedTemplateIds]);
  const activePetPayload = useMemo(() => toPublicPetPayload(booking.pet), [booking.pet]);

  useEffect(() => {
    if (!activePetPayload || selectedTemplates.length === 0) return;
    let cancelled = false;

    async function loadPlanner() {
      setLoading(true);
      setError(null);
      try {
        const offers = await apiRequest<PublicBookableOffer[]>("/api/public/booking-offers", { method: "POST", body: JSON.stringify({ pet: activePetPayload }) });
        if (cancelled) return;
        const resolved = resolveOffersFromTemplates(selectedTemplates, offers);
        if (resolved.length === 0) {
          setResolvedOfferIds([]);
          setPlanner(null);
          setError("Не вдалося знайти реальні offer'и під обраний набір послуг для цього профілю вихованця.");
          return;
        }
        setResolvedOfferIds(resolved);
        const loadedPlanner = await apiRequest<PublicBookingPlanner>("/api/public/booking-planner", {
          method: "POST",
          body: JSON.stringify({ pet: activePetPayload, localDate: selectedDate, items: resolved.map((item) => ({ offerId: item.offerId })) })
        });
        if (cancelled) return;
        setPlanner(loadedPlanner);
        setError(null);
      } catch (err) {
        if (!cancelled) {
          setPlanner(null);
          setError(err instanceof ApiError ? err.message : "Не вдалося завантажити доступні слоти.");
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void loadPlanner();
    return () => { cancelled = true; };
  }, [activePetPayload, selectedDate, selectedTemplates]);

  const visibleSlots = useMemo(() => {
    if (!planner) return [] as PublicPlannerSlot[];
    if (selectedGroomerId) {
      return planner.groomers.find((item) => item.groomerId === selectedGroomerId)?.slots ?? [];
    }
    return extractUniqueSlots(planner);
  }, [planner, selectedGroomerId]);

  const selectedSlot = useMemo(() => visibleSlots.find((slot) => slotKey(slot) === selectedSlotValue) ?? null, [selectedSlotValue, visibleSlots]);
  const selectedGroomer = useMemo(() => planner?.groomers.find((item) => item.groomerId === selectedGroomerId) ?? null, [planner, selectedGroomerId]);

  function handleContinue() {
    if (!planner || !selectedSlot || resolvedOfferIds.length === 0) {
      setError("Оберіть доступний слот, щоб продовжити.");
      return;
    }
    patchBooking({
      preferredDate: selectedDate,
      preferredSlot: slotLabelFromUtc(selectedSlot.startAtUtc, selectedSlot.endAtUtc),
      selectedSlotStartUtc: selectedSlot.startAtUtc,
      selectedSlotEndUtc: selectedSlot.endAtUtc,
      preferredGroomerId: selectedGroomerId || null,
      preferredGroomerName: selectedGroomer?.displayName ?? null,
      selectionMode: "exact",
      planner,
      quote: planner.quote,
      resolvedOffers: resolvedOfferIds
    });
    router.push("/booking/contact");
  }

  const canContinue = Boolean(selectedSlot && resolvedOfferIds.length > 0);

  return (
    <div className="min-h-screen bg-background pb-24 lg:pb-8">
      <ClientHeader />
      <div className="container py-8">
        <ProgressSteps steps={[{ label: "Послуги", completed: true }, { label: "Вихованець", completed: true }, { label: "Майстер та час", active: true }, { label: "Контакти" }]} />
        <div className="mx-auto max-w-7xl">
          <div className="mb-8"><h1 className="mb-2 text-3xl font-bold">Оберіть майстра та час</h1><p className="text-muted-foreground">Виберіть зручний час та грумера, якому довіряєте</p></div>
          <div className="grid gap-8 lg:grid-cols-3">
            <div className="space-y-8 lg:col-span-2">
              <div>
                <h2 className="mb-4 text-xl font-medium">Грумер</h2>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Card className={selectedGroomerId ? "cursor-pointer p-4" : "cursor-pointer p-4 ring-2 ring-primary shadow-md"} onClick={() => setSelectedGroomerId("")}> 
                    <div className="flex items-center gap-3"><div className="flex h-12 w-12 items-center justify-center rounded-full bg-accent"><SparklesIcon className="h-6 w-6 text-primary" /></div><div><h3 className="font-medium">Без переваги</h3><p className="text-sm text-muted-foreground">Перший вільний майстер</p></div></div>
                  </Card>
                  {(planner?.groomers ?? []).map((groomer) => (
                    <GroomerCard key={groomer.groomerId} groomer={mergeGroomerCardData(groomer, groomerProfiles)} compact selected={selectedGroomerId === groomer.groomerId} onSelect={() => setSelectedGroomerId(groomer.groomerId)} />
                  ))}
                </div>
              </div>

              <div>
                <h2 className="mb-4 text-xl font-medium">Дата</h2>
                <div className="inline-flex items-center gap-3 rounded-lg border border-border bg-card px-4 py-3 text-sm">
                  <CalendarIcon className="h-4 w-4 text-primary" />
                  <input type="date" value={selectedDate} min={todayLocalDate()} onChange={(event) => { setSelectedDate(event.target.value); setSelectedSlotValue(""); }} className="bg-transparent outline-none" />
                  <span className="text-muted-foreground">{formatDateLong(selectedDate)}</span>
                </div>
              </div>

              <div>
                <h2 className="mb-4 text-xl font-medium">Доступний час</h2>
                {loading ? <Card className="p-8 text-center text-muted-foreground">Підбираємо реальні слоти з поточного planner…</Card> : null}
                {!loading && visibleSlots.length > 0 ? <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-5">{visibleSlots.map((slot, index) => <SlotButton key={slotKey(slot)} time={slotLabelFromUtc(slot.startAtUtc, slot.endAtUtc)} available selected={selectedSlotValue === slotKey(slot)} suggested={index === 0} onSelect={() => setSelectedSlotValue(slotKey(slot))} />)}</div> : null}
                {!loading && visibleSlots.length === 0 ? <Card className="p-8 text-center text-muted-foreground">Наразі немає доступних слотів на обрану дату. Спробуйте іншу дату або залиште заявку пізніше.</Card> : null}
              </div>

              {selectedGroomer ? <div className="border-t border-border pt-6"><h3 className="mb-4 font-medium">Про обраного майстра</h3><GroomerCard groomer={mergeGroomerCardData(selectedGroomer, groomerProfiles)} /></div> : null}
              {planner?.quote ? <Card className="p-5 text-sm text-muted-foreground"><p className="font-medium text-foreground">Реальний quote під профіль вже розрахований.</p><div className="mt-2 space-y-2">{planner.quote.priceLines.map((line) => <div key={line.label} className="flex justify-between gap-4"><span>{line.label}</span><span>{line.amount.toLocaleString("uk-UA")} грн</span></div>)}</div></Card> : null}
              {error ? <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div> : null}
              <div className="hidden justify-between pt-6 lg:flex"><Link href="/booking/pet"><Button variant="outline"><ArrowLeftIcon className="h-4 w-4" /> Назад</Button></Link><Button onClick={handleContinue} disabled={!canContinue} size="lg">Продовжити <ArrowRightIcon className="h-4 w-4" /></Button></div>
            </div>
            <div className="hidden lg:block"><BookingSummaryPanel booking={{ ...booking, planner, quote: planner?.quote ?? booking.quote, preferredDate: selectedDate, preferredSlot: selectedSlot ? slotLabelFromUtc(selectedSlot.startAtUtc, selectedSlot.endAtUtc) : booking.preferredSlot, preferredGroomerId: selectedGroomerId || null }} /></div>
          </div>
        </div>
      </div>
      <BottomMobileCTA onClick={handleContinue} disabled={!canContinue}>Продовжити <ArrowRightIcon className="h-4 w-4" /></BottomMobileCTA>
    </div>
  );
}

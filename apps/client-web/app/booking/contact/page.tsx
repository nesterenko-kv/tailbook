"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import type { BookingRequestDetail, ClientContactPreferences, ClientMeResponse, PublicBookableOffer } from "@/lib/types";
import { BookingSummaryPanel } from "@/components/booking-summary-panel";
import { BottomMobileCTA } from "@/components/bottom-mobile-cta";
import { ClientHeader } from "@/components/client-header";
import { ProgressSteps } from "@/components/progress-steps";
import { ArrowLeftIcon, ArrowRightIcon } from "@/components/icons";
import { Button, Checkbox, Input, Label, Textarea } from "@/components/ui";
import { useClientBooking } from "@/lib/client-booking-context";
import { buildPreferredTimes, resolveOffersFromTemplates, toPublicPetPayload } from "@/lib/booking-helpers";
import { generateIdempotencyKey } from "@/lib/idempotency-key";
import { validateInput, type FieldErrors } from "@/lib/validation-helpers";
import { serviceTemplates } from "@/lib/display-data";

export default function BookingContactPage() {
  const router = useRouter();
  const { booking, patchBooking } = useClientBooking();
  const [fullName, setFullName] = useState(booking.contact.fullName);
  const [phone, setPhone] = useState(booking.contact.phone);
  const [instagram, setInstagram] = useState(booking.contact.instagram);
  const [comments, setComments] = useState(booking.contact.comments);
  const [createAccount, setCreateAccount] = useState(booking.contact.createAccount);
  const [consent, setConsent] = useState(booking.contact.consent);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const submitInFlightRef = useRef(false);

  useEffect(() => {
    let cancelled = false;
    async function prefill() {
      try {
        const me = await apiRequest<ClientMeResponse>("/api/client/me");
        if (cancelled) return;
        setFullName((current) => current || me.displayName);
        try {
          const preferences = await apiRequest<ClientContactPreferences>("/api/client/me/contact-preferences");
          if (cancelled) return;
          const phoneMethod = preferences.methods.find((item) => item.methodType.toLowerCase() === "phone");
          const instagramMethod = preferences.methods.find((item) => item.methodType.toLowerCase().includes("instagram"));
          setPhone((current) => current || phoneMethod?.displayValue || "");
          setInstagram((current) => current || instagramMethod?.displayValue || "");
        } catch {
          // optional
        }
      } catch {
        // guest flow
      }
    }
    void prefill();
    return () => { cancelled = true; };
  }, []);

  const canSubmit = useMemo(() => Boolean(fullName && consent && (phone || instagram) && booking.pet && booking.selectedTemplateIds.length > 0), [booking.pet, booking.selectedTemplateIds.length, consent, fullName, instagram, phone]);

  function validate(): boolean {
    const errors: FieldErrors = {};
    const nameResult = validateInput({ value: fullName, required: true, label: "ім'я" });
    if (nameResult) errors.fullName = nameResult;
    if (!phone && !instagram) {
      errors.phone = { message: "Вкажіть принаймні один контактний спосіб: телефон або Instagram." };
    } else if (phone) {
      const phoneResult = validateInput({ value: phone, pattern: /^\+38\d{10}$/, label: "номер телефону" });
      if (phoneResult) errors.phone = phoneResult;
    }
    if (!consent) {
      errors.consent = { message: "Потрібна згода на обробку персональних даних." };
    }
    if (!booking.pet) {
      errors.petName = { message: "Дані про вихованця не заповнені." };
    }
    if (booking.selectedTemplateIds.length === 0) {
      errors.fullName = errors.fullName
        ? { message: errors.fullName.message + " Оберіть хоча б одну послугу." }
        : { message: "Оберіть хоча б одну послугу." };
    }
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }

  async function handleSubmit(event?: FormEvent) {
    event?.preventDefault();
    if (busy || submitInFlightRef.current) return;
    if (!validate()) return;
    const petPayload = toPublicPetPayload(booking.pet);
    if (!petPayload) {
      setError("Спочатку заповніть дані про вихованця.");
      return;
    }

    submitInFlightRef.current = true;
    setBusy(true);
    setError(null);
    const idempotencyKey = generateIdempotencyKey();
    try {
      let resolvedOffers = booking.resolvedOffers;
      if (resolvedOffers.length === 0) {
        const availableOffers = await apiRequest<PublicBookableOffer[]>("/api/public/booking-offers", { method: "POST", body: JSON.stringify({ pet: petPayload }) });
        const templates = serviceTemplates.filter((item) => booking.selectedTemplateIds.includes(item.id));
        resolvedOffers = resolveOffersFromTemplates(templates, availableOffers);
      }
      if (resolvedOffers.length === 0) {
        setError("Не вдалося зібрати коректний список real offer'ів для заявки.");
        setBusy(false);
        submitInFlightRef.current = false;
        return;
      }

      const preferredTimes = booking.selectedSlotStart && booking.selectedSlotEnd
        ? [{ startAt: booking.selectedSlotStart, endAt: booking.selectedSlotEnd, label: booking.preferredSlot ?? "Requested exact slot" }]
        : (booking.planner?.anySuitableSlots?.[0] ? buildPreferredTimes(booking.planner.anySuitableSlots[0]) : []);

      const response = await apiRequest<BookingRequestDetail>("/api/public/booking-requests", {
        method: "POST",
        headers: {
          "Idempotency-Key": idempotencyKey
        },
        body: JSON.stringify({
          pet: petPayload,
          requester: {
            displayName: fullName,
            phone: phone || null,
            instagramHandle: instagram || null,
            email: null,
            preferredContactMethodCode: phone ? "Phone" : "Instagram"
          },
          preferredGroomerId: booking.preferredGroomerId,
          selectionMode: booking.selectionMode === "preferred_window" ? "PreferredWindow" : "ExactSlot",
          notes: comments || booking.contact.comments || null,
          preferredTimes,
          items: resolvedOffers.map((item) => ({ offerId: item.offerId }))
        })
      });

      patchBooking({
        contact: { fullName, phone, instagram, comments, createAccount, consent },
        resolvedOffers,
        lastCreatedRequestId: response.id
      });
      router.push("/booking/confirmation");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Не вдалося відправити booking request.");
    } finally {
      submitInFlightRef.current = false;
      setBusy(false);
    }
  }

  return (
    <div className="min-h-screen bg-background pb-24 lg:pb-8">
      <ClientHeader />
      <div className="container py-8">
        <ProgressSteps steps={[{ label: "Послуги", completed: true }, { label: "Вихованець", completed: true }, { label: "Майстер та час", completed: true }, { label: "Контакти", active: true }]} />
        <div className="mx-auto max-w-7xl">
          <div className="mb-8"><h1 className="mb-2 text-3xl font-bold">Залиште контакти</h1><p className="text-muted-foreground">Ми використаємо їх лише для підтвердження вашого запису</p></div>
          <div className="grid gap-8 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <form onSubmit={handleSubmit} noValidate className="space-y-6">
                <div aria-invalid={fieldErrors.fullName ? "true" : undefined} aria-describedby={fieldErrors.fullName ? "fullName-error" : undefined}>
                  <Label htmlFor="fullName">Ваше ім'я *</Label>
                  <Input id="fullName" value={fullName} onChange={(event) => { setFullName(event.target.value); setFieldErrors((prev) => ({ ...prev, fullName: undefined })); }} placeholder="Наприклад, Анна" className="mt-2" />
                  {fieldErrors.fullName ? <p id="fullName-error" className="mt-1 text-sm text-red-600" role="alert">{fieldErrors.fullName.message}</p> : null}
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div aria-invalid={fieldErrors.phone ? "true" : undefined} aria-describedby={fieldErrors.phone ? "phone-error" : undefined}>
                    <Label htmlFor="phone">Телефон *</Label>
                    <Input id="phone" value={phone} onChange={(event) => { setPhone(event.target.value); setFieldErrors((prev) => ({ ...prev, phone: undefined })); }} placeholder="+380…" className="mt-2" />
                    {fieldErrors.phone ? <p id="phone-error" className="mt-1 text-sm text-red-600" role="alert">{fieldErrors.phone.message}</p> : null}
                  </div>
                  <div aria-invalid={fieldErrors.instagram ? "true" : undefined} aria-describedby={fieldErrors.instagram ? "instagram-error" : undefined}>
                    <Label htmlFor="instagram">Instagram</Label>
                    <Input id="instagram" value={instagram} onChange={(event) => { setInstagram(event.target.value); setFieldErrors((prev) => ({ ...prev, instagram: undefined })); }} placeholder="@username" className="mt-2" />
                    {fieldErrors.instagram ? <p id="instagram-error" className="mt-1 text-sm text-red-600" role="alert">{fieldErrors.instagram.message}</p> : null}
                  </div>
                </div>
                <div aria-invalid={fieldErrors.comments ? "true" : undefined} aria-describedby={fieldErrors.comments ? "comments-error" : undefined}>
                  <Label htmlFor="comments">Коментар до запису</Label>
                  <Textarea id="comments" value={comments} onChange={(event) => { setComments(event.target.value); setFieldErrors((prev) => ({ ...prev, comments: undefined })); }} placeholder="Наприклад, потрібно обережно з лапами…" className="mt-2" />
                  {fieldErrors.comments ? <p id="comments-error" className="mt-1 text-sm text-red-600" role="alert">{fieldErrors.comments.message}</p> : null}
                </div>
                <div className="rounded-lg border border-border bg-accent/30 p-5"><div className="flex items-start gap-3"><Checkbox id="createAccount" checked={createAccount} onChange={(event) => setCreateAccount(event.target.checked)} className="mt-1" /><div><Label htmlFor="createAccount" className="cursor-pointer">Створити акаунт після запису</Label><p className="mt-1 text-sm text-muted-foreground">Ми не міняємо поточну auth-модель, а лише збережемо сумісність із нею.</p></div></div></div>
                <div className="flex items-start gap-3"><Checkbox id="consent" checked={consent} onChange={(event) => setConsent(event.target.checked)} className="mt-1" /><div><Label htmlFor="consent" className="cursor-pointer text-sm font-normal">Я погоджуюсь з умовами обробки персональних даних та правилами салону.</Label></div></div>
                {error ? <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">{error}</div> : null}
                <div className="hidden justify-between border-t border-border pt-6 lg:flex"><Link href="/booking/schedule"><Button variant="outline"><ArrowLeftIcon className="h-4 w-4" /> Назад</Button></Link><Button type="submit" disabled={!canSubmit || busy} size="lg">{busy ? "Надсилаємо…" : "Підтвердити запис"} {!busy ? <ArrowRightIcon className="h-4 w-4" /> : null}</Button></div>
              </form>
            </div>
            <div className="hidden lg:block"><BookingSummaryPanel booking={{ ...booking, contact: { fullName, phone, instagram, comments, createAccount, consent } }} /></div>
          </div>
        </div>
      </div>
      <BottomMobileCTA onClick={() => void handleSubmit()} disabled={!canSubmit || busy}>{busy ? "Надсилаємо…" : "Підтвердити запис"} {!busy ? <ArrowRightIcon className="h-4 w-4" /> : null}</BottomMobileCTA>
    </div>
  );
}

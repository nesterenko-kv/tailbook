"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ClientHeader } from "@/components/client-header";
import { Button, Card } from "@/components/ui";
import { ArrowRightIcon, CalendarIcon, CheckCircleIcon, HomeIcon, RefreshIcon } from "@/components/icons";
import { useClientBooking } from "@/lib/client-booking-context";
import { formatCurrency } from "@/lib/booking-helpers";
import { serviceTemplates } from "@/lib/display-data";
import { publicApiRequest } from "@/lib/api";
import type { PublicBookingRequestStatus } from "@/lib/types";

type PollingStatus = "idle" | "pending" | "confirmed" | "appointed" | "not_found" | "error";

export default function BookingConfirmationPage() {
  const { booking, resetBooking } = useClientBooking();
  const [pollingStatus, setPollingStatus] = useState<PollingStatus>("idle");
  const [appointmentId, setAppointmentId] = useState<string | null>(null);
  const [pollingError, setPollingError] = useState(false);

  useEffect(() => {
    if (!booking.lastCreatedRequestId) return;
    let cancelled = false;

    async function poll() {
      try {
        const data = await publicApiRequest<PublicBookingRequestStatus>(
          `/api/public/booking-requests/${booking.lastCreatedRequestId}`
        );
        if (cancelled) return;

        if (data.appointmentId) {
          setAppointmentId(data.appointmentId);
          setPollingStatus("appointed");
          return;
        }

        const normalized = data.status.toLowerCase();
        if (["submitted", "needsreview", "pending"].includes(normalized)) {
          setPollingStatus("pending");
        } else if (["confirmed", "proposed"].includes(normalized)) {
          setPollingStatus("confirmed");
        } else if (["completed", "closed", "cancelled", "noshow"].includes(normalized)) {
          setPollingStatus("confirmed");
        }
      } catch {
        if (!cancelled) {
          setPollingError(true);
        }
      }
    }

    void poll();
    const interval = setInterval(poll, 10000);
    return () => { cancelled = true; clearInterval(interval); };
  }, [booking.lastCreatedRequestId]);

  const selectedServices = serviceTemplates.filter((item) => booking.selectedTemplateIds.includes(item.id));
  const totalPrice = booking.quote?.totalAmount ?? selectedServices.reduce((sum, service) => sum + service.priceFrom, 0);
  const totalDuration = booking.quote?.reservedMinutes ?? selectedServices.reduce((sum, service) => sum + service.duration, 0);

  const statusBadge = pollingStatus === "appointed" ? { label: "Підтверджено", tone: "default" as const }
    : pollingStatus === "confirmed" ? { label: "Опрацьовується", tone: "secondary" as const }
    : pollingStatus === "pending" ? { label: "Очікує підтвердження", tone: "secondary" as const }
    : null;
  const statusLabel = statusBadge?.label;

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader />
      <div className="container py-12 lg:py-20">
        <div className="mx-auto max-w-2xl text-center">
          <div className="mb-8">
            <div className="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-green-100">
              {pollingStatus === "appointed" ? <CalendarIcon className="h-12 w-12 text-green-600" /> : <CheckCircleIcon className="h-12 w-12 text-green-600" />}
            </div>
            <h1 className="mb-3 text-3xl font-bold lg:text-4xl">Запит на запис відправлено!</h1>
            <p className="text-lg text-muted-foreground">Дякуємо, {booking.contact.fullName || "друже"}! Ми зв'яжемося з вами найближчим часом для підтвердження.</p>
            {statusLabel ? (
              <div className="mt-4">
                <span className="inline-flex items-center rounded-full bg-primary/15 px-4 py-1.5 text-sm font-medium text-foreground">
                  {pollingStatus === "pending" ? <RefreshIcon className="mr-2 h-4 w-4 animate-spin" /> : null}
                  Статус: {statusLabel}
                </span>
              </div>
            ) : null}
            {pollingStatus === "appointed" && appointmentId ? (
              <p className="mt-3 text-sm text-muted-foreground">
                <Link href={`/dashboard/appointments/${appointmentId}`} className="text-primary underline">Переглянути запис</Link>
              </p>
            ) : null}
            {pollingError ? (
              <p className="mt-3 text-sm text-amber-600">Не вдалося оновити статус. Сторінка оновлюватиме статус автоматично.</p>
            ) : null}
          </div>

          <Card className="mb-8 p-6 text-left lg:p-8">
            <h2 className="mb-4 text-lg font-medium">Деталі вашого запису</h2>
            {booking.pet ? <div className="mb-4 border-b border-border pb-4"><p className="mb-1 text-sm text-muted-foreground">Вихованець</p><p className="font-medium">{booking.pet.name} ({booking.pet.breedName || booking.pet.animalTypeCode || "Профіль"})</p></div> : null}
            {selectedServices.length > 0 ? <div className="mb-4 border-b border-border pb-4"><p className="mb-2 text-sm text-muted-foreground">Послуги</p><ul className="space-y-1">{selectedServices.map((service) => <li key={service.id} className="flex justify-between gap-4"><span>{service.name}</span><span className="text-muted-foreground">від {formatCurrency(service.priceFrom)}</span></li>)}</ul></div> : null}
            {booking.preferredDate && booking.preferredSlot ? <div className="mb-4 border-b border-border pb-4"><p className="mb-1 text-sm text-muted-foreground">Дата та час</p><div className="flex items-center gap-2"><CalendarIcon className="h-4 w-4 text-primary" /><p className="font-medium">{booking.preferredDate}, {booking.preferredSlot}</p></div><p className="mt-1 text-sm text-muted-foreground">Орієнтовна тривалість: {totalDuration} хв</p></div> : null}
            <div className="mb-4 border-b border-border pb-4"><p className="mb-1 text-sm text-muted-foreground">Контакти</p>{booking.contact.phone ? <p className="text-sm">{booking.contact.phone}</p> : null}{booking.contact.instagram ? <p className="text-sm">{booking.contact.instagram}</p> : null}</div>
            {booking.lastCreatedRequestId ? <div className="mb-4 border-b border-border pb-4"><p className="mb-1 text-sm text-muted-foreground">Номер заявки</p><p className="font-medium">{booking.lastCreatedRequestId}</p></div> : null}
            <div className="flex items-center justify-between"><span className="font-medium">Орієнтовна вартість:</span><span className="text-xl font-bold text-primary">від {formatCurrency(totalPrice)}</span></div>
            <p className="mt-2 text-xs text-muted-foreground">Фінальна ціна може відрізнятися залежно від стану шерсті та поведінки вихованця.</p>
          </Card>

          <div className="mb-8 rounded-lg bg-accent/50 p-6 text-left"><h3 className="mb-3 font-medium">Що далі?</h3><ul className="space-y-2 text-sm text-muted-foreground"><li className="flex items-start gap-2"><span className="text-primary">•</span><span>Наш менеджер зв'яжеться з вами для підтвердження часу.</span></li><li className="flex items-start gap-2"><span className="text-primary">•</span><span>Ви отримаєте підтвердження у телефон або Instagram.</span></li><li className="flex items-start gap-2"><span className="text-primary">•</span><span>Будь ласка, приходьте за 5-10 хвилин до призначеного часу.</span></li></ul></div>

          <div className="flex flex-col justify-center gap-4 sm:flex-row">
            <Link href="/" onClick={() => resetBooking()}><Button variant="outline" size="lg" className="w-full sm:w-auto"><HomeIcon className="h-4 w-4" /> На головну</Button></Link>
            <Link href="/booking/services" onClick={() => resetBooking()}><Button size="lg" className="w-full sm:w-auto">Записати іншого вихованця <ArrowRightIcon className="h-4 w-4" /></Button></Link>
          </div>
        </div>
      </div>
    </div>
  );
}

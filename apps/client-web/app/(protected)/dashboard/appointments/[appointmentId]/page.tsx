"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import type { ClientAppointmentDetail, ClientPetDetail } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Badge, Button, Card, Input, Label, Separator } from "@/components/ui";
import { ArrowLeftIcon, CalendarIcon, ClockIcon, MapPinIcon, PhoneIcon, RefreshIcon } from "@/components/icons";
import { formatCurrency, formatDateLong, formatTime, getAppointmentStatusLabel } from "@/lib/booking-helpers";
import { salonInfo } from "@/lib/display-data";

export default function AppointmentDetailPage() {
  const params = useParams<{ appointmentId: string }>();
  const appointmentId = params?.appointmentId as string;
  const [appointment, setAppointment] = useState<ClientAppointmentDetail | null>(null);
  const [pet, setPet] = useState<ClientPetDetail | null>(null);
  const [rescheduleDialogOpen, setRescheduleDialogOpen] = useState(false);
  const [rescheduleDate, setRescheduleDate] = useState("");
  const [rescheduleTime, setRescheduleTime] = useState("");
  const [rescheduling, setRescheduling] = useState(false);
  const [rescheduleError, setRescheduleError] = useState<string | null>(null);
  const [rescheduleSuccess, setRescheduleSuccess] = useState<string | null>(null);
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const detail = await apiRequest<ClientAppointmentDetail>(`/api/client/appointments/${appointmentId}`);
        if (cancelled) return;
        setAppointment(detail);
        const d = new Date(detail.startAt);
        setRescheduleDate(d.toISOString().slice(0, 10));
        setRescheduleTime(d.toISOString().slice(11, 16));
        try {
          const petDetail = await apiRequest<ClientPetDetail>(`/api/client/me/pets/${detail.petId}`);
          if (!cancelled) setPet(petDetail);
        } catch {
          if (!cancelled) setPet(null);
        }
      } catch {
        if (!cancelled) setAppointment(null);
      }
    }
    if (appointmentId) void load();
    return () => { cancelled = true; };
  }, [appointmentId]);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;
    if (rescheduleDialogOpen && !el.open) {
      el.showModal();
    } else if (!rescheduleDialogOpen && el.open) {
      el.close();
    }
  }, [rescheduleDialogOpen]);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;
    function handleClose() {
      setRescheduleDialogOpen(false);
    }
    el.addEventListener("close", handleClose);
    return () => el.removeEventListener("close", handleClose);
  }, []);

  if (!appointment) {
    return <div className="min-h-screen bg-background"><ClientHeader showProfile /><div className="container py-20 text-center"><h1 className="mb-2 text-2xl font-bold">Запис не знайдено</h1><Link href="/dashboard/appointments"><Button>Повернутися</Button></Link></div></div>;
  }

  const status = getAppointmentStatusLabel(appointment.status);
  const isReschedulable = ["confirmed", "rescheduled"].includes(appointment.status.toLowerCase());
  const isFinished = ["completed", "closed", "cancelled"].includes(appointment.status.toLowerCase());

  async function handleReschedule() {
    if (!appointment) return;
    setRescheduleError(null);
    setRescheduleSuccess(null);

    if (!rescheduleDate || !rescheduleTime) {
      setRescheduleError("Виберіть дату та час.");
      return;
    }

    const startAt = new Date(`${rescheduleDate}T${rescheduleTime}`);
    if (isNaN(startAt.getTime())) {
      setRescheduleError("Невірний формат дати або часу.");
      return;
    }

    setRescheduling(true);
    try {
      await apiRequest(`/api/client/appointments/${appointmentId}/reschedule`, {
        method: "POST",
        body: JSON.stringify({
          appointmentId,
          groomerId: appointment.groomerId,
          startAt: startAt.toISOString(),
          expectedVersionNo: appointment.versionNo
        })
      });
      setRescheduleSuccess("Запис успішно перенесено!");
      setRescheduleDialogOpen(false);
      const detail = await apiRequest<ClientAppointmentDetail>(`/api/client/appointments/${appointmentId}`);
      setAppointment(detail);
      const d = new Date(detail.startAt);
      setRescheduleDate(d.toISOString().slice(0, 10));
      setRescheduleTime(d.toISOString().slice(11, 16));
    } catch (err) {
      setRescheduleError(err instanceof ApiError ? err.message : "Не вдалося перенести запис.");
    } finally {
      setRescheduling(false);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mx-auto max-w-3xl">
          {rescheduleSuccess ? <div className="mb-6 rounded-lg border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800">{rescheduleSuccess}</div> : null}
          <div className="mb-6 flex items-center justify-between gap-4"><Badge variant={status.tone} className="px-4 py-1 text-base">{status.label}</Badge><Link href="/dashboard/appointments"><Button variant="ghost"><ArrowLeftIcon className="h-4 w-4" /> До записів</Button></Link></div>
          <Card className="mb-6 p-6 lg:p-8"><div className="mb-6 flex items-start gap-4"><div className="flex h-20 w-20 items-center justify-center rounded-full bg-accent text-2xl">🐾</div><div><h1 className="mb-2 text-2xl font-bold">{pet?.name ?? "Ваш вихованець"}</h1><p className="text-muted-foreground">{appointment.breedName}</p><p className="text-sm text-muted-foreground">{pet?.sizeCategoryCode ?? pet?.animalTypeName ?? "Профіль"}</p></div></div><Separator className="my-6" /><div className="space-y-4 mb-6"><div className="flex items-start gap-3"><CalendarIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Дата</p><p className="text-muted-foreground">{formatDateLong(appointment.startAt)}</p></div></div><div className="flex items-start gap-3"><ClockIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Час</p><p className="text-muted-foreground">{formatTime(appointment.startAt)} – {formatTime(appointment.endAt)} (орієнтовно {appointment.reservedMinutes} хв)</p></div></div></div></Card>
          <Card className="mb-6 p-6 lg:p-8"><h2 className="mb-4 text-lg font-medium">Послуги</h2><div className="space-y-3 mb-4">{appointment.items.map((item) => <div key={item.id} className="flex justify-between gap-4"><div><p className="font-medium">{item.offerDisplayName}</p><p className="text-sm text-muted-foreground">{item.itemType} · {item.reservedMinutes} хв</p></div><div className="text-right font-medium">{formatCurrency(item.priceAmount)}</div></div>)}</div><Separator className="my-4" /><div className="flex justify-between text-lg"><span className="font-medium">Орієнтовна вартість:</span><span className="font-bold text-primary">{formatCurrency(appointment.totalAmount)}</span></div><p className="mt-2 text-xs text-muted-foreground">Фінальна ціна може бути нижчою або вищою лише через явні signed adjustments у візиті.</p></Card>
          <Card className="mb-6 p-6 lg:p-8"><h2 className="mb-4 text-lg font-medium">Салон Tailbook</h2><div className="space-y-3"><div className="flex items-start gap-3"><MapPinIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Адреса</p><p className="text-muted-foreground">{salonInfo.address}</p></div></div><div className="flex items-start gap-3"><PhoneIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Телефон</p><a href={salonInfo.phoneHref} className="text-muted-foreground hover:text-primary">{salonInfo.phone}</a></div></div></div></Card>
          <div className="flex gap-4">{isFinished ? <Link href={`/booking/services?pet=${appointment.petId}`} className="flex-1"><Button className="w-full" size="lg"><RefreshIcon className="h-4 w-4" /> Повторити запис</Button></Link> : null}{isReschedulable ? <Button variant="outline" size="lg" className="flex-1" onClick={() => setRescheduleDialogOpen(true)}><CalendarIcon className="h-4 w-4" /> Перенести запис</Button> : null}</div>
        </div>
      </div>

      <dialog
        ref={dialogRef}
        onClick={(e) => { if (e.target === dialogRef.current) setRescheduleDialogOpen(false); }}
        className="w-[min(480px,calc(100vw-2rem))] rounded-3xl border border-border bg-card p-0 text-foreground shadow-2xl backdrop:bg-black/50 backdrop:backdrop-blur-sm open:flex"
      >
        <div className="flex w-full flex-col p-6">
          <h2 className="text-lg font-semibold">Перенесення запису</h2>
          <p className="mt-1 text-sm text-muted-foreground">Виберіть нову дату та час. Послуги та майстер залишаються без змін.</p>
          {rescheduleError ? <div className="mt-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{rescheduleError}</div> : null}
          <div className="mt-6 space-y-4">
            <div>
              <Label htmlFor="reschedule-date">Нова дата</Label>
              <Input id="reschedule-date" type="date" value={rescheduleDate} onChange={(e) => setRescheduleDate(e.target.value)} className="mt-1" />
            </div>
            <div>
              <Label htmlFor="reschedule-time">Новий час</Label>
              <Input id="reschedule-time" type="time" value={rescheduleTime} onChange={(e) => setRescheduleTime(e.target.value)} className="mt-1" />
            </div>
          </div>
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="outline" onClick={() => setRescheduleDialogOpen(false)}>Скасувати</Button>
            <Button onClick={handleReschedule} disabled={rescheduling}>{rescheduling ? "Перенесення..." : "Підтвердити перенесення"}</Button>
          </div>
        </div>
      </dialog>
    </div>
  );
}

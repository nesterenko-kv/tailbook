"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { apiRequest } from "@/lib/api";
import type { ClientAppointmentDetail, ClientPetDetail } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Badge, Button, Card, Separator } from "@/components/ui";
import { ArrowLeftIcon, CalendarIcon, ClockIcon, MapPinIcon, PhoneIcon, RefreshIcon } from "@/components/icons";
import { formatCurrency, formatDateLong, formatTime, getAppointmentStatusLabel } from "@/lib/booking-helpers";
import { salonInfo } from "@/lib/display-data";

export default function AppointmentDetailPage() {
  const params = useParams<{ appointmentId: string }>();
  const appointmentId = params?.appointmentId as string;
  const [appointment, setAppointment] = useState<ClientAppointmentDetail | null>(null);
  const [pet, setPet] = useState<ClientPetDetail | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const detail = await apiRequest<ClientAppointmentDetail>(`/api/client/appointments/${appointmentId}`);
        if (cancelled) return;
        setAppointment(detail);
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

  if (!appointment) {
    return <div className="min-h-screen bg-background"><ClientHeader showProfile /><div className="container py-20 text-center"><h1 className="mb-2 text-2xl font-bold">Запис не знайдено</h1><Link href="/dashboard/appointments"><Button>Повернутися</Button></Link></div></div>;
  }

  const status = getAppointmentStatusLabel(appointment.status);

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mx-auto max-w-3xl">
          <div className="mb-6 flex items-center justify-between gap-4"><Badge variant={status.tone} className="px-4 py-1 text-base">{status.label}</Badge><Link href="/dashboard/appointments"><Button variant="ghost"><ArrowLeftIcon className="h-4 w-4" /> До записів</Button></Link></div>
          <Card className="mb-6 p-6 lg:p-8"><div className="mb-6 flex items-start gap-4"><div className="flex h-20 w-20 items-center justify-center rounded-full bg-accent text-2xl">🐾</div><div><h1 className="mb-2 text-2xl font-bold">{pet?.name ?? "Ваш вихованець"}</h1><p className="text-muted-foreground">{appointment.breedName}</p><p className="text-sm text-muted-foreground">{pet?.sizeCategoryCode ?? pet?.animalTypeName ?? "Профіль"}</p></div></div><Separator className="my-6" /><div className="space-y-4 mb-6"><div className="flex items-start gap-3"><CalendarIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Дата</p><p className="text-muted-foreground">{formatDateLong(appointment.startAtUtc)}</p></div></div><div className="flex items-start gap-3"><ClockIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Час</p><p className="text-muted-foreground">{formatTime(appointment.startAtUtc)} – {formatTime(appointment.endAtUtc)} (орієнтовно {appointment.reservedMinutes} хв)</p></div></div></div></Card>
          <Card className="mb-6 p-6 lg:p-8"><h2 className="mb-4 text-lg font-medium">Послуги</h2><div className="space-y-3 mb-4">{appointment.items.map((item) => <div key={item.id} className="flex justify-between gap-4"><div><p className="font-medium">{item.offerDisplayName}</p><p className="text-sm text-muted-foreground">{item.itemType} · {item.reservedMinutes} хв</p></div><div className="text-right font-medium">{formatCurrency(item.priceAmount)}</div></div>)}</div><Separator className="my-4" /><div className="flex justify-between text-lg"><span className="font-medium">Орієнтовна вартість:</span><span className="font-bold text-primary">{formatCurrency(appointment.totalAmount)}</span></div><p className="mt-2 text-xs text-muted-foreground">Фінальна ціна може бути нижчою або вищою лише через явні signed adjustments у візиті.</p></Card>
          <Card className="mb-6 p-6 lg:p-8"><h2 className="mb-4 text-lg font-medium">Салон Tailbook</h2><div className="space-y-3"><div className="flex items-start gap-3"><MapPinIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Адреса</p><p className="text-muted-foreground">{salonInfo.address}</p></div></div><div className="flex items-start gap-3"><PhoneIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="mb-1 font-medium">Телефон</p><a href={salonInfo.phoneHref} className="text-muted-foreground hover:text-primary">{salonInfo.phone}</a></div></div></div></Card>
          <div className="flex gap-4">{["completed", "closed"].includes(appointment.status.toLowerCase()) ? <Link href={`/booking/services?pet=${appointment.petId}`} className="flex-1"><Button className="w-full" size="lg"><RefreshIcon className="h-4 w-4" /> Повторити запис</Button></Link> : <Button variant="outline" size="lg" className="flex-1" disabled>Керування записом відкриється після додавання відповідних команд API</Button>}</div>
        </div>
      </div>
    </div>
  );
}

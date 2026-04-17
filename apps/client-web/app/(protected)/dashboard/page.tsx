"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "@/lib/api";
import type { ClientAppointmentSummary, ClientPetSummary } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Avatar, AvatarFallback, Badge, Button, Card } from "@/components/ui";
import { ArrowRightIcon, CalendarIcon, PawIcon, PlusIcon, UserIcon } from "@/components/icons";
import { appointmentCanBeRepeated, formatDateLong, formatTime, getAppointmentStatusLabel } from "@/lib/booking-helpers";

export default function DashboardPage() {
  const [appointments, setAppointments] = useState<ClientAppointmentSummary[]>([]);
  const [pets, setPets] = useState<ClientPetSummary[]>([]);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const [loadedAppointments, loadedPets] = await Promise.all([
          apiRequest<ClientAppointmentSummary[]>("/api/client/appointments"),
          apiRequest<ClientPetSummary[]>("/api/client/me/pets")
        ]);
        if (cancelled) return;
        setAppointments(loadedAppointments);
        setPets(loadedPets);
      } catch {
        if (cancelled) return;
      }
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  const upcomingAppointments = useMemo(() => appointments.filter((item) => !["completed", "closed", "cancelled", "noshow"].includes(item.status.toLowerCase())), [appointments]);

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mb-8"><h1 className="mb-2 text-3xl font-bold">Вітаємо!</h1><p className="text-muted-foreground">Керуйте записами та вихованцями</p></div>
        <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Link href="/booking/services"><Card className="cursor-pointer border-primary/30 p-6 hover:shadow-md"><div className="flex items-center gap-4"><div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10"><PlusIcon className="h-6 w-6 text-primary" /></div><div><h3 className="mb-1 font-medium">Новий запис</h3><p className="text-sm text-muted-foreground">Записатися онлайн</p></div></div></Card></Link>
          <Link href="/dashboard/appointments"><Card className="cursor-pointer p-6 hover:shadow-md"><div className="flex items-center gap-4"><div className="flex h-12 w-12 items-center justify-center rounded-full bg-accent"><CalendarIcon className="h-6 w-6 text-primary" /></div><div><h3 className="mb-1 font-medium">Мої записи</h3><p className="text-sm text-muted-foreground">{upcomingAppointments.length} майбутніх</p></div></div></Card></Link>
          <Link href="/dashboard/pets"><Card className="cursor-pointer p-6 hover:shadow-md"><div className="flex items-center gap-4"><div className="flex h-12 w-12 items-center justify-center rounded-full bg-accent"><PawIcon className="h-6 w-6 text-primary" /></div><div><h3 className="mb-1 font-medium">Мої вихованці</h3><p className="text-sm text-muted-foreground">{pets.length} вихованців</p></div></div></Card></Link>
        </div>

        <div className="grid gap-8 lg:grid-cols-3">
          <div className="lg:col-span-2">
            <div className="mb-4 flex items-center justify-between"><h2 className="text-xl font-medium">Майбутні записи</h2><Link href="/dashboard/appointments"><Button variant="ghost" size="sm">Переглянути всі <ArrowRightIcon className="h-4 w-4" /></Button></Link></div>
            {upcomingAppointments.length > 0 ? <div className="space-y-4">{upcomingAppointments.slice(0, 3).map((appointment) => { const status = getAppointmentStatusLabel(appointment.status); return <Card key={appointment.id} className="p-6"><div className="mb-4 flex items-start justify-between gap-4"><div><h3 className="mb-1 font-medium">{appointment.petName}</h3><p className="mb-2 text-sm text-muted-foreground">{appointment.itemLabels.join(", ")}</p><div className="flex items-center gap-2 text-sm"><CalendarIcon className="h-4 w-4 text-primary" /><span>{formatDateLong(appointment.startAtUtc)}, {formatTime(appointment.startAtUtc)}</span></div></div><Badge variant={status.tone}>{status.label}</Badge></div><div className="flex items-center justify-between border-t border-border pt-4"><span className="text-sm text-muted-foreground">{appointment.status}</span><Link href={`/dashboard/appointments/${appointment.id}`}><Button variant="outline" size="sm">Деталі</Button></Link></div></Card>; })}</div> : <Card className="p-12 text-center"><CalendarIcon className="mx-auto mb-4 h-16 w-16 text-muted-foreground" /><h3 className="mb-2 font-medium">Немає майбутніх записів</h3><p className="mb-4 text-sm text-muted-foreground">Запишіться на зручний час прямо зараз</p><Link href="/booking/services"><Button>Записатися</Button></Link></Card>}
          </div>
          <div>
            <div className="mb-4 flex items-center justify-between"><h2 className="text-xl font-medium">Мої вихованці</h2><Link href="/dashboard/pets"><Button variant="ghost" size="sm">Всі</Button></Link></div>
            <div className="space-y-4">{pets.slice(0, 3).map((pet) => <Card key={pet.id} className="p-4"><div className="flex items-center gap-3"><Avatar className="h-12 w-12"><AvatarFallback>{pet.name.slice(0,1)}</AvatarFallback></Avatar><div className="flex-1"><h3 className="font-medium">{pet.name}</h3><p className="text-sm text-muted-foreground">{pet.breedName}</p></div></div><Link href={`/booking/services?pet=${pet.id}`} className="mt-3 block"><Button variant="outline" size="sm" className="w-full">Записати</Button></Link></Card>)}<Link href="/dashboard/profile"><Button variant="outline" className="w-full"><UserIcon className="h-4 w-4" /> Профіль та контакти</Button></Link></div>
          </div>
        </div>

        {appointments.some(appointmentCanBeRepeated) ? <div className="mt-8"><Link href="/dashboard/appointments"><Card className="cursor-pointer p-6 hover:shadow-md"><div className="flex items-center justify-between"><div><h3 className="mb-1 font-medium">Історія відвідувань</h3><p className="text-sm text-muted-foreground">Переглянути попередні візити та повторити запис</p></div><ArrowRightIcon className="h-5 w-5 text-muted-foreground" /></div></Card></Link></div> : null}
      </div>
    </div>
  );
}

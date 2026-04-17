"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiRequest } from "@/lib/api";
import type { ClientAppointmentSummary } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Badge, Button, Card } from "@/components/ui";
import { ArrowLeftIcon, CalendarIcon, RefreshIcon } from "@/components/icons";
import { appointmentCanBeRepeated, formatDateLong, formatTime, getAppointmentStatusLabel } from "@/lib/booking-helpers";

export default function DashboardAppointmentsPage() {
  const [appointments, setAppointments] = useState<ClientAppointmentSummary[]>([]);
  const [tab, setTab] = useState<"upcoming" | "history">("upcoming");

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const loaded = await apiRequest<ClientAppointmentSummary[]>("/api/client/appointments");
        if (!cancelled) setAppointments(loaded);
      } catch {
        if (!cancelled) setAppointments([]);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  const upcoming = useMemo(() => appointments.filter((item) => !["completed", "closed", "cancelled", "noshow"].includes(item.status.toLowerCase())), [appointments]);
  const history = useMemo(() => appointments.filter((item) => !upcoming.includes(item)), [appointments, upcoming]);
  const items = tab === "upcoming" ? upcoming : history;

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mb-6 flex items-center justify-between gap-4"><div><h1 className="text-3xl font-bold">Мої записи</h1><p className="text-muted-foreground">Усі активні та завершені appointments вашого профілю</p></div><Link href="/dashboard"><Button variant="ghost"><ArrowLeftIcon className="h-4 w-4" /> До кабінету</Button></Link></div>
        <div className="mb-8 grid w-full max-w-sm grid-cols-2 rounded-xl bg-secondary p-1"><button type="button" onClick={() => setTab("upcoming")} className={tab === "upcoming" ? "rounded-lg bg-card px-4 py-2 text-sm font-medium shadow-sm" : "rounded-lg px-4 py-2 text-sm text-muted-foreground"}>Майбутні</button><button type="button" onClick={() => setTab("history")} className={tab === "history" ? "rounded-lg bg-card px-4 py-2 text-sm font-medium shadow-sm" : "rounded-lg px-4 py-2 text-sm text-muted-foreground"}>Історія</button></div>
        {items.length > 0 ? <div className="space-y-4">{items.map((appointment) => { const status = getAppointmentStatusLabel(appointment.status); return <Card key={appointment.id} className="p-6"><div className="mb-4 flex items-start justify-between gap-4"><div><h3 className="mb-1 font-medium">{appointment.petName}</h3><p className="mb-2 text-sm text-muted-foreground">{appointment.itemLabels.join(", ")}</p><div className="flex items-center gap-2 text-sm"><CalendarIcon className="h-4 w-4 text-primary" /><span>{formatDateLong(appointment.startAtUtc)}, {formatTime(appointment.startAtUtc)}</span></div></div><Badge variant={status.tone}>{status.label}</Badge></div><div className="flex gap-2 border-t border-border pt-4"><Link href={`/dashboard/appointments/${appointment.id}`} className="flex-1"><Button variant="outline" className="w-full">Деталі</Button></Link>{appointmentCanBeRepeated(appointment) ? <Link href={`/booking/services?pet=${appointment.petId}`} className="flex-1"><Button className="w-full"><RefreshIcon className="h-4 w-4" /> Повторити</Button></Link> : null}</div></Card>; })}</div> : <Card className="p-12 text-center"><CalendarIcon className="mx-auto mb-4 h-16 w-16 text-muted-foreground" /><h3 className="mb-2 font-medium">Немає записів у цій вкладці</h3><p className="text-sm text-muted-foreground">Тут відображаються ваші appointments з client portal.</p></Card>}
      </div>
    </div>
  );
}

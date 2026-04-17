"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { BottomMobileCTA } from "@/components/bottom-mobile-cta";
import { ClientHeader } from "@/components/client-header";
import { ProgressSteps } from "@/components/progress-steps";
import { ServiceCard } from "@/components/service-card";
import { Button, Card } from "@/components/ui";
import { ArrowLeftIcon, ArrowRightIcon } from "@/components/icons";
import { useClientBooking } from "@/lib/client-booking-context";
import { formatCurrency } from "@/lib/booking-helpers";
import { serviceTemplates } from "@/lib/display-data";

const categories = [
  { id: "dog", label: "Собаки" },
  { id: "cat", label: "Коти" },
  { id: "extra", label: "Додатково" }
] as const;

export default function BookingServicesPage() {
  const router = useRouter();
  const { booking, patchBooking } = useClientBooking();
  const [category, setCategory] = useState<(typeof categories)[number]["id"]>("dog");
  const [selected, setSelected] = useState<string[]>(booking.selectedTemplateIds);

  const filteredServices = useMemo(() => serviceTemplates.filter((item) => item.category === category), [category]);
  const selectedServices = useMemo(() => serviceTemplates.filter((item) => selected.includes(item.id)), [selected]);

  function toggleService(serviceId: string) {
    setSelected((current) => current.includes(serviceId) ? current.filter((id) => id !== serviceId) : [...current, serviceId]);
  }

  function handleContinue() {
    patchBooking({ selectedTemplateIds: selected });
    router.push("/booking/pet");
  }

  return (
    <div className="min-h-screen bg-background pb-24 lg:pb-8">
      <ClientHeader />
      <div className="container py-8">
        <ProgressSteps steps={[{ label: "Послуги", active: true }, { label: "Вихованець" }, { label: "Майстер та час" }, { label: "Контакти" }]} />
        <div className="mx-auto max-w-5xl">
          <div className="mb-8">
            <h1 className="mb-2 text-3xl font-bold">Оберіть послуги</h1>
            <p className="text-muted-foreground">Виберіть одну або декілька послуг для вашого вихованця</p>
          </div>

          <div className="mb-8 grid w-full max-w-md grid-cols-3 rounded-xl bg-secondary p-1">
            {categories.map((item) => (
              <button key={item.id} type="button" onClick={() => setCategory(item.id)} className={item.id === category ? "rounded-lg bg-card px-4 py-2 text-sm font-medium shadow-sm" : "rounded-lg px-4 py-2 text-sm text-muted-foreground"}>{item.label}</button>
            ))}
          </div>

          <div className="mb-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {filteredServices.map((service) => (
              <ServiceCard key={service.id} service={service} selected={selected.includes(service.id)} onToggle={() => toggleService(service.id)} interactive />
            ))}
          </div>

          {selectedServices.length > 0 ? (
            <div className="mb-6 rounded-lg bg-accent/50 p-6">
              <h3 className="mb-3 font-medium">Обрано послуг: {selectedServices.length}</h3>
              <div className="mb-4 space-y-2">
                {selectedServices.map((service) => (
                  <div key={service.id} className="flex justify-between gap-4 text-sm"><span>{service.name}</span><span className="text-muted-foreground">від {formatCurrency(service.priceFrom)}</span></div>
                ))}
              </div>
              <div className="flex justify-between border-t border-border pt-3 font-medium"><span>Орієнтовна вартість:</span><span className="text-primary">від {formatCurrency(selectedServices.reduce((sum, service) => sum + service.priceFrom, 0))}</span></div>
            </div>
          ) : null}

          <Card className="mb-8 border-primary/25 p-5 text-sm text-muted-foreground">
            Остаточний real offer та точна оцінка підтягнуться після кроку з даними про вихованця — це зберігає коректну доменну логіку pricing та scheduling.
          </Card>

          <div className="hidden justify-between lg:flex">
            <Link href="/"><Button variant="outline"><ArrowLeftIcon className="h-4 w-4" /> Назад</Button></Link>
            <Button onClick={handleContinue} disabled={selectedServices.length === 0} size="lg">Продовжити <ArrowRightIcon className="h-4 w-4" /></Button>
          </div>
        </div>
      </div>
      <BottomMobileCTA onClick={handleContinue} disabled={selectedServices.length === 0}>Продовжити <ArrowRightIcon className="h-4 w-4" /></BottomMobileCTA>
    </div>
  );
}

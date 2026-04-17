"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { apiRequest } from "@/lib/api";
import type { ClientPetDetail } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Button, Card, Separator } from "@/components/ui";
import { ArrowLeftIcon, PawIcon } from "@/components/icons";

export default function DashboardPetDetailPage() {
  const params = useParams<{ petId: string }>();
  const petId = params?.petId as string;
  const [pet, setPet] = useState<ClientPetDetail | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const loaded = await apiRequest<ClientPetDetail>(`/api/client/me/pets/${petId}`);
        if (!cancelled) setPet(loaded);
      } catch {
        if (!cancelled) setPet(null);
      }
    }
    if (petId) void load();
    return () => { cancelled = true; };
  }, [petId]);

  if (!pet) {
    return <div className="min-h-screen bg-background"><ClientHeader showProfile /><div className="container py-20 text-center"><h1 className="mb-2 text-2xl font-bold">Профіль не знайдено</h1><Link href="/dashboard/pets"><Button>Повернутися</Button></Link></div></div>;
  }

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mx-auto max-w-3xl"><div className="mb-6 flex items-center justify-between gap-4"><div><h1 className="text-3xl font-bold">{pet.name}</h1><p className="text-muted-foreground">Профіль вихованця</p></div><Link href="/dashboard/pets"><Button variant="ghost"><ArrowLeftIcon className="h-4 w-4" /> До списку</Button></Link></div><Card className="p-6 lg:p-8"><div className="mb-6 flex items-center gap-4"><div className="flex h-20 w-20 items-center justify-center rounded-full bg-accent"><PawIcon className="h-9 w-9 text-primary" /></div><div><h2 className="text-xl font-medium">{pet.name}</h2><p className="text-muted-foreground">{pet.breedName}</p><p className="text-sm text-muted-foreground">{pet.animalTypeName}</p></div></div><Separator className="my-6" /><div className="grid gap-4 sm:grid-cols-2"><div><p className="mb-1 text-sm text-muted-foreground">Тип шерсті</p><p className="font-medium">{pet.coatTypeCode ?? "Не вказано"}</p></div><div><p className="mb-1 text-sm text-muted-foreground">Розмір</p><p className="font-medium">{pet.sizeCategoryCode ?? "Не вказано"}</p></div><div><p className="mb-1 text-sm text-muted-foreground">Дата народження</p><p className="font-medium">{pet.birthDate ?? "Не вказано"}</p></div><div><p className="mb-1 text-sm text-muted-foreground">Вага</p><p className="font-medium">{pet.weightKg ? `${pet.weightKg} кг` : "Не вказано"}</p></div></div>{pet.notes ? <><Separator className="my-6" /><div><p className="mb-1 text-sm text-muted-foreground">Нотатки</p><p className="leading-6">{pet.notes}</p></div></> : null}<div className="mt-8"><Link href={`/booking/services?pet=${pet.id}`}><Button size="lg">Записати цього вихованця</Button></Link></div></Card></div>
      </div>
    </div>
  );
}

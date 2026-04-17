"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { apiRequest } from "@/lib/api";
import type { ClientPetSummary } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Avatar, AvatarFallback, Button, Card } from "@/components/ui";
import { ArrowLeftIcon, PawIcon, PlusIcon } from "@/components/icons";

export default function DashboardPetsPage() {
  const [pets, setPets] = useState<ClientPetSummary[]>([]);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const loaded = await apiRequest<ClientPetSummary[]>("/api/client/me/pets");
        if (!cancelled) setPets(loaded);
      } catch {
        if (!cancelled) setPets([]);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mb-6 flex items-center justify-between gap-4"><div><h1 className="text-3xl font-bold">Мої вихованці</h1><p className="text-muted-foreground">Усі профілі, доступні для client portal</p></div><Link href="/dashboard"><Button variant="ghost"><ArrowLeftIcon className="h-4 w-4" /> До кабінету</Button></Link></div>
        {pets.length > 0 ? <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{pets.map((pet) => <Card key={pet.id} className="p-5"><div className="mb-4 flex items-center gap-3"><Avatar className="h-14 w-14"><AvatarFallback>{pet.name.slice(0,1)}</AvatarFallback></Avatar><div><h3 className="font-medium">{pet.name}</h3><p className="text-sm text-muted-foreground">{pet.breedName}</p><p className="text-sm text-muted-foreground">{pet.animalTypeName}</p></div></div><div className="flex gap-2"><Link href={`/dashboard/pets/${pet.id}`} className="flex-1"><Button variant="outline" className="w-full">Деталі</Button></Link><Link href={`/booking/services?pet=${pet.id}`} className="flex-1"><Button className="w-full">Записати</Button></Link></div></Card>)}</div> : <Card className="p-12 text-center"><PawIcon className="mx-auto mb-4 h-16 w-16 text-muted-foreground" /><h3 className="mb-2 font-medium">Профілі вихованців поки відсутні</h3><p className="mb-4 text-sm text-muted-foreground">Додайте вихованця через flow нового запису.</p><Link href="/booking/services"><Button><PlusIcon className="h-4 w-4" /> Створити профіль у записі</Button></Link></Card>}
      </div>
    </div>
  );
}

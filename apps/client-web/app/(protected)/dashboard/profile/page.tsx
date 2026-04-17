"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest } from "@/lib/api";
import { clearSession } from "@/lib/auth";
import type { ClientContactPreferences, ClientMeResponse } from "@/lib/types";
import { ClientHeader } from "@/components/client-header";
import { Button, Card } from "@/components/ui";
import { ArrowLeftIcon } from "@/components/icons";

export default function DashboardProfilePage() {
  const router = useRouter();
  const [me, setMe] = useState<ClientMeResponse | null>(null);
  const [prefs, setPrefs] = useState<ClientContactPreferences | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const [loadedMe, loadedPrefs] = await Promise.all([
          apiRequest<ClientMeResponse>("/api/client/me"),
          apiRequest<ClientContactPreferences>("/api/client/me/contact-preferences")
        ]);
        if (cancelled) return;
        setMe(loadedMe);
        setPrefs(loadedPrefs);
      } catch {
        if (cancelled) return;
      }
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showProfile />
      <div className="container py-8">
        <div className="mx-auto max-w-3xl"><div className="mb-6 flex items-center justify-between gap-4"><div><h1 className="text-3xl font-bold">Профіль</h1><p className="text-muted-foreground">Ваш client portal акаунт і контактні дані</p></div><Link href="/dashboard"><Button variant="ghost"><ArrowLeftIcon className="h-4 w-4" /> До кабінету</Button></Link></div><Card className="mb-6 p-6 lg:p-8"><h2 className="mb-4 text-lg font-medium">Основна інформація</h2><div className="space-y-3 text-sm"><div className="flex justify-between gap-4"><span className="text-muted-foreground">Ім’я</span><span className="font-medium">{me?.displayName ?? "—"}</span></div><div className="flex justify-between gap-4"><span className="text-muted-foreground">Email</span><span className="font-medium">{me?.email ?? "—"}</span></div><div className="flex justify-between gap-4"><span className="text-muted-foreground">Client ID</span><span className="font-medium">{me?.clientId ?? "—"}</span></div></div></Card><Card className="mb-6 p-6 lg:p-8"><h2 className="mb-4 text-lg font-medium">Контактні методи</h2>{prefs?.methods?.length ? <div className="space-y-3">{prefs.methods.map((method) => <div key={method.id} className="flex items-start justify-between gap-4 rounded-lg border border-border p-4"><div><p className="font-medium">{method.methodType}</p><p className="text-sm text-muted-foreground">{method.displayValue}</p></div><div className="text-right text-sm text-muted-foreground"><p>{method.verificationStatus}</p>{method.isPreferred ? <p>Preferred</p> : null}</div></div>)}</div> : <p className="text-sm text-muted-foreground">Контакти не знайдено або endpoint ще не повернув дані.</p>}</Card><Button variant="outline" onClick={() => { clearSession(); router.push('/login'); }}>Вийти з акаунта</Button></div>
      </div>
    </div>
  );
}

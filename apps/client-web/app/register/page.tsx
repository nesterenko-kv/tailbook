"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { ClientHeader } from "@/components/client-header";
import { Button, Card, Input, Label } from "@/components/ui";
import { apiRequest, ApiError } from "@/lib/api";
import { storeSession } from "@/lib/auth";
import type { ClientLoginResponse } from "@/lib/types";

export default function RegisterPage() {
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const response = await apiRequest<ClientLoginResponse>("/api/client/auth/register", {
        method: "POST",
        body: JSON.stringify({ displayName, email, password })
      });
      storeSession(response.accessToken, response.user.email);
      router.replace("/dashboard");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Не вдалося створити акаунт.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader />
      <div className="container flex min-h-[calc(100vh-4rem)] items-center justify-center py-12">
        <Card className="w-full max-w-md p-8">
          <h1 className="mb-2 text-3xl font-bold">Створення акаунта</h1>
          <p className="mb-8 text-muted-foreground">Акаунт прискорює повторні записи і дає доступ до історії візитів.</p>
          <form onSubmit={onSubmit} className="space-y-5">
            <div><Label htmlFor="displayName">Ім’я</Label><Input id="displayName" value={displayName} onChange={(event) => setDisplayName(event.target.value)} placeholder="Ваше ім’я" className="mt-2" /></div>
            <div><Label htmlFor="email">Email</Label><Input id="email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="you@example.com" className="mt-2" /></div>
            <div><Label htmlFor="password">Пароль</Label><Input id="password" type="password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="••••••••" className="mt-2" /></div>
            {error ? <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div> : null}
            <Button type="submit" size="lg" className="w-full" disabled={busy}>{busy ? "Створюємо…" : "Створити акаунт"}</Button>
          </form>
          <p className="mt-6 text-sm text-muted-foreground">Уже зареєстровані? <Link href="/login" className="text-primary underline">Увійти</Link></p>
        </Card>
      </div>
    </div>
  );
}

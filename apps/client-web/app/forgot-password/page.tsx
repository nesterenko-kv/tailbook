"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { ClientHeader } from "@/components/client-header";
import { Button, Card, Input, Label } from "@/components/ui";
import { ApiError, publicApiRequest } from "@/lib/api";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    try {
      await publicApiRequest<void>("/api/identity/auth/request-password-reset", {
        method: "POST",
        body: JSON.stringify({ email })
      });
      setSubmitted(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Не вдалося надіслати запит.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader />
      <div className="container flex min-h-[calc(100vh-4rem)] items-center justify-center py-12">
        <Card className="w-full max-w-md p-8">
          <h1 className="mb-2 text-3xl font-bold">Відновлення пароля</h1>
          <p className="mb-8 text-muted-foreground">Вкажіть email акаунта, і ми надішлемо посилання для зміни пароля.</p>

          {submitted ? (
            <div className="space-y-6">
              <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                Якщо цей email належить акаунту Tailbook, посилання для відновлення буде надіслано найближчим часом.
              </div>
              <Link href="/login">
                <Button type="button" size="lg" className="w-full">Повернутися до входу</Button>
              </Link>
            </div>
          ) : (
            <form onSubmit={onSubmit} className="space-y-5">
              <div>
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} placeholder="you@example.com" className="mt-2" required />
              </div>
              {error ? <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div> : null}
              <Button type="submit" size="lg" className="w-full" disabled={busy}>{busy ? "Надсилаємо…" : "Надіслати посилання"}</Button>
            </form>
          )}

          <p className="mt-6 text-sm text-muted-foreground">Згадали пароль? <Link href="/login" className="text-primary underline">Увійти</Link></p>
        </Card>
      </div>
    </div>
  );
}

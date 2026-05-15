"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, Suspense, useState } from "react";
import { ClientHeader } from "@/components/client-header";
import { Button, Card, Input, Label } from "@/components/ui";
import { ApiError, publicApiRequest } from "@/lib/api";
import { clearSession } from "@/lib/auth";

export default function ResetPasswordPage() {
  return (
    <Suspense fallback={null}>
      <ResetPasswordContent />
    </Suspense>
  );
}

function ResetPasswordContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [completed, setCompleted] = useState(false);
  const [error, setError] = useState<string | null>(token ? null : "Посилання для відновлення неповне або пошкоджене.");

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!token) {
      setError("Посилання для відновлення неповне або пошкоджене.");
      return;
    }
    if (newPassword.length < 8) {
      setError("Пароль має містити щонайменше 8 символів.");
      return;
    }
    if (newPassword !== confirmPassword) {
      setError("Паролі не збігаються.");
      return;
    }

    setBusy(true);
    setError(null);

    try {
      await publicApiRequest<void>("/api/identity/auth/reset-password", {
        method: "POST",
        body: JSON.stringify({ token, newPassword })
      });
      clearSession();
      setCompleted(true);
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        setError("Посилання недійсне або вже прострочене. Запитайте нове посилання для відновлення.");
      } else {
        setError(err instanceof ApiError ? err.message : "Не вдалося змінити пароль.");
      }
    } finally {
      setBusy(false);
    }
  }

  function goToLogin() {
    router.replace("/login");
  }

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader />
      <div className="container flex min-h-[calc(100vh-4rem)] items-center justify-center py-12">
        <Card className="w-full max-w-md p-8">
          <h1 className="mb-2 text-3xl font-bold">Новий пароль</h1>
          <p className="mb-8 text-muted-foreground">Створіть новий пароль для доступу до клієнтського порталу.</p>

          {completed ? (
            <div className="space-y-6">
              <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                Пароль оновлено. Увійдіть з новим паролем, щоб продовжити.
              </div>
              <Button type="button" size="lg" className="w-full" onClick={goToLogin}>Увійти</Button>
            </div>
          ) : (
            <form onSubmit={onSubmit} className="space-y-5">
              <div>
                <Label htmlFor="newPassword">Новий пароль</Label>
                <Input id="newPassword" type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} placeholder="••••••••" className="mt-2" autoComplete="new-password" required disabled={!token} />
              </div>
              <div>
                <Label htmlFor="confirmPassword">Повторіть пароль</Label>
                <Input id="confirmPassword" type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} placeholder="••••••••" className="mt-2" autoComplete="new-password" required disabled={!token} />
              </div>
              {error ? <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div> : null}
              <Button type="submit" size="lg" className="w-full" disabled={busy || !token}>{busy ? "Оновлюємо…" : "Оновити пароль"}</Button>
            </form>
          )}

          <p className="mt-6 text-sm text-muted-foreground">Потрібне нове посилання? <Link href="/forgot-password" className="text-primary underline">Надіслати ще раз</Link></p>
        </Card>
      </div>
    </div>
  );
}

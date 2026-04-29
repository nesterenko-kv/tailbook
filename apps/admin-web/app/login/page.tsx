"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { setAdminSession } from "@/lib/auth";
import type { LoginResponse } from "@/lib/types";
import { Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton } from "@/components/ui";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@tailbook.local");
  const [password, setPassword] = useState("Admin12345!");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);
    try {
      const response = await apiRequest<LoginResponse>("/api/identity/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password })
      });
      setAdminSession({ accessToken: response.accessToken, refreshToken: response.refreshToken, email: response.user.email, displayName: response.user.displayName });
      router.replace("/clients");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Login failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-screen w-full max-w-5xl items-center justify-center px-6">
      <div className="grid w-full gap-8 lg:grid-cols-[1.15fr_0.85fr]">
        <div>
          <PageHeader eyebrow="Tailbook" title="Admin Web MVP" description="Stage 9 admin workspace for CRM, pets, catalog, pricing, groomers, booking, appointments and visits." />
          <div className="mt-6 rounded-3xl border border-slate-800 bg-slate-900/60 p-6 text-sm text-slate-300">
            <p>Use the bootstrap admin credentials from the API seed to enter the admin workspace.</p>
            <p className="mt-3 text-slate-400">Default seed: admin@tailbook.local / Admin12345!</p>
          </div>
        </div>
        <Card title="Sign in" description="JWT login against the modular monolith API.">
          <form className="space-y-4" onSubmit={submit}>
            <ErrorBanner message={error} />
            <Field label="Email"><Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></Field>
            <Field label="Password"><Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></Field>
            <PrimaryButton type="submit" disabled={isSubmitting} className="w-full">{isSubmitting ? "Signing in…" : "Sign in"}</PrimaryButton>
          </form>
        </Card>
      </div>
    </div>
  );
}

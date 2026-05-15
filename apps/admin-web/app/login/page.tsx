"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, publicApiRequest } from "@/lib/api";
import { createAdminBrowserSessionRequest, setAdminSession } from "@/lib/auth";
import type { AuthenticatedLoginResponse, LoginResponse, MfaChallenge } from "@/lib/types";
import { Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, SecondaryButton } from "@/components/ui";

type MfaVerificationMode = "code" | "recoveryCode";

function formatChallengeExpiry(value: string) {
  const expiresAt = new Date(value);
  if (Number.isNaN(expiresAt.getTime())) return null;

  return new Intl.DateTimeFormat(undefined, {
    hour: "numeric",
    minute: "2-digit"
  }).format(expiresAt);
}

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@tailbook.local");
  const [password, setPassword] = useState("MyV3ryC00lAdminP@ss");
  const [mfaChallenge, setMfaChallenge] = useState<MfaChallenge | null>(null);
  const [challengeEmail, setChallengeEmail] = useState<string | null>(null);
  const [mfaCode, setMfaCode] = useState("");
  const [trustDevice, setTrustDevice] = useState(false);
  const [mfaRecoveryCode, setMfaRecoveryCode] = useState("");
  const [mfaVerificationMode, setMfaVerificationMode] = useState<MfaVerificationMode>("code");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isVerifying, setIsVerifying] = useState(false);

  function completeLogin(response: AuthenticatedLoginResponse) {
    setAdminSession({ accessToken: response.accessToken, refreshToken: response.refreshToken, email: response.user.email, displayName: response.user.displayName });
    router.replace("/clients");
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);
    setMfaChallenge(null);
    setMfaCode("");
    setMfaRecoveryCode("");
    setMfaVerificationMode("code");
    try {
      const response = await publicApiRequest<LoginResponse>("/api/identity/auth/login", {
        ...createAdminBrowserSessionRequest({
          method: "POST",
          body: JSON.stringify({ email, password })
        })
      });

      if (response.status === "MfaRequired") {
        setMfaChallenge(response.mfaChallenge);
        setChallengeEmail(email);
        return;
      }

      completeLogin(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Login failed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function verifyMfa(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!mfaChallenge) return;

    setIsVerifying(true);
    setError(null);
    try {
      const isRecoveryCode = mfaVerificationMode === "recoveryCode";
      const requestBody = isRecoveryCode
        ? { challengeId: mfaChallenge.challengeId, recoveryCode: mfaRecoveryCode, trustDevice }
        : { challengeId: mfaChallenge.challengeId, code: mfaCode, trustDevice };

      const response = await publicApiRequest<LoginResponse>(isRecoveryCode ? "/api/identity/auth/mfa/recovery-code/verify" : "/api/identity/auth/mfa/verify", {
        ...createAdminBrowserSessionRequest({
          method: "POST",
          body: JSON.stringify(requestBody)
        })
      });

      if (response.status !== "Authenticated") {
        setError("Verification did not return a session.");
        return;
      }

      completeLogin(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Verification failed.");
    } finally {
      setIsVerifying(false);
    }
  }

  function resetChallenge() {
    setMfaChallenge(null);
    setMfaCode("");
    setMfaRecoveryCode("");
    setMfaVerificationMode("code");
    setError(null);
  }

  function setVerificationMode(mode: MfaVerificationMode) {
    setMfaVerificationMode(mode);
    setError(null);
  }

  const challengeExpiry = mfaChallenge ? formatChallengeExpiry(mfaChallenge.expiresAt) : null;

  return (
    <div className="mx-auto flex min-h-screen w-full max-w-5xl items-center justify-center px-6">
      <div className="grid w-full gap-8 lg:grid-cols-[1.15fr_0.85fr]">
        <div>
          <PageHeader eyebrow="Tailbook" title="Admin Web MVP" description="Stage 9 admin workspace for CRM, pets, catalog, pricing, groomers, booking, appointments and visits." />
          <div className="mt-6 rounded-3xl border border-slate-800 bg-slate-900/60 p-6 text-sm text-slate-300">
            <p>Use the bootstrap admin credentials from the API seed to enter the admin workspace.</p>
            <p className="mt-3 text-slate-400">Default seed: admin@tailbook.local / MyV3ryC00lAdminP@ss</p>
          </div>
        </div>
        <Card title={mfaChallenge ? "Enter verification code" : "Sign in"} description={mfaChallenge ? `Code sent to ${challengeEmail ?? email}.` : "Identity login against the modular monolith API."}>
          {mfaChallenge ? (
            <form className="space-y-4" onSubmit={verifyMfa}>
              <ErrorBanner message={error} />
              {challengeExpiry ? <p className="text-sm text-slate-400">Code expires at {challengeExpiry}.</p> : null}
              <div className="grid grid-cols-2 rounded-2xl border border-slate-800 bg-slate-950 p-1 text-sm">
                <button
                  type="button"
                  aria-pressed={mfaVerificationMode === "code"}
                  onClick={() => setVerificationMode("code")}
                  className={`rounded-xl px-3 py-2 font-medium transition ${mfaVerificationMode === "code" ? "bg-emerald-500 text-slate-950" : "text-slate-300 hover:text-white"}`}
                >
                  Code
                </button>
                <button
                  type="button"
                  aria-pressed={mfaVerificationMode === "recoveryCode"}
                  onClick={() => setVerificationMode("recoveryCode")}
                  className={`rounded-xl px-3 py-2 font-medium transition ${mfaVerificationMode === "recoveryCode" ? "bg-emerald-500 text-slate-950" : "text-slate-300 hover:text-white"}`}
                >
                  Recovery code
                </button>
              </div>
              {mfaVerificationMode === "code" ? (
                <Field label="Verification code">
                  <Input
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    pattern="[0-9]*"
                    value={mfaCode}
                    onChange={(e) => setMfaCode(e.target.value)}
                    required
                  />
                </Field>
              ) : (
                <Field label="Recovery code">
                  <Input
                    autoComplete="off"
                    value={mfaRecoveryCode}
                    onChange={(e) => setMfaRecoveryCode(e.target.value)}
                    required
                  />
                </Field>
              )}
              <label className="flex items-center gap-2 text-sm text-slate-300">
                <input type="checkbox" checked={trustDevice} onChange={(e) => setTrustDevice(e.target.checked)} className="h-4 w-4" />
                Trust this device for 30 days
              </label>
              <div className="flex flex-col gap-3 sm:flex-row">
                <PrimaryButton type="submit" disabled={isVerifying} className="w-full">{isVerifying ? "Verifying..." : mfaVerificationMode === "recoveryCode" ? "Verify recovery code" : "Verify"}</PrimaryButton>
                <SecondaryButton type="button" disabled={isVerifying} className="w-full" onClick={resetChallenge}>Back</SecondaryButton>
              </div>
            </form>
          ) : (
            <form className="space-y-4" onSubmit={submit}>
              <ErrorBanner message={error} />
              <Field label="Email"><Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></Field>
              <Field label="Password"><Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required /></Field>
              <PrimaryButton type="submit" disabled={isSubmitting} className="w-full">{isSubmitting ? "Signing in..." : "Sign in"}</PrimaryButton>
            </form>
          )}
        </Card>
      </div>
    </div>
  );
}

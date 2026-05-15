"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, publicApiRequest } from "@/lib/api";
import { createGroomerBrowserSessionRequest, getStoredAccessToken, storeSession } from "@/lib/auth";
import type { AuthenticatedLoginResponse, LoginResponse, MfaChallenge } from "@/lib/types";

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
    const [email, setEmail] = useState("groomer@tailbook.local");
    const [password, setPassword] = useState("Groomer123!");
    const [mfaChallenge, setMfaChallenge] = useState<MfaChallenge | null>(null);
    const [challengeEmail, setChallengeEmail] = useState<string | null>(null);
    const [mfaCode, setMfaCode] = useState("");
    const [trustDevice, setTrustDevice] = useState(false);
    const [mfaRecoveryCode, setMfaRecoveryCode] = useState("");
    const [mfaVerificationMode, setMfaVerificationMode] = useState<MfaVerificationMode>("code");
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isVerifying, setIsVerifying] = useState(false);

    const helpText = useMemo(
        () => "Use a groomer-linked IAM user to enter the appointment and visit workspace.",
        []
    );

    useEffect(() => {
        if (getStoredAccessToken()) {
            router.replace("/appointments");
        }
    }, [router]);

    function completeLogin(response: AuthenticatedLoginResponse) {
        storeSession(response.accessToken, response.user.email, response.user.displayName, response.refreshToken);
        router.push("/appointments");
    }

    async function onSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setIsLoading(true);
        setMfaChallenge(null);
        setMfaCode("");
        setMfaRecoveryCode("");
        setMfaVerificationMode("code");

        try {
            const response = await publicApiRequest<LoginResponse>("/api/identity/auth/login", {
                ...createGroomerBrowserSessionRequest({
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
            setIsLoading(false);
        }
    }

    async function onVerifyMfa(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        if (!mfaChallenge) return;

        setError(null);
        setIsVerifying(true);

        try {
            const isRecoveryCode = mfaVerificationMode === "recoveryCode";
            const requestBody = isRecoveryCode
                ? { challengeId: mfaChallenge.challengeId, recoveryCode: mfaRecoveryCode, trustDevice }
                : { challengeId: mfaChallenge.challengeId, code: mfaCode, trustDevice };

            const response = await publicApiRequest<LoginResponse>(isRecoveryCode ? "/api/identity/auth/mfa/recovery-code/verify" : "/api/identity/auth/mfa/verify", {
                ...createGroomerBrowserSessionRequest({
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
        <main className="mx-auto flex min-h-screen max-w-xl flex-col justify-center px-6 py-16">
            <div className="rounded-3xl border border-slate-800 bg-slate-900/70 p-8 shadow-2xl shadow-black/20">
                <div className="space-y-2">
                    <span className="inline-flex rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-xs uppercase tracking-wide text-emerald-300">
                        Groomer login
                    </span>
                    <h1 className="text-3xl font-semibold">Sign in</h1>
                    <p className="text-sm text-slate-300">{helpText}</p>
                </div>

                {mfaChallenge ? (
                    <form className="mt-8 space-y-5" onSubmit={onVerifyMfa}>
                        <div className="rounded-2xl border border-slate-700 bg-slate-950/70 px-4 py-3 text-sm text-slate-300">
                            <p>Code sent to {challengeEmail ?? email}.</p>
                            {challengeExpiry ? <p className="mt-1 text-slate-400">Code expires at {challengeExpiry}.</p> : null}
                        </div>

                        <div className="grid grid-cols-2 rounded-2xl border border-slate-700 bg-slate-950 p-1 text-sm">
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
                            <label className="block space-y-2">
                                <span className="text-sm text-slate-300">Verification code</span>
                                <input
                                    inputMode="numeric"
                                    autoComplete="one-time-code"
                                    pattern="[0-9]*"
                                    value={mfaCode}
                                    onChange={(event) => setMfaCode(event.target.value)}
                                    className="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 outline-none ring-0 transition focus:border-emerald-500"
                                    required
                                />
                            </label>
                        ) : (
                            <label className="block space-y-2">
                                <span className="text-sm text-slate-300">Recovery code</span>
                                <input
                                    autoComplete="off"
                                    value={mfaRecoveryCode}
                                    onChange={(event) => setMfaRecoveryCode(event.target.value)}
                                    className="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 outline-none ring-0 transition focus:border-emerald-500"
                                    required
                                />
                            </label>
                        )}

                        {error ? <p className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{error}</p> : null}

                        <label className="flex items-center gap-2 text-sm text-slate-300">
                            <input type="checkbox" checked={trustDevice} onChange={(e) => setTrustDevice(e.target.checked)} className="h-4 w-4" />
                            Trust this device for 30 days
                        </label>

                        <div className="flex flex-col gap-3 sm:flex-row">
                            <button type="submit" disabled={isVerifying} className="inline-flex w-full items-center justify-center rounded-2xl bg-emerald-500 px-4 py-3 font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60">
                                {isVerifying ? "Verifying..." : mfaVerificationMode === "recoveryCode" ? "Verify recovery code" : "Verify"}
                            </button>
                            <button type="button" disabled={isVerifying} onClick={resetChallenge} className="inline-flex w-full items-center justify-center rounded-2xl border border-slate-700 bg-slate-900 px-4 py-3 font-medium text-slate-100 transition hover:border-slate-500 disabled:cursor-not-allowed disabled:opacity-60">
                                Back
                            </button>
                        </div>
                    </form>
                ) : (
                    <form className="mt-8 space-y-5" onSubmit={onSubmit}>
                        <label className="block space-y-2">
                            <span className="text-sm text-slate-300">Email</span>
                            <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} className="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 outline-none ring-0 transition focus:border-emerald-500" required />
                        </label>

                        <label className="block space-y-2">
                            <span className="text-sm text-slate-300">Password</span>
                            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 outline-none ring-0 transition focus:border-emerald-500" required />
                        </label>

                        {error ? <p className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{error}</p> : null}

                        <button type="submit" disabled={isLoading} className="inline-flex w-full items-center justify-center rounded-2xl bg-emerald-500 px-4 py-3 font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60">
                            {isLoading ? "Signing in..." : "Sign in"}
                        </button>
                    </form>
                )}
            </div>
        </main>
    );
}

"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { getStoredAccessToken, storeSession } from "@/lib/auth";

type LoginResponse = {
    accessToken: string;
    expiresAtUtc: string;
    refreshToken: string;
    refreshTokenExpiresAtUtc: string;
    user: {
        id: string;
        email: string;
        displayName: string;
        roles: string[];
        permissions: string[];
    };
};

export default function LoginPage() {
    const router = useRouter();
    const [email, setEmail] = useState("groomer@tailbook.local");
    const [password, setPassword] = useState("Groomer123!");
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);

    const helpText = useMemo(
        () => "Use a groomer-linked IAM user. The frontend stores the issued JWT in localStorage for MVP-only local development.",
        []
    );

    useEffect(() => {
        if (getStoredAccessToken()) {
            router.replace("/appointments");
        }
    }, [router]);

    async function onSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setIsLoading(true);

        try {
            const response = await apiRequest<LoginResponse>("/api/identity/auth/login", {
                method: "POST",
                body: JSON.stringify({ email, password })
            });

            storeSession(response.accessToken, response.user.email, response.user.displayName, response.refreshToken);
            router.push("/appointments");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Login failed.");
        } finally {
            setIsLoading(false);
        }
    }

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

                <form className="mt-8 space-y-5" onSubmit={onSubmit}>
                    <label className="block space-y-2">
                        <span className="text-sm text-slate-300">Email</span>
                        <input value={email} onChange={(event) => setEmail(event.target.value)} className="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 outline-none ring-0 transition focus:border-emerald-500" />
                    </label>

                    <label className="block space-y-2">
                        <span className="text-sm text-slate-300">Password</span>
                        <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} className="w-full rounded-2xl border border-slate-700 bg-slate-950 px-4 py-3 outline-none ring-0 transition focus:border-emerald-500" />
                    </label>

                    {error ? <p className="rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{error}</p> : null}

                    <button type="submit" disabled={isLoading} className="inline-flex w-full items-center justify-center rounded-2xl bg-emerald-500 px-4 py-3 font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60">
                        {isLoading ? "Signing in..." : "Sign in"}
                    </button>
                </form>
            </div>
        </main>
    );
}

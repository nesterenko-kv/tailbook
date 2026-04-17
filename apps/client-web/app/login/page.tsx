"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { storeSession } from "@/lib/auth";
import type { ClientLoginResponse } from "@/lib/types";
import { Button, Card, Input } from "@/components/ui";

export default function LoginPage() {
    const router = useRouter();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    async function onSubmit(event: FormEvent) {
        event.preventDefault();
        setBusy(true);
        setError(null);

        try {
            const response = await apiRequest<ClientLoginResponse>("/api/client/auth/login", {
                method: "POST",
                body: JSON.stringify({ email, password })
            });

            storeSession(response.accessToken, response.user.email);
            router.push("/book");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Unable to sign in.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <main className="mx-auto flex min-h-screen max-w-md items-center px-6 py-16">
            <Card className="w-full space-y-6">
                <div>
                    <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Tailbook</p>
                    <h1 className="mt-2 text-2xl font-semibold">Client portal sign in</h1>
                    <p className="mt-2 text-sm text-slate-400">Use your client portal email and password.</p>
                </div>
                <form className="space-y-4" onSubmit={onSubmit}>
                    <Input value={email} onChange={(event) => setEmail(event.target.value)} placeholder="Email" type="email" />
                    <Input value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Password" type="password" />
                    {error ? <p className="text-sm text-rose-300">{error}</p> : null}
                    <Button disabled={busy} type="submit" className="w-full">{busy ? "Signing in..." : "Sign in"}</Button>
                </form>
                <p className="text-sm text-slate-400">
                    No account yet? <Link href="/register" className="text-emerald-300 underline">Create one</Link>
                </p>
            </Card>
        </main>
    );
}

"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { storeSession } from "@/lib/auth";
import type { ClientLoginResponse } from "@/lib/types";
import { Button, Card, Input } from "@/components/ui";

export default function RegisterPage() {
    const router = useRouter();
    const [form, setForm] = useState({ displayName: "", firstName: "", lastName: "", email: "", password: "", phone: "", instagram: "" });
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    async function onSubmit(event: FormEvent) {
        event.preventDefault();
        setBusy(true);
        setError(null);

        try {
            const response = await apiRequest<ClientLoginResponse>("/api/client/auth/register", {
                method: "POST",
                body: JSON.stringify(form)
            });
            storeSession(response.accessToken, response.user.email);
            router.push("/book");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Unable to register.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <main className="mx-auto flex min-h-screen max-w-2xl items-center px-6 py-16">
            <Card className="w-full space-y-6">
                <div>
                    <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Tailbook</p>
                    <h1 className="mt-2 text-2xl font-semibold">Create client portal account</h1>
                    <p className="mt-2 text-sm text-slate-400">This creates your portal login and a linked client/contact profile for MVP.</p>
                </div>
                <form className="grid gap-4 md:grid-cols-2" onSubmit={onSubmit}>
                    <Input value={form.displayName} onChange={(e) => setForm({ ...form, displayName: e.target.value })} placeholder="Display name" />
                    <Input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} placeholder="First name" />
                    <Input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} placeholder="Last name" />
                    <Input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} placeholder="Email" type="email" />
                    <Input value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} placeholder="Password" type="password" />
                    <Input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} placeholder="Phone (optional)" />
                    <div className="md:col-span-2">
                        <Input value={form.instagram} onChange={(e) => setForm({ ...form, instagram: e.target.value })} placeholder="Instagram handle (optional)" />
                    </div>
                    {error ? <p className="md:col-span-2 text-sm text-rose-300">{error}</p> : null}
                    <div className="md:col-span-2 flex items-center justify-between gap-4">
                        <p className="text-sm text-slate-400">Already registered? <Link href="/login" className="text-emerald-300 underline">Sign in</Link></p>
                        <Button disabled={busy} type="submit">{busy ? "Creating..." : "Create account"}</Button>
                    </div>
                </form>
            </Card>
        </main>
    );
}

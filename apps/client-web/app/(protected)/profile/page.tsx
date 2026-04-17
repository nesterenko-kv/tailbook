"use client";

import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import type { ClientContactPreferences } from "@/lib/types";
import { Button, Card, Input } from "@/components/ui";

export default function ProfilePage() {
    const [profile, setProfile] = useState<ClientContactPreferences | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    useEffect(() => {
        apiRequest<ClientContactPreferences>("/api/client/me/contact-preferences")
            .then(setProfile)
            .catch((err: Error) => setError(err.message));
    }, []);

    async function onSubmit(event: FormEvent) {
        event.preventDefault();
        if (!profile) {
            return;
        }

        setError(null);
        setMessage(null);

        try {
            const updated = await apiRequest<ClientContactPreferences>("/api/client/me/contact-preferences", {
                method: "PATCH",
                body: JSON.stringify({ methods: profile.methods.map((method) => ({ methodType: method.methodType, value: method.displayValue, isPreferred: method.isPreferred, notes: method.notes })) })
            });
            setProfile(updated);
            setMessage("Contact preferences saved.");
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Unable to update contact preferences.");
        }
    }

    return (
        <section className="space-y-6">
            <div>
                <p className="text-xs uppercase tracking-[0.2em] text-emerald-300">Profile</p>
                <h2 className="mt-2 text-3xl font-semibold">Contact preferences</h2>
                <p className="mt-2 text-sm text-slate-400">Update the methods the salon can use for your own profile.</p>
            </div>
            {!profile ? <p className="text-sm text-slate-400">Loading profile…</p> : (
                <Card>
                    <form className="space-y-4" onSubmit={onSubmit}>
                        {profile.methods.map((method, index) => (
                            <div key={method.id} className="grid gap-2 rounded-xl border border-slate-800 p-4">
                                <p className="text-sm font-medium text-slate-200">{method.methodType}</p>
                                <Input value={method.displayValue} onChange={(event) => {
                                    const methods = [...profile.methods];
                                    methods[index] = { ...method, displayValue: event.target.value };
                                    setProfile({ ...profile, methods });
                                }} />
                                <label className="flex items-center gap-2 text-sm text-slate-300">
                                    <input type="checkbox" checked={method.isPreferred} onChange={(event) => {
                                        const methods = profile.methods.map((item, itemIndex) => ({ ...item, isPreferred: itemIndex === index ? event.target.checked : false }));
                                        setProfile({ ...profile, methods });
                                    }} />
                                    Preferred method
                                </label>
                            </div>
                        ))}
                        {message ? <p className="text-sm text-emerald-300">{message}</p> : null}
                        {error ? <p className="text-sm text-rose-300">{error}</p> : null}
                        <div className="flex justify-end"><Button type="submit">Save</Button></div>
                    </form>
                </Card>
            )}
        </section>
    );
}

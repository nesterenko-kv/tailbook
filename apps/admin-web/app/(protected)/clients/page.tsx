"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { ClientListItem, PagedResult } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, SuccessBanner, TextArea } from "@/components/ui";

export default function ClientsPage() {
    const [clients, setClients] = useState<ClientListItem[]>([]);
    const [search, setSearch] = useState("");
    const [displayName, setDisplayName] = useState("");
    const [notes, setNotes] = useState("");
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    async function loadClients(term?: string) {
        setIsLoading(true);
        setError(null);
        try {
            const query = new URLSearchParams({ page: "1", pageSize: "50" });
            if (term) {
                query.set("search", term);
            }
            const response = await apiRequest<PagedResult<ClientListItem>>(`/api/admin/clients?${query.toString()}`);
            setClients(response.items);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load clients.");
        } finally {
            setIsLoading(false);
        }
    }

    useEffect(() => {
        void loadClients();
    }, []);

    async function createClient(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setIsSaving(true);
        setError(null);
        setSuccess(null);

        try {
            await apiRequest("/api/admin/clients", {
                method: "POST",
                body: JSON.stringify({ displayName, notes: notes || null })
            });
            setDisplayName("");
            setNotes("");
            setSuccess("Client created.");
            await loadClients(search);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create client.");
        } finally {
            setIsSaving(false);
        }
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader
                eyebrow="CRM"
                title="Clients"
                description="Create and browse CRM client accounts. Open a client to manage contacts and linked pets."
            />

            <div className="grid gap-6 xl:grid-cols-[1.6fr_1fr]">
                <Card title="Client list" description="Search and open client profiles.">
                    <div className="mb-4 flex gap-3">
                        <Input placeholder="Search by display name" value={search} onChange={(event) => setSearch(event.target.value)} />
                        <PrimaryButton type="button" onClick={() => void loadClients(search)}>Search</PrimaryButton>
                    </div>
                    <ErrorBanner message={error} />
                    <SuccessBanner message={success} />
                    {isLoading ? <p className="text-sm text-slate-300">Loading clients…</p> : null}
                    <div className="grid gap-3">
                        {clients.map((client) => (
                            <Link key={client.id} href={`/clients/${client.id}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <h3 className="font-medium">{client.displayName}</h3>
                                        <p className="mt-1 text-sm text-slate-400">Created {formatDateTime(client.createdAtUtc)}</p>
                                    </div>
                                    <Badge>{client.status}</Badge>
                                </div>
                                <p className="mt-3 text-sm text-slate-300">Contacts: {client.contactCount}</p>
                            </Link>
                        ))}
                        {!isLoading && clients.length === 0 ? <p className="text-sm text-slate-400">No clients found.</p> : null}
                    </div>
                </Card>

                <Card title="Create client" description="Minimal CRM account creation for front desk/admin.">
                    <form className="space-y-4" onSubmit={createClient}>
                        <Field label="Display name">
                            <Input value={displayName} onChange={(event) => setDisplayName(event.target.value)} maxLength={200} required />
                        </Field>
                        <Field label="Notes">
                            <TextArea value={notes} onChange={(event) => setNotes(event.target.value)} maxLength={2000} />
                        </Field>
                        <PrimaryButton type="submit" disabled={isSaving} className="w-full">
                            {isSaving ? "Creating…" : "Create client"}
                        </PrimaryButton>
                    </form>
                </Card>
            </div>
        </div>
    );
}

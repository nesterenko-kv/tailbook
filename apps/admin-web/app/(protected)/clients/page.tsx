"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { ClientListItem, PagedResult } from "@/lib/types";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, LoadingState, PageHeader, PrimaryButton, SecondaryButton, SuccessBanner, TextArea } from "@/components/ui";
import { Pagination } from "@/components/pagination";
import { SortControl } from "@/components/sort-control";

export default function ClientsPage() {
    const [clientResult, setClientResult] = useState<PagedResult<ClientListItem> | null>(null);
    const [search, setSearch] = useState("");
    const [editingId, setEditingId] = useState<string | null>(null);
    const [editName, setEditName] = useState("");
    const [page, setPage] = useState(1);
    const [sortBy, setSortBy] = useState("displayName");
    const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");
    const [displayName, setDisplayName] = useState("");
    const [notes, setNotes] = useState("");
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    async function loadClients(term: string | undefined, pageNum: number, sortField?: string, sortDir?: string) {
        setIsLoading(true);
        setError(null);
        try {
            const query = new URLSearchParams({ page: String(pageNum), pageSize: "50" });
            if (term) {
                query.set("search", term);
            }
            query.set("sortBy", sortField ?? sortBy);
            query.set("sortDirection", sortDir ?? sortDirection);
            const response = await apiRequest<PagedResult<ClientListItem>>(`/api/admin/clients?${query.toString()}`);
            setClientResult(response);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load clients.");
        } finally {
            setIsLoading(false);
        }
    }

    useEffect(() => {
        void loadClients(undefined, 1, sortBy, sortDirection);
    }, [sortBy, sortDirection]);

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
            await loadClients(search, 1, sortBy, sortDirection);
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

            <ErrorBanner message={error} />
            <SuccessBanner message={success} />
            <div className="grid gap-6 xl:grid-cols-[1.6fr_1fr]">
                <Card title="Client list" description="Search and open client profiles.">
                    <div className="mb-4 flex gap-3">
                        <Input placeholder="Search by display name" value={search} onChange={(event) => setSearch(event.target.value)} />
                        <PrimaryButton type="button" onClick={() => { setPage(1); void loadClients(search, 1, sortBy, sortDirection); }}>Search</PrimaryButton>
                    </div>
                    {isLoading ? <LoadingState label="Loading clients..." /> : null}
                    <SortControl
                      sortBy={sortBy}
                      sortDirection={sortDirection}
                      options={[
                        { value: "displayName", label: "Display name" },
                        { value: "createdAt", label: "Created date" },
                        { value: "updatedAt", label: "Updated date" },
                      ]}
                      onSortByChange={(v) => { setSortBy(v); setPage(1); }}
                      onSortDirectionChange={(v) => { setSortDirection(v); setPage(1); }}
                    />
                    <div className="grid gap-3">
                        {clientResult?.items.map((client) => (
                            <div key={client.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
                                <div className="flex items-start justify-between gap-4">
                                    <div className="flex-1">
                                        {editingId === client.id ? (
                                            <div className="flex items-center gap-2">
                                                <Input
                                                    value={editName}
                                                    onChange={(e) => setEditName(e.target.value)}
                                                    maxLength={200}
                                                    className="flex-1"
                                                    autoFocus
                                                    onKeyDown={(e) => {
                                                        if (e.key === "Escape") setEditingId(null);
                                                        if (e.key === "Enter" && editName.trim()) {
                                                            apiRequest(`/api/admin/clients/${client.id}`, {
                                                                method: "PUT",
                                                                body: JSON.stringify({ displayName: editName.trim() })
                                                            }).then(() => {
                                                                setEditingId(null);
                                                                void loadClients(search, page, sortBy, sortDirection);
                                                            }).catch(() => setEditingId(null));
                                                        }
                                                    }}
                                                />
                                                <SecondaryButton
                                                    type="button"
                                                    className="px-3 py-1 text-xs"
                                                    onClick={() => {
                                                        if (editName.trim()) {
                                                            apiRequest(`/api/admin/clients/${client.id}`, {
                                                                method: "PUT",
                                                                body: JSON.stringify({ displayName: editName.trim() })
                                                            }).then(() => {
                                                                setEditingId(null);
                                                                void loadClients(search, page, sortBy, sortDirection);
                                                            }).catch(() => setEditingId(null));
                                                        }
                                                    }}
                                                >
                                                    Save
                                                </SecondaryButton>
                                                <SecondaryButton type="button" className="px-3 py-1 text-xs" onClick={() => setEditingId(null)}>Cancel</SecondaryButton>
                                            </div>
                                        ) : (
                                            <button
                                                type="button"
                                                className="group flex items-center gap-2"
                                                onClick={() => { setEditingId(client.id); setEditName(client.displayName); }}
                                            >
                                                <h3 className="font-medium text-left">{client.displayName}</h3>
                                                <span className="text-xs text-slate-600 opacity-0 transition group-hover:opacity-100">✎</span>
                                            </button>
                                        )}
                                        <p className="mt-1 text-sm text-slate-400">Created {formatDateTime(client.createdAt)}</p>
                                    </div>
                                    <Badge>{client.status}</Badge>
                                </div>
                                <p className="mt-3 text-sm text-slate-300">Contacts: {client.contactCount}</p>
                            </div>
                        ))}
                        {!isLoading && (!clientResult || clientResult.items.length === 0) ? <EmptyState title="No clients found" description="Create a client or adjust search." /> : null}
                    </div>
                    {clientResult ? <Pagination page={page} pageSize={50} totalCount={clientResult.totalCount} onPageChange={(p) => { setPage(p); void loadClients(search, p, sortBy, sortDirection); }} /> : null}
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

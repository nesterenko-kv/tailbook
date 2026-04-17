"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import type { ClientDetail } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, LinkButton, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

export default function ClientDetailPage() {
    const params = useParams<{ clientId: string }>();
    const clientId = String(params.clientId);

    const [client, setClient] = useState<ClientDetail | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [contactForm, setContactForm] = useState({ firstName: "", lastName: "", notes: "", trustLevel: "Normal" });
    const [methodFormByContact, setMethodFormByContact] = useState<Record<string, { methodType: string; value: string; displayValue: string; isPreferred: boolean; verificationStatus: string; notes: string }>>({});

    async function loadClient() {
        setIsLoading(true);
        setError(null);
        try {
            const response = await apiRequest<ClientDetail>(`/api/admin/clients/${clientId}`);
            setClient(response);
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load client.");
        } finally {
            setIsLoading(false);
        }
    }

    useEffect(() => {
        void loadClient();
    }, [clientId]);

    function getMethodForm(contactId: string) {
        return methodFormByContact[contactId] ?? {
            methodType: "Instagram",
            value: "",
            displayValue: "",
            isPreferred: false,
            verificationStatus: "Unverified",
            notes: ""
        };
    }

    async function addContact(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/clients/${clientId}/contacts`, {
                method: "POST",
                body: JSON.stringify({
                    clientId,
                    firstName: contactForm.firstName,
                    lastName: contactForm.lastName || null,
                    notes: contactForm.notes || null,
                    trustLevel: contactForm.trustLevel || null
                })
            });
            setContactForm({ firstName: "", lastName: "", notes: "", trustLevel: "Normal" });
            setSuccess("Contact added.");
            await loadClient();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to add contact.");
        }
    }

    async function addMethod(contactId: string, event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const state = getMethodForm(contactId);
        setError(null);
        setSuccess(null);
        try {
            await apiRequest(`/api/admin/contacts/${contactId}/methods`, {
                method: "POST",
                body: JSON.stringify({
                    contactId,
                    methodType: state.methodType,
                    value: state.value,
                    displayValue: state.displayValue || null,
                    isPreferred: state.isPreferred,
                    verificationStatus: state.verificationStatus || null,
                    notes: state.notes || null
                })
            });
            setMethodFormByContact((current) => ({
                ...current,
                [contactId]: { methodType: "Instagram", value: "", displayValue: "", isPreferred: false, verificationStatus: "Unverified", notes: "" }
            }));
            setSuccess("Contact method added.");
            await loadClient();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to add contact method.");
        }
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader
                eyebrow="CRM detail"
                title={client?.displayName ?? "Client detail"}
                description="Admin-only customer contact view. Use this page to manage contacts and navigate to pets."
                action={<LinkButton href="/clients">Back to clients</LinkButton>}
            />

            <ErrorBanner message={error} />
            <SuccessBanner message={success} />
            {isLoading ? <p className="text-sm text-slate-300">Loading client…</p> : null}

            {client ? (
                <>
                    <div className="grid gap-6 xl:grid-cols-[1.3fr_1fr]">
                        <Card title="Client profile">
                            <div className="space-y-3 text-sm text-slate-300">
                                <div className="flex items-center gap-3"><Badge>{client.status}</Badge></div>
                                <p>{client.notes || "No notes."}</p>
                                <p>Pets linked: {client.pets.length}</p>
                            </div>
                        </Card>

                        <Card title="Add contact person">
                            <form className="space-y-4" onSubmit={addContact}>
                                <Field label="First name"><Input value={contactForm.firstName} onChange={(event) => setContactForm((current) => ({ ...current, firstName: event.target.value }))} required /></Field>
                                <Field label="Last name"><Input value={contactForm.lastName} onChange={(event) => setContactForm((current) => ({ ...current, lastName: event.target.value }))} /></Field>
                                <Field label="Trust level"><Input value={contactForm.trustLevel} onChange={(event) => setContactForm((current) => ({ ...current, trustLevel: event.target.value }))} /></Field>
                                <Field label="Notes"><TextArea value={contactForm.notes} onChange={(event) => setContactForm((current) => ({ ...current, notes: event.target.value }))} /></Field>
                                <PrimaryButton type="submit" className="w-full">Add contact</PrimaryButton>
                            </form>
                        </Card>
                    </div>

                    <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
                        <Card title="Contacts">
                            <div className="grid gap-4">
                                {client.contacts.map((contact) => {
                                    const formState = getMethodForm(contact.id);
                                    return (
                                        <article key={contact.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                            <div className="flex items-start justify-between gap-3">
                                                <div>
                                                    <h3 className="font-medium">{contact.firstName} {contact.lastName ?? ""}</h3>
                                                    <p className="text-sm text-slate-400">Trust: {contact.trustLevel}</p>
                                                </div>
                                                <Badge>{contact.methods.length} methods</Badge>
                                            </div>
                                            {contact.notes ? <p className="mt-3 text-sm text-slate-300">{contact.notes}</p> : null}
                                            <div className="mt-3 flex flex-wrap gap-2">
                                                {contact.methods.map((method) => (
                                                    <span key={method.id} className="rounded-full border border-slate-700 px-3 py-1 text-xs text-slate-200">{method.methodType}: {method.displayValue}</span>
                                                ))}
                                            </div>
                                            <form className="mt-4 grid gap-3 md:grid-cols-2" onSubmit={(event) => void addMethod(contact.id, event)}>
                                                <Field label="Method type">
                                                    <Select value={formState.methodType} onChange={(event) => setMethodFormByContact((current) => ({ ...current, [contact.id]: { ...formState, methodType: event.target.value } }))}>
                                                        <option value="Instagram">Instagram</option>
                                                        <option value="Phone">Phone</option>
                                                        <option value="Email">Email</option>
                                                        <option value="Other">Other</option>
                                                    </Select>
                                                </Field>
                                                <Field label="Normalized value"><Input value={formState.value} onChange={(event) => setMethodFormByContact((current) => ({ ...current, [contact.id]: { ...formState, value: event.target.value } }))} required /></Field>
                                                <Field label="Display value"><Input value={formState.displayValue} onChange={(event) => setMethodFormByContact((current) => ({ ...current, [contact.id]: { ...formState, displayValue: event.target.value } }))} /></Field>
                                                <Field label="Verification status"><Input value={formState.verificationStatus} onChange={(event) => setMethodFormByContact((current) => ({ ...current, [contact.id]: { ...formState, verificationStatus: event.target.value } }))} /></Field>
                                                <Field label="Notes"><Input value={formState.notes} onChange={(event) => setMethodFormByContact((current) => ({ ...current, [contact.id]: { ...formState, notes: event.target.value } }))} /></Field>
                                                <label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={formState.isPreferred} onChange={(event) => setMethodFormByContact((current) => ({ ...current, [contact.id]: { ...formState, isPreferred: event.target.checked } }))} /> Preferred</label>
                                                <PrimaryButton type="submit" className="md:col-span-2">Add contact method</PrimaryButton>
                                            </form>
                                        </article>
                                    );
                                })}
                                {client.contacts.length === 0 ? <p className="text-sm text-slate-400">No contacts yet.</p> : null}
                            </div>
                        </Card>

                        <Card title="Linked pets">
                            <div className="grid gap-3">
                                {client.pets.map((pet) => (
                                    <Link key={pet.id} href={`/pets/${pet.id}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
                                        <h3 className="font-medium">{pet.name}</h3>
                                        <p className="mt-1 text-sm text-slate-400">{pet.animalTypeName} • {pet.breedName}</p>
                                    </Link>
                                ))}
                                {client.pets.length === 0 ? <p className="text-sm text-slate-400">No pets linked yet. Use the Pets page to register one for this client.</p> : null}
                            </div>
                        </Card>
                    </div>
                </>
            ) : null}
        </div>
    );
}

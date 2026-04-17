"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime, formatMoney } from "@/lib/format";
import { unwrapItems } from "@/lib/contracts";
import type { AppointmentListItem, ClientDetail, GroomerListItem, GroomerListResponse, OfferListItem, PagedResult } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, Select, SuccessBanner } from "@/components/ui";

export default function AppointmentsPage() {
  const [appointments, setAppointments] = useState<AppointmentListItem[]>([]);
  const [clients, setClients] = useState<{ id: string; displayName: string }[]>([]);
  const [selectedClient, setSelectedClient] = useState<ClientDetail | null>(null);
  const [offers, setOffers] = useState<OfferListItem[]>([]);
  const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState({ clientId: "", petId: "", groomerId: "", startAtUtc: "", offerId: "" });

  async function loadBase() {
    setError(null);
    try {
      const [appointmentResponse, clientResponse, offerResponse, groomerResponse] = await Promise.all([
        apiRequest<PagedResult<AppointmentListItem>>("/api/admin/appointments?page=1&pageSize=50"),
        apiRequest<PagedResult<{ id: string; displayName: string }>>("/api/admin/clients?page=1&pageSize=100"),
        apiRequest<OfferListItem[]>("/api/admin/catalog/offers"),
        apiRequest<GroomerListResponse>("/api/admin/groomers")
      ]);
      setAppointments(appointmentResponse.items);
      setClients(clientResponse.items);
      setOffers(offerResponse);
      setGroomers(unwrapItems(groomerResponse));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load appointments.");
    }
  }

  useEffect(() => { void loadBase(); }, []);
  useEffect(() => {
    if (!form.clientId) { setSelectedClient(null); return; }
    apiRequest<ClientDetail>(`/api/admin/clients/${form.clientId}`)
      .then(setSelectedClient)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load client detail."));
  }, [form.clientId]);

  async function createAppointment(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null); setSuccess(null);
    try {
      await apiRequest("/api/admin/appointments", {
        method: "POST",
        body: JSON.stringify({
          petId: form.petId,
          groomerId: form.groomerId,
          startAtUtc: new Date(form.startAtUtc).toISOString(),
          items: form.offerId ? [{ offerId: form.offerId, itemType: null, requestedNotes: null }] : []
        })
      });
      setSuccess("Appointment created.");
      await loadBase();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to create appointment."); }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Appointments" title="Appointments" description="Create direct appointments and manage the confirmed reservation queue." />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} />
      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <Card title="Appointment list">
          <div className="grid gap-3">
            {appointments.map((item) => (
              <Link key={item.id} href={`/appointments/${item.id}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-medium">{item.id}</div>
                    <div className="text-sm text-slate-400">{formatDateTime(item.startAtUtc)}</div>
                  </div>
                  <Badge>{item.status}</Badge>
                </div>
                <div className="mt-2 text-sm text-slate-300">{formatMoney(item.totalAmount)} · items {item.itemCount}</div>
              </Link>
            ))}
          </div>
        </Card>
        <Card title="Create direct appointment">
          <form className="grid gap-4" onSubmit={createAppointment}>
            <Field label="Client"><Select value={form.clientId} onChange={(e)=>setForm(c=>({...c, clientId:e.target.value, petId:""}))} required><option value="">Select client</option>{clients.map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Pet"><Select value={form.petId} onChange={(e)=>setForm(c=>({...c, petId:e.target.value}))} required><option value="">Select pet</option>{selectedClient?.pets.map(x=><option key={x.id} value={x.id}>{x.name} · {x.breedName}</option>)}</Select></Field>
            <Field label="Groomer"><Select value={form.groomerId} onChange={(e)=>setForm(c=>({...c, groomerId:e.target.value}))} required><option value="">Select groomer</option>{groomers.map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Start at"><Input type="datetime-local" value={form.startAtUtc} onChange={(e)=>setForm(c=>({...c, startAtUtc:e.target.value}))} required /></Field>
            <Field label="Offer"><Select value={form.offerId} onChange={(e)=>setForm(c=>({...c, offerId:e.target.value}))} required><option value="">Select offer</option>{offers.map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <PrimaryButton type="submit">Create appointment</PrimaryButton>
          </form>
        </Card>
      </div>
    </div>
  );
}

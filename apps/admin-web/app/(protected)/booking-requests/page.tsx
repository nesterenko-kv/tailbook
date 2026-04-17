"use client";

import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { BookingRequestListItem, ClientDetail, GroomerListItem, OfferListItem, PagedResult } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

export default function BookingRequestsPage() {
  const [requests, setRequests] = useState<BookingRequestListItem[]>([]);
  const [offers, setOffers] = useState<OfferListItem[]>([]);
  const [clients, setClients] = useState<{ id: string; displayName: string }[]>([]);
  const [selectedClient, setSelectedClient] = useState<ClientDetail | null>(null);
  const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
  const [selectedRequest, setSelectedRequest] = useState<BookingRequestListItem | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState({ clientId: "", petId: "", requestedByContactId: "", channel: "Admin", preferredStartAtUtc: "", preferredEndAtUtc: "", preferredLabel: "Preferred slot", notes: "", offerId: "" });
  const [convertForm, setConvertForm] = useState({ bookingRequestId: "", groomerId: "", startAtUtc: "" });

  async function loadBase() {
    setError(null);
    try {
      const [requestResponse, offerResponse, clientResponse, groomerResponse] = await Promise.all([
        apiRequest<PagedResult<BookingRequestListItem>>("/api/admin/booking-requests?page=1&pageSize=50"),
        apiRequest<OfferListItem[]>("/api/admin/catalog/offers"),
        apiRequest<PagedResult<{ id: string; displayName: string }>>("/api/admin/clients?page=1&pageSize=100"),
        apiRequest<GroomerListItem[]>("/api/admin/groomers")
      ]);
      setRequests(requestResponse.items);
      setOffers(offerResponse);
      setClients(clientResponse.items);
      setGroomers(groomerResponse);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load booking requests.");
    }
  }

  useEffect(() => { void loadBase(); }, []);

  useEffect(() => {
    if (!form.clientId) {
      setSelectedClient(null);
      return;
    }
    apiRequest<ClientDetail>(`/api/admin/clients/${form.clientId}`)
      .then(setSelectedClient)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load client detail."));
  }, [form.clientId]);

  async function createRequest(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    try {
      await apiRequest("/api/admin/booking-requests", {
        method: "POST",
        body: JSON.stringify({
          clientId: form.clientId || null,
          petId: form.petId,
          requestedByContactId: form.requestedByContactId || null,
          channel: form.channel,
          preferredTimes: form.preferredStartAtUtc && form.preferredEndAtUtc
            ? [{ startAtUtc: new Date(form.preferredStartAtUtc).toISOString(), endAtUtc: new Date(form.preferredEndAtUtc).toISOString(), label: form.preferredLabel || null }]
            : [],
          notes: form.notes || null,
          items: form.offerId ? [{ offerId: form.offerId, itemType: null, requestedNotes: null }] : []
        })
      });
      setSuccess("Booking request created.");
      setForm({ clientId: "", petId: "", requestedByContactId: "", channel: "Admin", preferredStartAtUtc: "", preferredEndAtUtc: "", preferredLabel: "Preferred slot", notes: "", offerId: "" });
      setSelectedClient(null);
      await loadBase();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to create booking request.");
    }
  }

  function openRequest(id: string) {
    const request = requests.find((item) => item.id === id) ?? null;
    setSelectedRequest(request);
    setConvertForm((current) => ({ ...current, bookingRequestId: id }));
  }

  async function convertRequest(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    try {
      await apiRequest(`/api/admin/booking-requests/${convertForm.bookingRequestId}/convert`, {
        method: "POST",
        body: JSON.stringify({
          bookingRequestId: convertForm.bookingRequestId,
          groomerId: convertForm.groomerId,
          startAtUtc: new Date(convertForm.startAtUtc).toISOString()
        })
      });
      setSuccess("Booking request converted to appointment.");
      await loadBase();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to convert booking request.");
    }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Booking" title="Booking requests" description="Create booking requests on behalf of clients and convert them into appointments." />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} />

      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <Card title="Booking request list">
          <div className="grid gap-3">
            {requests.map((item) => (
              <button key={item.id} type="button" onClick={() => openRequest(item.id)} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-left transition hover:border-emerald-500/40">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <div className="font-medium">{item.id}</div>
                    <div className="text-sm text-slate-400">Created {formatDateTime(item.createdAtUtc)}</div>
                  </div>
                  <Badge>{item.status}</Badge>
                </div>
                <div className="mt-2 text-sm text-slate-300">Items: {item.itemCount}</div>
              </button>
            ))}
          </div>
        </Card>

        <Card title="Create booking request">
          <form className="grid gap-4" onSubmit={createRequest}>
            <Field label="Client"><Select value={form.clientId} onChange={(e) => setForm((c) => ({ ...c, clientId: e.target.value, petId: "", requestedByContactId: "" }))} required><option value="">Select client</option>{clients.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Pet"><Select value={form.petId} onChange={(e) => setForm((c) => ({ ...c, petId: e.target.value }))} required><option value="">Select pet</option>{selectedClient?.pets.map((x) => <option key={x.id} value={x.id}>{x.name} · {x.breedName}</option>)}</Select></Field>
            <Field label="Requester contact"><Select value={form.requestedByContactId} onChange={(e) => setForm((c) => ({ ...c, requestedByContactId: e.target.value }))}><option value="">None</option>{selectedClient?.contacts.map((x) => <option key={x.id} value={x.id}>{x.firstName} {x.lastName ?? ""}</option>)}</Select></Field>
            <Field label="Channel"><Select value={form.channel} onChange={(e) => setForm((c) => ({ ...c, channel: e.target.value }))}><option>Admin</option><option>Instagram</option><option>Phone</option><option>ClientPortal</option></Select></Field>
            <Field label="Offer"><Select value={form.offerId} onChange={(e) => setForm((c) => ({ ...c, offerId: e.target.value }))} required><option value="">Select offer</option>{offers.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <div className="grid gap-4 md:grid-cols-2">
              <Field label="Preferred start"><Input type="datetime-local" value={form.preferredStartAtUtc} onChange={(e) => setForm((c) => ({ ...c, preferredStartAtUtc: e.target.value }))} /></Field>
              <Field label="Preferred end"><Input type="datetime-local" value={form.preferredEndAtUtc} onChange={(e) => setForm((c) => ({ ...c, preferredEndAtUtc: e.target.value }))} /></Field>
            </div>
            <Field label="Preferred label"><Input value={form.preferredLabel} onChange={(e) => setForm((c) => ({ ...c, preferredLabel: e.target.value }))} /></Field>
            <Field label="Notes"><TextArea value={form.notes} onChange={(e) => setForm((c) => ({ ...c, notes: e.target.value }))} /></Field>
            <PrimaryButton type="submit">Create booking request</PrimaryButton>
          </form>
        </Card>
      </div>

      {selectedRequest ? (
        <Card title="Selected booking request" description="Convert the request into a confirmed appointment.">
          <div className="grid gap-2 text-sm text-slate-300">
            <div>Status: {selectedRequest.status}</div>
            <div>Pet: {selectedRequest.petId}</div>
            <div>Items: {selectedRequest.itemCount}</div>
          </div>
          <form className="mt-4 grid gap-4 md:grid-cols-3" onSubmit={convertRequest}>
            <Field label="Groomer"><Select value={convertForm.groomerId} onChange={(e) => setConvertForm((c) => ({ ...c, groomerId: e.target.value }))} required><option value="">Select groomer</option>{groomers.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Start at"><Input type="datetime-local" value={convertForm.startAtUtc} onChange={(e) => setConvertForm((c) => ({ ...c, startAtUtc: e.target.value }))} required /></Field>
            <PrimaryButton type="submit" className="md:self-end">Convert to appointment</PrimaryButton>
          </form>
        </Card>
      ) : null}
    </div>
  );
}

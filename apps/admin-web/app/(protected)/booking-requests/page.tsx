"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { BookingRequestDetail, BookingRequestListItem, ClientDetail, GroomerListItem, GroomerListResponse, OfferListItem, PagedResult } from "@/lib/types";
import { unwrapItems } from "@/lib/contracts";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, LoadingState, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

function toneForStatus(status: string): "default" | "success" | "warning" {
  switch (status) {
    case "Converted":
      return "success";
    case "NeedsReview":
      return "warning";
    default:
      return "default";
  }
}

export default function BookingRequestsPage() {
  const [requests, setRequests] = useState<BookingRequestListItem[]>([]);
  const [offers, setOffers] = useState<OfferListItem[]>([]);
  const [clients, setClients] = useState<{ id: string; displayName: string }[]>([]);
  const [selectedClient, setSelectedClient] = useState<ClientDetail | null>(null);
  const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
  const [selectedRequestId, setSelectedRequestId] = useState("");
  const [selectedRequest, setSelectedRequest] = useState<BookingRequestDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [actionInFlight, setActionInFlight] = useState<"create" | "attach" | "convert" | null>(null);
  const [form, setForm] = useState({ clientId: "", petId: "", requestedByContactId: "", channel: "Admin", preferredStartAtUtc: "", preferredEndAtUtc: "", preferredLabel: "Preferred slot", notes: "", offerId: "" });
  const [attachForm, setAttachForm] = useState({ clientId: "", petId: "", requestedByContactId: "" });
  const [convertForm, setConvertForm] = useState({ bookingRequestId: "", groomerId: "", startAtUtc: "" });

  async function loadBase() {
    setError(null);
    setIsLoading(true);
    try {
      const [requestResponse, offerResponse, clientResponse, groomerResponse] = await Promise.all([
        apiRequest<PagedResult<BookingRequestListItem>>("/api/admin/booking-requests?page=1&pageSize=50"),
        apiRequest<OfferListItem[]>("/api/admin/catalog/offers"),
        apiRequest<PagedResult<{ id: string; displayName: string }>>("/api/admin/clients?page=1&pageSize=100"),
        apiRequest<GroomerListResponse>("/api/admin/groomers")
      ]);
      setRequests(requestResponse.items);
      setOffers(offerResponse);
      setClients(clientResponse.items);
      setGroomers(unwrapItems(groomerResponse));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load booking requests.");
    } finally {
      setIsLoading(false);
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

  useEffect(() => {
    if (!attachForm.clientId) {
      return;
    }
    apiRequest<ClientDetail>(`/api/admin/clients/${attachForm.clientId}`)
      .then((client) => setSelectedClient(client))
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load client detail."));
  }, [attachForm.clientId]);

  useEffect(() => {
    if (!selectedRequestId) {
      setSelectedRequest(null);
      return;
    }

    apiRequest<BookingRequestDetail>(`/api/admin/booking-requests/${selectedRequestId}`)
      .then((detail) => {
        setSelectedRequest(detail);
        setConvertForm((current) => ({ ...current, bookingRequestId: detail.id }));
        setAttachForm((current) => ({
          ...current,
          clientId: detail.clientId ?? current.clientId,
          requestedByContactId: detail.requestedByContactId ?? current.requestedByContactId
        }));
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load booking request detail."));
  }, [selectedRequestId]);

  const attachClient = useMemo(() => {
    if (!attachForm.clientId) {
      return null;
    }
    return selectedClient && selectedClient.id === attachForm.clientId ? selectedClient : null;
  }, [attachForm.clientId, selectedClient]);

  async function createRequest(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (actionInFlight) return;
    setError(null);
    setSuccess(null);
    setActionInFlight("create");
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
    } finally {
      setActionInFlight(null);
    }
  }

  async function attachContext(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!selectedRequest) {
      return;
    }
    if (actionInFlight) return;

    setError(null);
    setSuccess(null);
    setActionInFlight("attach");
    try {
      const updated = await apiRequest<BookingRequestDetail>(`/api/admin/booking-requests/${selectedRequest.id}/attach-context`, {
        method: "POST",
        body: JSON.stringify({
          clientId: attachForm.clientId || null,
          petId: attachForm.petId,
          requestedByContactId: attachForm.requestedByContactId || null
        })
      });
      setSelectedRequest(updated);
      setSuccess("Guest request linked to CRM pet context.");
      await loadBase();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to attach guest request context.");
    } finally {
      setActionInFlight(null);
    }
  }

  async function convertRequest(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (actionInFlight) return;
    setError(null);
    setSuccess(null);
    setActionInFlight("convert");
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
      if (selectedRequestId) {
        const detail = await apiRequest<BookingRequestDetail>(`/api/admin/booking-requests/${selectedRequestId}`);
        setSelectedRequest(detail);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to convert booking request.");
    } finally {
      setActionInFlight(null);
    }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Booking" title="Booking requests" description="Review guest-first intake, link ad-hoc requests to CRM pets, and convert reviewed requests into appointments." />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} />

      <div className="grid gap-6 xl:grid-cols-[1.15fr_1fr]">
        <Card title="Booking request queue" description="Guest requests can stay in NeedsReview until staff links them to a real pet and confirms details.">
          {isLoading ? <LoadingState label="Loading booking requests..." /> : null}
          {!isLoading && requests.length === 0 ? <EmptyState title="No booking requests found" description="New public or admin-assisted booking requests will appear here." /> : null}
          {!isLoading && requests.length > 0 ? (
            <div className="grid gap-3">
              {requests.map((item) => (
                <button key={item.id} type="button" onClick={() => setSelectedRequestId(item.id)} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-left transition hover:border-emerald-500/40">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-medium text-white">{item.petDisplayName ?? item.id}</div>
                      <div className="text-sm text-slate-400">{item.requesterDisplayName ?? "Unknown requester"} · {item.requesterPrimaryContact ?? item.channel}</div>
                    </div>
                    <Badge tone={toneForStatus(item.status)}>{item.status}</Badge>
                  </div>
                  <div className="mt-2 grid gap-1 text-sm text-slate-300 md:grid-cols-2">
                    <div>Items: {item.itemCount}</div>
                    <div>Selection: {item.selectionMode ?? "—"}</div>
                    <div>Preferred groomer: {item.preferredGroomerName ?? "Any suitable"}</div>
                    <div>Created: {formatDateTime(item.createdAtUtc)}</div>
                  </div>
                </button>
              ))}
            </div>
          ) : null}
        </Card>

        <Card title="Create booking request" description="Admin-assisted intake for already-known clients still works as before.">
          <form className="grid gap-4" onSubmit={createRequest}>
            <Field label="Client"><Select value={form.clientId} onChange={(e) => setForm((c) => ({ ...c, clientId: e.target.value, petId: "", requestedByContactId: "" }))} required><option value="">Select client</option>{clients.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Pet"><Select value={form.petId} onChange={(e) => setForm((c) => ({ ...c, petId: e.target.value }))} required><option value="">Select pet</option>{selectedClient?.pets.map((x) => <option key={x.id} value={x.id}>{x.name} · {x.breedName}</option>)}</Select></Field>
            <Field label="Requester contact"><Select value={form.requestedByContactId} onChange={(e) => setForm((c) => ({ ...c, requestedByContactId: e.target.value }))}><option value="">None</option>{selectedClient?.contacts.map((x) => <option key={x.id} value={x.id}>{x.firstName} {x.lastName ?? ""}</option>)}</Select></Field>
            <Field label="Channel"><Select value={form.channel} onChange={(e) => setForm((c) => ({ ...c, channel: e.target.value }))}><option>Admin</option><option>Instagram</option><option>Phone</option><option>ClientPortal</option><option>PublicWidget</option></Select></Field>
            <Field label="Offer"><Select value={form.offerId} onChange={(e) => setForm((c) => ({ ...c, offerId: e.target.value }))} required><option value="">Select offer</option>{offers.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <div className="grid gap-4 md:grid-cols-2">
              <Field label="Preferred start"><Input type="datetime-local" value={form.preferredStartAtUtc} onChange={(e) => setForm((c) => ({ ...c, preferredStartAtUtc: e.target.value }))} /></Field>
              <Field label="Preferred end"><Input type="datetime-local" value={form.preferredEndAtUtc} onChange={(e) => setForm((c) => ({ ...c, preferredEndAtUtc: e.target.value }))} /></Field>
            </div>
            <Field label="Preferred label"><Input value={form.preferredLabel} onChange={(e) => setForm((c) => ({ ...c, preferredLabel: e.target.value }))} /></Field>
            <Field label="Notes"><TextArea value={form.notes} onChange={(e) => setForm((c) => ({ ...c, notes: e.target.value }))} /></Field>
            <PrimaryButton type="submit" disabled={actionInFlight !== null}>{actionInFlight === "create" ? "Creating..." : "Create booking request"}</PrimaryButton>
          </form>
        </Card>
      </div>

      {selectedRequest ? (
        <Card title="Selected request" description="Guest requests without petId must be linked to a CRM pet before conversion.">
          <div className="grid gap-4 lg:grid-cols-[1.1fr_1fr]">
            <div className="space-y-4 rounded-2xl border border-slate-800 bg-slate-950/40 p-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="text-lg font-semibold text-white">{selectedRequest.subject?.petDisplayName ?? "Ad-hoc pet request"}</div>
                  <div className="mt-1 text-sm text-slate-400">{selectedRequest.subject?.breedName ?? "Breed pending"} · {selectedRequest.subject?.animalTypeCode ?? "Animal type pending"}</div>
                </div>
                <Badge tone={toneForStatus(selectedRequest.status)}>{selectedRequest.status}</Badge>
              </div>
              <div className="grid gap-2 text-sm text-slate-300 md:grid-cols-2">
                <div>Channel: {selectedRequest.channel}</div>
                <div>Selection: {selectedRequest.selectionMode ?? "—"}</div>
                <div>Requester: {selectedRequest.subject?.requesterDisplayName ?? "—"}</div>
                <div>Primary contact: {selectedRequest.subject?.requesterPrimaryContact ?? "—"}</div>
                <div>Preferred groomer: {selectedRequest.preferredGroomerName ?? selectedRequest.subject?.preferredGroomerName ?? "Any suitable"}</div>
                <div>Created: {formatDateTime(selectedRequest.createdAtUtc)}</div>
              </div>
              {selectedRequest.preferredTimes.length > 0 ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-300">
                  <div className="font-medium text-white">Preferred times</div>
                  <div className="mt-2 grid gap-2">
                    {selectedRequest.preferredTimes.map((time, index) => <div key={`${time.startAtUtc}-${index}`}>{formatDateTime(time.startAtUtc)} → {formatDateTime(time.endAtUtc)} {time.label ? `· ${time.label}` : ""}</div>)}
                  </div>
                </div>
              ) : null}
              {selectedRequest.subject?.guestIntake ? (
                <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-300">
                  <div className="font-medium text-white">Guest intake snapshot</div>
                  <div className="mt-2 grid gap-1">
                    <div>Pet display name: {selectedRequest.subject.guestIntake.pet?.displayName ?? "—"}</div>
                    <div>Coat: {selectedRequest.subject.guestIntake.pet?.coatTypeName ?? "—"}</div>
                    <div>Size: {selectedRequest.subject.guestIntake.pet?.sizeCategoryName ?? "—"}</div>
                    <div>Weight: {selectedRequest.subject.guestIntake.pet?.weightKg ?? "—"}</div>
                    <div>Requester phone: {selectedRequest.subject.guestIntake.requester?.phone ?? "—"}</div>
                    <div>Requester Instagram: {selectedRequest.subject.guestIntake.requester?.instagramHandle ?? "—"}</div>
                    <div>Requester email: {selectedRequest.subject.guestIntake.requester?.email ?? "—"}</div>
                  </div>
                </div>
              ) : null}
              {selectedRequest.notes ? <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-300">{selectedRequest.notes}</div> : null}
            </div>

            <div className="space-y-4">
              {!selectedRequest.petId ? (
                <Card title="Attach CRM pet context" description="Required before converting an ad-hoc guest request into an appointment.">
                  <form className="grid gap-4" onSubmit={attachContext}>
                    <Field label="Client"><Select value={attachForm.clientId} onChange={(e) => setAttachForm((c) => ({ ...c, clientId: e.target.value, petId: "", requestedByContactId: "" }))} required><option value="">Select client</option>{clients.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
                    <Field label="Pet"><Select value={attachForm.petId} onChange={(e) => setAttachForm((c) => ({ ...c, petId: e.target.value }))} required><option value="">Select pet</option>{attachClient?.pets.map((x) => <option key={x.id} value={x.id}>{x.name} · {x.breedName}</option>)}</Select></Field>
                    <Field label="Requester contact"><Select value={attachForm.requestedByContactId} onChange={(e) => setAttachForm((c) => ({ ...c, requestedByContactId: e.target.value }))}><option value="">None</option>{attachClient?.contacts.map((x) => <option key={x.id} value={x.id}>{x.firstName} {x.lastName ?? ""}</option>)}</Select></Field>
                    <PrimaryButton type="submit" disabled={actionInFlight !== null}>{actionInFlight === "attach" ? "Attaching..." : "Attach CRM context"}</PrimaryButton>
                  </form>
                </Card>
              ) : null}

              <Card title="Convert to appointment" description="Use after staff review and contact confirmation.">
                <form className="grid gap-4" onSubmit={convertRequest}>
                  <Field label="Groomer"><Select value={convertForm.groomerId} onChange={(e) => setConvertForm((c) => ({ ...c, groomerId: e.target.value }))} required><option value="">Select groomer</option>{groomers.map((x) => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
                  <Field label="Start at"><Input type="datetime-local" value={convertForm.startAtUtc} onChange={(e) => setConvertForm((c) => ({ ...c, startAtUtc: e.target.value }))} required /></Field>
                  <PrimaryButton type="submit" disabled={!selectedRequest.petId || actionInFlight !== null}>{actionInFlight === "convert" ? "Converting..." : "Convert to appointment"}</PrimaryButton>
                </form>
                {!selectedRequest.petId ? <p className="mt-3 text-sm text-amber-300">Link this request to a real pet before conversion.</p> : null}
              </Card>
            </div>
          </div>
        </Card>
      ) : null}
    </div>
  );
}

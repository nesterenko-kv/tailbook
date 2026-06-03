"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime, formatMoney } from "@/lib/format";
import { unwrapItems } from "@/lib/contracts";
import type { AppointmentListItem, BulkCancelAppointmentsResponse, ClientDetail, GroomerListItem, GroomerListResponse, OfferListItem, PagedResult } from "@/lib/types";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, LoadingState, PageHeader, PrimaryButton, SecondaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";
import { Pagination } from "@/components/pagination";
import { SortControl } from "@/components/sort-control";

export default function AppointmentsPage() {
  const [appointmentResult, setAppointmentResult] = useState<PagedResult<AppointmentListItem> | null>(null);
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState("startAt");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");
  const [clients, setClients] = useState<{ id: string; displayName: string }[]>([]);
  const [selectedClient, setSelectedClient] = useState<ClientDetail | null>(null);
  const [offers, setOffers] = useState<OfferListItem[]>([]);
  const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [bulkCancelOpen, setBulkCancelOpen] = useState(false);
  const [bulkReasonCode, setBulkReasonCode] = useState("CUSTOMER_REQUEST");
  const [bulkNotes, setBulkNotes] = useState("");
  const [isBulkCancelling, setIsBulkCancelling] = useState(false);
  const [createdAppointmentId, setCreatedAppointmentId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState({ clientId: "", petId: "", groomerId: "", startAt: "", offerId: "" });

  async function loadBase() {
    setError(null);
    setIsLoading(true);
    try {
      const [appointmentResponse, clientResponse, offerResponse, groomerResponse] = await Promise.all([
        apiRequest<PagedResult<AppointmentListItem>>(`/api/admin/appointments?page=${page}&pageSize=50&sortBy=${sortBy}&sortDirection=${sortDirection}`),
        apiRequest<PagedResult<{ id: string; displayName: string }>>("/api/admin/clients?page=1&pageSize=100"),
        apiRequest<OfferListItem[]>("/api/admin/catalog/offers"),
        apiRequest<GroomerListResponse>("/api/admin/groomers")
      ]);
      setAppointmentResult(appointmentResponse);
      setClients(clientResponse.items);
      setOffers(offerResponse);
      setGroomers(unwrapItems(groomerResponse));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load appointments.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => { void loadBase(); }, [page, sortBy, sortDirection]);
  useEffect(() => {
    if (!form.clientId) { setSelectedClient(null); return; }
    apiRequest<ClientDetail>(`/api/admin/clients/${form.clientId}`)
      .then(setSelectedClient)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load client detail."));
  }, [form.clientId]);

  const toggleSelect = useCallback((id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }, []);

  const toggleAll = useCallback(() => {
    if (!appointmentResult) return;
    if (selectedIds.size === appointmentResult.items.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(appointmentResult.items.map((x) => x.id)));
    }
  }, [appointmentResult, selectedIds]);

  async function handleBulkCancel() {
    if (isBulkCancelling) return;
    setIsBulkCancelling(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await apiRequest<BulkCancelAppointmentsResponse>("/api/admin/appointments/bulk/cancel", {
        method: "POST",
        body: JSON.stringify({
          appointmentIds: Array.from(selectedIds),
          reasonCode: bulkReasonCode,
          notes: bulkNotes || null
        })
      });
      setSelectedIds(new Set());
      setBulkCancelOpen(false);
      if (result.failed === 0) {
        setSuccess(`${result.succeeded} appointment${result.succeeded === 1 ? "" : "s"} cancelled.`);
      } else {
        setError(`${result.succeeded} cancelled, ${result.failed} failed.`);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to cancel appointments.");
    }
    await loadBase();
    setIsBulkCancelling(false);
  }

  async function createAppointment(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (isSubmitting) return;
    setError(null); setSuccess(null);
    setIsSubmitting(true);
    try {
      const created = await apiRequest<{ id: string }>("/api/admin/appointments", {
        method: "POST",
        body: JSON.stringify({
          petId: form.petId,
          groomerId: form.groomerId,
          startAt: new Date(form.startAt).toISOString(),
          items: form.offerId ? [{ offerId: form.offerId, itemType: null, requestedNotes: null }] : []
        })
      });
      setCreatedAppointmentId(created.id);
      setSuccess("Appointment created.");
      await loadBase();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to create appointment."); }
    finally { setIsSubmitting(false); }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Appointments" title="Appointments" description="Create direct appointments and manage the confirmed reservation queue." />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} action={createdAppointmentId ? { label: "View →", onClick: () => window.open(`/appointments/${createdAppointmentId}`, "_self") } : undefined} />
      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <Card title="Appointment list">
          <SortControl
            sortBy={sortBy}
            sortDirection={sortDirection}
            options={[
              { value: "startAt", label: "Start date" },
              { value: "status", label: "Status" },
              { value: "createdAt", label: "Created date" },
            ]}
            onSortByChange={(v) => { setSortBy(v); setPage(1); }}
            onSortDirectionChange={(v) => { setSortDirection(v); setPage(1); }}
          />
          {isLoading ? <LoadingState label="Loading appointments..." /> : null}
          {!isLoading && (!appointmentResult || appointmentResult.items.length === 0) ? <EmptyState title="No appointments found" description="Create a direct appointment or convert a booking request." /> : null}
          {!isLoading && appointmentResult && appointmentResult.items.length > 0 ? (
            <>
              {selectedIds.size > 0 ? (
                <div className="mb-3 rounded-2xl border border-emerald-700/40 bg-emerald-950/30 px-4 py-3">
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-emerald-300">{selectedIds.size} selected</span>
                    {bulkCancelOpen ? (
                      <SecondaryButton type="button" onClick={() => { setBulkCancelOpen(false); setBulkNotes(""); }}>Back</SecondaryButton>
                    ) : (
                      <>
                        <SecondaryButton type="button" onClick={() => setBulkCancelOpen(true)} className="ml-auto">Cancel selected</SecondaryButton>
                        <SecondaryButton type="button" onClick={() => setSelectedIds(new Set())}>Clear</SecondaryButton>
                      </>
                    )}
                  </div>
                  {bulkCancelOpen ? (
                    <div className="mt-3 grid gap-3 border-t border-emerald-700/30 pt-3">
                      <Field label="Reason">
                        <Select value={bulkReasonCode} onChange={(e) => setBulkReasonCode(e.target.value)}>
                          <option value="CUSTOMER_REQUEST">Customer request</option>
                          <option value="GROOMER_UNAVAILABLE">Groomer unavailable</option>
                          <option value="DUPLICATE_BOOKING">Duplicate booking</option>
                          <option value="NO_SHOW">No show</option>
                          <option value="OTHER">Other</option>
                        </Select>
                      </Field>
                      <Field label="Notes (optional)">
                        <TextArea value={bulkNotes} onChange={(e) => setBulkNotes(e.target.value)} rows={2} />
                      </Field>
                      <div className="flex justify-end gap-2">
                        <PrimaryButton type="button" disabled={isBulkCancelling} onClick={handleBulkCancel}>
                          {isBulkCancelling ? "Cancelling..." : `Cancel ${selectedIds.size} appointment${selectedIds.size === 1 ? "" : "s"}`}
                        </PrimaryButton>
                      </div>
                    </div>
                  ) : null}
                </div>
              ) : null}
              <div className="flex items-center gap-2 px-1 pb-2">
                <input
                  type="checkbox"
                  className="size-4 accent-emerald-500"
                  checked={appointmentResult.items.length > 0 && selectedIds.size === appointmentResult.items.length}
                  onChange={toggleAll}
                  aria-label="Select all"
                />
                <span className="text-xs text-slate-500">Select all</span>
              </div>
              <div className="grid gap-3">
                {appointmentResult.items.map((item) => (
                  <div key={item.id} className="flex items-start gap-3">
                    <input
                      type="checkbox"
                      className="mt-4 size-4 accent-emerald-500"
                      checked={selectedIds.has(item.id)}
                      onChange={() => toggleSelect(item.id)}
                      aria-label={`Select ${item.id}`}
                    />
                    <Link href={`/appointments/${item.id}`} className="flex-1 rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="font-medium">{item.id}</div>
                          <div className="text-sm text-slate-400">{formatDateTime(item.startAt)}</div>
                        </div>
                        <Badge>{item.status}</Badge>
                      </div>
                      <div className="mt-2 text-sm text-slate-300">{formatMoney(item.totalAmount)} · items {item.itemCount}</div>
                    </Link>
                  </div>
                ))}
              </div>
            </>
          ) : null}
          {appointmentResult ? <Pagination page={page} pageSize={50} totalCount={appointmentResult.totalCount} onPageChange={setPage} /> : null}
        </Card>
        <Card title="Create direct appointment">
          <form className="grid gap-4" onSubmit={createAppointment}>
            <Field label="Client"><Select value={form.clientId} onChange={(e)=>setForm(c=>({...c, clientId:e.target.value, petId:""}))} required><option value="">Select client</option>{clients.map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Pet"><Select value={form.petId} onChange={(e)=>setForm(c=>({...c, petId:e.target.value}))} required><option value="">Select pet</option>{selectedClient?.pets.map(x=><option key={x.id} value={x.id}>{x.name} · {x.breedName}</option>)}</Select></Field>
            <Field label="Groomer"><Select value={form.groomerId} onChange={(e)=>setForm(c=>({...c, groomerId:e.target.value}))} required><option value="">Select groomer</option>{groomers.map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <Field label="Start at"><Input type="datetime-local" value={form.startAt} onChange={(e)=>setForm(c=>({...c, startAt:e.target.value}))} required /></Field>
            <Field label="Offer"><Select value={form.offerId} onChange={(e)=>setForm(c=>({...c, offerId:e.target.value}))} required><option value="">Select offer</option>{offers.map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
            <PrimaryButton type="submit" disabled={isSubmitting}>{isSubmitting ? "Creating..." : "Create appointment"}</PrimaryButton>
          </form>
        </Card>
      </div>

    </div>
  );
}

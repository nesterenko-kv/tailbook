"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime, formatMoney } from "@/lib/format";
import type { GroomerListResponse, PagedResult, VisitListItem } from "@/lib/types";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, PageHeader, Select } from "@/components/ui";

const statusOptions = ["Open", "InProgress", "AwaitingFinalization", "Closed"];

export default function VisitsPage() {
  const [visits, setVisits] = useState<PagedResult<VisitListItem> | null>(null);
  const [groomers, setGroomers] = useState<GroomerListResponse>({ items: [] });
  const [filters, setFilters] = useState({ status: "", groomerId: "", fromUtc: "", toUtc: "", appointmentId: "" });
  const [error, setError] = useState<string | null>(null);

  async function loadVisits() {
    setError(null);
    try {
      const query = new URLSearchParams({ page: "1", pageSize: "25" });
      if (filters.status) query.set("status", filters.status);
      if (filters.groomerId) query.set("groomerId", filters.groomerId);
      if (filters.appointmentId.trim()) query.set("appointmentId", filters.appointmentId.trim());
      if (filters.fromUtc) query.set("fromUtc", new Date(filters.fromUtc).toISOString());
      if (filters.toUtc) query.set("toUtc", new Date(filters.toUtc).toISOString());
      const response = await apiRequest<PagedResult<VisitListItem>>(`/api/admin/visits?${query.toString()}`);
      setVisits(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load visits.");
    }
  }

  useEffect(() => {
    void apiRequest<GroomerListResponse>("/api/admin/groomers")
      .then(setGroomers)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Failed to load groomers."));
  }, []);

  useEffect(() => {
    void loadVisits();
  }, [filters.status, filters.groomerId, filters.fromUtc, filters.toUtc, filters.appointmentId]);

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Visits" title="Visits" description="Filter active and recent visits, then open the execution detail workflow." />
      <ErrorBanner message={error} />
      <Card title="Visit filters">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          <Field label="Status"><Select value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value }))}><option value="">All statuses</option>{statusOptions.map((status) => <option key={status} value={status}>{status}</option>)}</Select></Field>
          <Field label="Groomer"><Select value={filters.groomerId} onChange={(event) => setFilters((current) => ({ ...current, groomerId: event.target.value }))}><option value="">All groomers</option>{groomers.items.map((groomer) => <option key={groomer.id} value={groomer.id}>{groomer.displayName}</option>)}</Select></Field>
          <Field label="From"><Input type="datetime-local" value={filters.fromUtc} onChange={(event) => setFilters((current) => ({ ...current, fromUtc: event.target.value }))} /></Field>
          <Field label="To"><Input type="datetime-local" value={filters.toUtc} onChange={(event) => setFilters((current) => ({ ...current, toUtc: event.target.value }))} /></Field>
          <Field label="Appointment id"><Input value={filters.appointmentId} onChange={(event) => setFilters((current) => ({ ...current, appointmentId: event.target.value }))} placeholder="Optional exact id" /></Field>
        </div>
      </Card>
      <Card title="Visit list">
        <div className="grid gap-3">
          {visits?.items.map((visit) => (
            <Link key={visit.id} href={`/visits/${visit.id}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-medium text-white">{visit.petName}</h3>
                    <Badge>{visit.status}</Badge>
                  </div>
                  <p className="mt-1 text-sm text-slate-400">{visit.breedName} · {formatDateTime(visit.appointmentStartAtUtc)}</p>
                  <p className="mt-1 text-xs text-slate-500">Appointment {visit.appointmentId}</p>
                </div>
                <div className="text-right text-sm">
                  <p className="font-medium text-white">{formatMoney(visit.finalTotalAmount)}</p>
                  <p className="text-slate-500">{visit.itemCount} item{visit.itemCount === 1 ? "" : "s"}</p>
                </div>
              </div>
            </Link>
          ))}
          {visits && visits.items.length === 0 ? <EmptyState title="No visits found" description="Adjust filters or check in an appointment." /> : null}
          {!visits ? <p className="text-sm text-slate-400">Loading visits...</p> : null}
          {visits ? <p className="text-xs text-slate-500">Showing {visits.items.length} of {visits.totalCount} visits.</p> : null}
        </div>
      </Card>
    </div>
  );
}

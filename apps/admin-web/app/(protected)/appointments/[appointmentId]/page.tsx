"use client";

import { unwrapItems } from "@/lib/contracts";
import type { AppointmentDetail, GroomerListItem, GroomerListResponse } from "@/lib/types";
import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { addRecentVisitId } from "@/lib/recent";
import { formatDateTime, formatMoney } from "@/lib/format";
import { Badge, Card, ErrorBanner, Field, Input, LinkButton, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

export default function AppointmentDetailPage() {
  const params = useParams<{ appointmentId: string }>();
  const appointmentId = String(params.appointmentId);
  const router = useRouter();
  const [appointment, setAppointment] = useState<AppointmentDetail | null>(null);
  const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [rescheduleForm, setRescheduleForm] = useState({ groomerId: "", startAtUtc: "" });
  const [cancelForm, setCancelForm] = useState({ reasonCode: "CUSTOMER_REQUEST", notes: "" });

  async function loadAll() {
    setError(null);
    try {
      const [detail, groomerResponse] = await Promise.all([
        apiRequest<AppointmentDetail>(`/api/admin/appointments/${appointmentId}`),
        apiRequest<GroomerListResponse>("/api/admin/groomers")
      ]);
      setAppointment(detail);
      setGroomers(unwrapItems(groomerResponse));
      setRescheduleForm({ groomerId: detail.groomerId, startAtUtc: new Date(detail.startAtUtc).toISOString().slice(0,16) });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load appointment.");
    }
  }

  useEffect(() => { void loadAll(); }, [appointmentId]);

  async function reschedule(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!appointment) return;
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/appointments/${appointmentId}/reschedule`, { method: "POST", body: JSON.stringify({ appointmentId, groomerId: rescheduleForm.groomerId, startAtUtc: new Date(rescheduleForm.startAtUtc).toISOString(), expectedVersionNo: appointment.versionNo }) });
      setSuccess("Appointment rescheduled.");
      await loadAll();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to reschedule appointment."); }
  }

  async function cancel(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!appointment) return;
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/appointments/${appointmentId}/cancel`, { method: "POST", body: JSON.stringify({ appointmentId, expectedVersionNo: appointment.versionNo, reasonCode: cancelForm.reasonCode, notes: cancelForm.notes || null }) });
      setSuccess("Appointment cancelled.");
      await loadAll();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to cancel appointment."); }
  }

  async function checkIn() {
    setError(null); setSuccess(null);
    try {
      const response = await apiRequest<{ id: string }>(`/api/admin/appointments/${appointmentId}/check-in`, { method: "POST", body: JSON.stringify({ appointmentId }) });
      addRecentVisitId(response.id);
      setSuccess("Visit opened.");
      router.push(`/visits/${response.id}`);
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to check in appointment."); }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Appointment detail" title={appointment?.id ?? "Appointment detail"} description="Reschedule, cancel, or check in to open a visit." action={<LinkButton href="/appointments">Back to appointments</LinkButton>} />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} />
      {!appointment ? <div className="text-sm text-slate-300">Loading appointment…</div> : (
        <div className="grid gap-6 xl:grid-cols-[1.15fr_0.85fr]">
          <Card title="Reservation summary">
            <div className="grid gap-2 text-sm text-slate-300">
              <div className="flex items-center gap-2"><Badge>{appointment.status}</Badge><span>Version {appointment.versionNo}</span></div>
              <div>Pet: {appointment.pet.breedName} · {appointment.pet.animalTypeName}</div>
              <div>Groomer: {appointment.groomerId}</div>
              <div>Start: {formatDateTime(appointment.startAtUtc)}</div>
              <div>End: {formatDateTime(appointment.endAtUtc)}</div>
              <div>Total: {formatMoney(appointment.totalAmount)}</div>
              <div>Reserved minutes: {appointment.reservedMinutes}</div>
            </div>
            <div className="mt-4 grid gap-3">
              {appointment.items.map((item) => (
                <div key={item.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm">
                  <div className="font-medium">{item.offerDisplayName}</div>
                  <div className="text-slate-400">{item.itemType} · {formatMoney(item.priceAmount)}</div>
                  <div className="text-slate-400">Service {item.serviceMinutes} min · Reserved {item.reservedMinutes} min</div>
                </div>
              ))}
            </div>
            <div className="mt-4"><PrimaryButton type="button" onClick={() => void checkIn()} disabled={appointment.status === 'CheckedIn' || appointment.status === 'Cancelled' || appointment.status === 'Closed'}>Check in and open visit</PrimaryButton></div>
          </Card>
          <div className="grid gap-6">
            <Card title="Reschedule">
              <form className="grid gap-4" onSubmit={reschedule}>
                <Field label="Groomer"><Select value={rescheduleForm.groomerId} onChange={(e) => setRescheduleForm(c => ({ ...c, groomerId: e.target.value }))}>{groomers.map(x => <option key={x.id} value={x.id}>{x.displayName}</option>)}</Select></Field>
                <Field label="Start at"><Input type="datetime-local" value={rescheduleForm.startAtUtc} onChange={(e) => setRescheduleForm(c => ({ ...c, startAtUtc: e.target.value }))} /></Field>
                <PrimaryButton type="submit">Reschedule</PrimaryButton>
              </form>
            </Card>
            <Card title="Cancel">
              <form className="grid gap-4" onSubmit={cancel}>
                <Field label="Reason code"><Input value={cancelForm.reasonCode} onChange={(e) => setCancelForm(c => ({ ...c, reasonCode: e.target.value }))} /></Field>
                <Field label="Notes"><TextArea value={cancelForm.notes} onChange={(e) => setCancelForm(c => ({ ...c, notes: e.target.value }))} /></Field>
                <PrimaryButton type="submit" className="bg-rose-500 text-white hover:bg-rose-400">Cancel appointment</PrimaryButton>
              </form>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
}

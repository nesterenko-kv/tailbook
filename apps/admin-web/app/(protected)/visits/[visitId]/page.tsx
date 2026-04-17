"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { addRecentVisitId } from "@/lib/recent";
import { formatDateTime, formatMoney } from "@/lib/format";
import type { VisitDetail } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, LinkButton, PageHeader, PrimaryButton, Select, SuccessBanner, TextArea } from "@/components/ui";

export default function VisitDetailPage() {
  const params = useParams<{ visitId: string }>();
  const visitId = String(params.visitId);
  const [visit, setVisit] = useState<VisitDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [performedForm, setPerformedForm] = useState({ visitExecutionItemId: "", procedureId: "", status: "Performed", note: "" });
  const [skippedForm, setSkippedForm] = useState({ visitExecutionItemId: "", offerVersionComponentId: "", procedureId: "", omissionReasonCode: "NOT_NEEDED", note: "" });
  const [adjustmentForm, setAdjustmentForm] = useState({ sign: "1", amount: "0", reasonCode: "MANUAL_ADJUSTMENT", note: "" });

  async function loadVisit() {
    setError(null);
    try {
      const response = await apiRequest<VisitDetail>(`/api/admin/visits/${visitId}`);
      addRecentVisitId(response.id);
      setVisit(response);
      if (response.items[0]) {
        const firstItem = response.items[0];
        const firstExpected = firstItem.expectedComponents[0];
        setPerformedForm((c) => ({ ...c, visitExecutionItemId: firstItem.id, procedureId: firstExpected?.procedureId ?? c.procedureId }));
        setSkippedForm((c) => ({ ...c, visitExecutionItemId: firstItem.id, offerVersionComponentId: firstExpected?.id ?? c.offerVersionComponentId, procedureId: firstExpected?.procedureId ?? c.procedureId }));
      }
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to load visit."); }
  }

  useEffect(() => { void loadVisit(); }, [visitId]);

  async function recordPerformed(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/visits/${visitId}/performed-procedures`, { method: "POST", body: JSON.stringify({ visitId, visitExecutionItemId: performedForm.visitExecutionItemId, procedureId: performedForm.procedureId, status: performedForm.status, note: performedForm.note || null }) });
      setSuccess("Performed procedure recorded.");
      await loadVisit();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to record performed procedure."); }
  }

  async function recordSkipped(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/visits/${visitId}/skipped-components`, { method: "POST", body: JSON.stringify({ visitId, visitExecutionItemId: skippedForm.visitExecutionItemId, offerVersionComponentId: skippedForm.offerVersionComponentId, procedureId: skippedForm.procedureId, omissionReasonCode: skippedForm.omissionReasonCode, note: skippedForm.note || null }) });
      setSuccess("Skipped component recorded.");
      await loadVisit();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to record skipped component."); }
  }

  async function applyAdjustment(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/visits/${visitId}/adjustments`, { method: "POST", body: JSON.stringify({ visitId, sign: Number(adjustmentForm.sign), amount: Number(adjustmentForm.amount), reasonCode: adjustmentForm.reasonCode, note: adjustmentForm.note || null }) });
      setSuccess("Adjustment applied.");
      await loadVisit();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to apply adjustment."); }
  }

  async function markComplete() {
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/visits/${visitId}/complete`, { method: "POST", body: JSON.stringify({ visitId }) });
      setSuccess("Visit completed.");
      await loadVisit();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to complete visit."); }
  }

  async function closeVisit() {
    setError(null); setSuccess(null);
    try {
      await apiRequest(`/api/admin/visits/${visitId}/close`, { method: "POST", body: JSON.stringify({ visitId }) });
      setSuccess("Visit closed.");
      await loadVisit();
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to close visit."); }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Visit detail" title={visit?.id ?? "Visit detail"} description="Record execution truth, signed final adjustments, and close the visit." action={<LinkButton href="/visits">Back to visits</LinkButton>} />
      <ErrorBanner message={error} />
      <SuccessBanner message={success} />
      {!visit ? <div className="text-sm text-slate-300">Loading visit…</div> : (
        <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
          <Card title="Visit summary">
            <div className="grid gap-2 text-sm text-slate-300">
              <div className="flex items-center gap-2"><Badge>{visit.status}</Badge><span>{visit.pet.name} · {visit.pet.breedName}</span></div>
              <div>Checked in: {formatDateTime(visit.checkedInAtUtc)}</div>
              <div>Service minutes: {visit.serviceMinutes}</div>
              <div>Reserved minutes: {visit.reservedMinutes}</div>
              <div>Appointment total: {formatMoney(visit.appointmentTotalAmount)}</div>
              <div>Adjustment total: {formatMoney(visit.adjustmentTotalAmount)}</div>
              <div className="font-medium text-white">Final total: {formatMoney(visit.finalTotalAmount)}</div>
            </div>
            <div className="mt-4 grid gap-3">
              {visit.items.map((item) => (
                <div key={item.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm">
                  <div className="font-medium">{item.offerDisplayName}</div>
                  <div className="text-slate-400">Expected components: {item.expectedComponents.length}</div>
                  <div className="mt-2 space-y-1">
                    {item.expectedComponents.map((component) => <div key={component.id} className="text-slate-300">• {component.procedureName} {component.isSkipped ? '(skipped)' : ''}</div>)}
                  </div>
                </div>
              ))}
            </div>
            <div className="mt-4 flex flex-wrap gap-3">
              <PrimaryButton type="button" onClick={() => void markComplete()}>Complete visit</PrimaryButton>
              <PrimaryButton type="button" onClick={() => void closeVisit()}>Close visit</PrimaryButton>
            </div>
          </Card>
          <div className="grid gap-6">
            <Card title="Record performed procedure">
              <form className="grid gap-4" onSubmit={recordPerformed}>
                <Field label="Execution item"><Select value={performedForm.visitExecutionItemId} onChange={(e)=>setPerformedForm(c=>({...c, visitExecutionItemId:e.target.value}))}>{visit.items.map(x=><option key={x.id} value={x.id}>{x.offerDisplayName}</option>)}</Select></Field>
                <Field label="Procedure id"><Input value={performedForm.procedureId} onChange={(e)=>setPerformedForm(c=>({...c, procedureId:e.target.value}))} required /></Field>
                <Field label="Status"><Select value={performedForm.status} onChange={(e)=>setPerformedForm(c=>({...c, status:e.target.value}))}><option>Performed</option><option>PartiallyPerformed</option></Select></Field>
                <Field label="Note"><TextArea value={performedForm.note} onChange={(e)=>setPerformedForm(c=>({...c, note:e.target.value}))} /></Field>
                <PrimaryButton type="submit">Record performed procedure</PrimaryButton>
              </form>
            </Card>
            <Card title="Record skipped component">
              <form className="grid gap-4" onSubmit={recordSkipped}>
                <Field label="Execution item"><Select value={skippedForm.visitExecutionItemId} onChange={(e)=>setSkippedForm(c=>({...c, visitExecutionItemId:e.target.value}))}>{visit.items.map(x=><option key={x.id} value={x.id}>{x.offerDisplayName}</option>)}</Select></Field>
                <Field label="Offer version component id"><Input value={skippedForm.offerVersionComponentId} onChange={(e)=>setSkippedForm(c=>({...c, offerVersionComponentId:e.target.value}))} required /></Field>
                <Field label="Procedure id"><Input value={skippedForm.procedureId} onChange={(e)=>setSkippedForm(c=>({...c, procedureId:e.target.value}))} required /></Field>
                <Field label="Reason code"><Input value={skippedForm.omissionReasonCode} onChange={(e)=>setSkippedForm(c=>({...c, omissionReasonCode:e.target.value}))} required /></Field>
                <Field label="Note"><TextArea value={skippedForm.note} onChange={(e)=>setSkippedForm(c=>({...c, note:e.target.value}))} /></Field>
                <PrimaryButton type="submit">Record skipped component</PrimaryButton>
              </form>
            </Card>
            <Card title="Apply final adjustment">
              <form className="grid gap-4" onSubmit={applyAdjustment}>
                <Field label="Sign"><Select value={adjustmentForm.sign} onChange={(e)=>setAdjustmentForm(c=>({...c, sign:e.target.value}))}><option value="1">+1 surcharge</option><option value="-1">-1 reduction</option></Select></Field>
                <Field label="Amount"><Input type="number" step="0.01" value={adjustmentForm.amount} onChange={(e)=>setAdjustmentForm(c=>({...c, amount:e.target.value}))} /></Field>
                <Field label="Reason code"><Input value={adjustmentForm.reasonCode} onChange={(e)=>setAdjustmentForm(c=>({...c, reasonCode:e.target.value}))} /></Field>
                <Field label="Note"><TextArea value={adjustmentForm.note} onChange={(e)=>setAdjustmentForm(c=>({...c, note:e.target.value}))} /></Field>
                <PrimaryButton type="submit">Apply adjustment</PrimaryButton>
              </form>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
}

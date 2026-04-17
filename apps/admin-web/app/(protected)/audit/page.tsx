"use client";

import { FormEvent, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { AuditAccessItem } from "@/lib/types";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, PageHeader, PrimaryButton } from "@/components/ui";

export default function AuditPage() {
  const [items, setItems] = useState<AuditAccessItem[]>([]);
  const [resourceType, setResourceType] = useState("");
  const [resourceId, setResourceId] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function loadAudit(e?: FormEvent<HTMLFormElement>) {
    e?.preventDefault();
    setError(null);
    try {
      const query = new URLSearchParams({ page: "1", pageSize: "100" });
      if (resourceType) query.set("resourceType", resourceType);
      if (resourceId) query.set("resourceId", resourceId);
      const response = await apiRequest<{ items: AuditAccessItem[] }>(`/api/admin/audit/access?${query.toString()}`);
      setItems(response.items);
    } catch (err) { setError(err instanceof ApiError ? err.message : "Failed to load access audit entries."); }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Audit" title="Access audit" description="Review access to sensitive resources such as IAM and CRM-facing views." />
      <ErrorBanner message={error} />
      <Card title="Filters">
        <form className="grid gap-4 md:grid-cols-[1fr_1fr_auto]" onSubmit={loadAudit}>
          <Field label="Resource type"><Input value={resourceType} onChange={(e)=>setResourceType(e.target.value)} placeholder="client, pet, iam_user" /></Field>
          <Field label="Resource id"><Input value={resourceId} onChange={(e)=>setResourceId(e.target.value)} placeholder="Optional resource id" /></Field>
          <PrimaryButton type="submit" className="md:self-end">Load audit</PrimaryButton>
        </form>
      </Card>
      <Card title="Entries">
        {items.length === 0 ? <EmptyState title="No audit entries" description="Run a sensitive read, then refresh this page." /> : (
          <div className="grid gap-3">{items.map(item => <div key={item.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm"><div className="flex items-start justify-between gap-3"><div><div className="font-medium">{item.resourceType}</div><div className="text-slate-400">{item.resourceId}</div></div><Badge>{item.actionCode}</Badge></div><div className="mt-2 text-slate-400">Actor: {item.actorUserId ?? 'system'} · {formatDateTime(item.happenedAtUtc)}</div></div>)}</div>
        )}
      </Card>
    </div>
  );
}

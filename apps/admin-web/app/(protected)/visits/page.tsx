"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { getRecentVisitIds } from "@/lib/recent";
import { Card, EmptyState, Field, Input, LinkButton, PageHeader, PrimaryButton } from "@/components/ui";

export default function VisitsPage() {
  const [visitId, setVisitId] = useState("");
  const recentIds = useMemo(() => getRecentVisitIds(), []);

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader eyebrow="Visits" title="Visits" description="Open active or recent visits by id. The current backend stage does not expose a visit list endpoint yet." />
      <div className="grid gap-6 xl:grid-cols-2">
        <Card title="Open visit by id">
          <div className="grid gap-4 md:grid-cols-[1fr_auto]">
            <Field label="Visit id"><Input value={visitId} onChange={(e)=>setVisitId(e.target.value)} placeholder="Paste visit id" /></Field>
            <LinkButton href={visitId ? `/visits/${visitId}` : "/visits"} className="md:self-end">Open visit</LinkButton>
          </div>
        </Card>
        <Card title="Recent visits">
          {recentIds.length === 0 ? <EmptyState title="No recent visits" description="Recent visit ids appear here after appointment check-in." /> : (
            <div className="grid gap-3">{recentIds.map(id => <Link key={id} href={`/visits/${id}`} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40">{id}</Link>)}</div>
          )}
        </Card>
      </div>
    </div>
  );
}

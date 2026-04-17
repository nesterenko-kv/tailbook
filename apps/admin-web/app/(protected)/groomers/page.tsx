"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { unwrapItems } from "@/lib/contracts";
import { formatDateTime } from "@/lib/format";
import type { GroomerListItem, GroomerListResponse } from "@/lib/types";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, PageHeader, PrimaryButton, SuccessBanner } from "@/components/ui";

export default function GroomersPage() {
  const [groomers, setGroomers] = useState<GroomerListItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState({ displayName: "", userId: "" });

  async function loadGroomers() {
    setError(null);
    try {
      const response = await apiRequest<GroomerListResponse>("/api/admin/groomers");
      setGroomers(unwrapItems(response));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load groomers.");
    }
  }

  useEffect(() => {
    void loadGroomers();
  }, []);

  async function createGroomer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await apiRequest("/api/admin/groomers", {
        method: "POST",
        body: JSON.stringify({
          displayName: form.displayName,
          userId: form.userId || null
        })
      });

      setSuccess("Groomer created.");
      setForm({ displayName: "", userId: "" });
      await loadGroomers();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to create groomer.");
    }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader
        eyebrow="Staff"
        title="Groomers"
        description="Manage groomer profiles and open the detailed staff workspace for capabilities, schedules, and availability."
      />

      <ErrorBanner message={error} />
      <SuccessBanner message={success} />

      <div className="grid gap-6 xl:grid-cols-[1.2fr_1fr]">
        <Card title="Groomer list">
          {groomers.length === 0 ? (
            <EmptyState
              title="No groomers yet"
              description="Create the first groomer profile to start scheduling appointments."
            />
          ) : (
            <div className="grid gap-3">
              {groomers.map((item) => (
                <Link
                  key={item.id}
                  href={`/groomers/${item.id}`}
                  className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 transition hover:border-emerald-500/40"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="font-medium">{item.displayName}</div>
                      <div className="mt-1 text-sm text-slate-400">
                        Capabilities: {item.capabilityCount}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        Updated {formatDateTime(item.updatedAtUtc)}
                      </div>
                    </div>
                    <Badge tone={item.active ? "success" : "warning"}>
                      {item.active ? "Active" : "Inactive"}
                    </Badge>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </Card>

        <Card title="Create groomer">
          <form className="grid gap-4" onSubmit={createGroomer}>
            <Field label="Display name">
              <Input
                value={form.displayName}
                onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))}
                placeholder="Oksana"
                required
              />
            </Field>

            <Field label="IAM user id" hint="Optional. Link an existing IAM user GUID if you already created one.">
              <Input
                value={form.userId}
                onChange={(event) => setForm((current) => ({ ...current, userId: event.target.value }))}
                placeholder="Optional GUID"
              />
            </Field>

            <PrimaryButton type="submit">Create groomer</PrimaryButton>
          </form>
        </Card>
      </div>
    </div>
  );
}

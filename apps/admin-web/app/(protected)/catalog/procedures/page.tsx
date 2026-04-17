"use client";

import { FormEvent, useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { ProcedureItem } from "@/lib/types";
import { Badge, Card, ErrorBanner, Field, Input, PageHeader, PrimaryButton, SuccessBanner } from "@/components/ui";

export default function ProceduresPage() {
    const [items, setItems] = useState<ProcedureItem[]>([]);
    const [code, setCode] = useState("");
    const [name, setName] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    async function load() {
        try {
            setItems(await apiRequest<ProcedureItem[]>("/api/admin/catalog/procedures"));
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to load procedures.");
        }
    }

    useEffect(() => {
        void load();
    }, []);

    async function createProcedure(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccess(null);
        try {
            await apiRequest("/api/admin/catalog/procedures", {
                method: "POST",
                body: JSON.stringify({ code, name })
            });
            setCode("");
            setName("");
            setSuccess("Procedure created.");
            await load();
        } catch (err) {
            setError(err instanceof ApiError ? err.message : "Failed to create procedure.");
        }
    }

    return (
        <div className="flex flex-col gap-6 px-2 py-2">
            <PageHeader eyebrow="Catalog" title="Procedures" description="Manage atomic operational procedures used in package execution plans." />
            <ErrorBanner message={error} />
            <SuccessBanner message={success} />
            <div className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
                <Card title="Procedure list">
                    <div className="grid gap-3">
                        {items.map((item) => (
                            <article key={item.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <div className="flex items-center justify-between gap-3">
                                    <div>
                                        <h3 className="font-medium">{item.name}</h3>
                                        <p className="text-sm text-slate-400">{item.code}</p>
                                    </div>
                                    <Badge tone={item.isActive ? "success" : "default"}>{item.isActive ? "Active" : "Inactive"}</Badge>
                                </div>
                                <p className="mt-3 text-sm text-slate-400">Updated {formatDateTime(item.updatedAtUtc)}</p>
                            </article>
                        ))}
                    </div>
                </Card>
                <Card title="Create procedure">
                    <form className="space-y-4" onSubmit={createProcedure}>
                        <Field label="Code"><Input value={code} onChange={(event) => setCode(event.target.value)} required /></Field>
                        <Field label="Name"><Input value={name} onChange={(event) => setName(event.target.value)} required /></Field>
                        <PrimaryButton type="submit" className="w-full">Create procedure</PrimaryButton>
                    </form>
                </Card>
            </div>
        </div>
    );
}

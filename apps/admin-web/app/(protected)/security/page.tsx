"use client";

import { useEffect, useState } from "react";
import { Copy, RefreshCw, ShieldCheck } from "lucide-react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { GenerateMfaRecoveryCodesResponse, MfaRecoveryCodeStatus } from "@/lib/types";
import { Card, ErrorBanner, LoadingState, PageHeader, PrimaryButton, SecondaryButton, SuccessBanner } from "@/components/ui";

export default function SecurityPage() {
  const [status, setStatus] = useState<MfaRecoveryCodeStatus | null>(null);
  const [generatedCodes, setGeneratedCodes] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function loadStatus() {
    setIsLoading(true);
    setError(null);
    try {
      setStatus(await apiRequest<MfaRecoveryCodeStatus>("/api/identity/me/mfa/recovery-codes"));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to load recovery-code status.");
    } finally {
      setIsLoading(false);
    }
  }

  async function generateCodes() {
    setIsGenerating(true);
    setError(null);
    setSuccess(null);
    try {
      const response = await apiRequest<GenerateMfaRecoveryCodesResponse>("/api/identity/me/mfa/recovery-codes", {
        method: "POST"
      });
      setGeneratedCodes(response.recoveryCodes);
      setStatus({
        activeCodeCount: response.activeCodeCount,
        lastGeneratedAt: response.createdAt
      });
      setSuccess("Recovery codes generated.");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to generate recovery codes.");
    } finally {
      setIsGenerating(false);
    }
  }

  async function copyCodes() {
    if (generatedCodes.length === 0 || !navigator.clipboard) return;

    try {
      await navigator.clipboard.writeText(generatedCodes.join("\n"));
      setSuccess("Recovery codes copied.");
    } catch {
      setError("Unable to copy recovery codes.");
    }
  }

  useEffect(() => {
    void loadStatus();
  }, []);

  const activeCodeCount = status?.activeCodeCount ?? 0;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Account security"
        title="Recovery codes"
        description="Generate one-time MFA recovery codes for this signed-in admin account."
      />

      <div className="grid gap-6 lg:grid-cols-[0.95fr_1.05fr]">
        <Card title="Recovery status" description="Recovery codes can be used once during an MFA challenge. Generating a new batch invalidates the previous active batch.">
          {isLoading ? (
            <LoadingState label="Loading recovery-code status..." />
          ) : (
            <div className="space-y-4">
              <ErrorBanner message={error} />
              <SuccessBanner message={success} />

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3">
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Active</p>
                  <p className="mt-2 text-2xl font-semibold text-white">{activeCodeCount}</p>
                </div>
                <div className="rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3">
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Generated</p>
                  <p className="mt-2 text-sm font-medium text-white">{formatDateTime(status?.lastGeneratedAt, "Never")}</p>
                </div>
              </div>

              <PrimaryButton type="button" disabled={isGenerating} onClick={() => void generateCodes()} className="inline-flex w-full items-center justify-center gap-2">
                {activeCodeCount > 0 ? <RefreshCw aria-hidden="true" className="size-4" /> : <ShieldCheck aria-hidden="true" className="size-4" />}
                <span>{isGenerating ? "Generating..." : activeCodeCount > 0 ? "Regenerate codes" : "Generate codes"}</span>
              </PrimaryButton>
            </div>
          )}
        </Card>

        <Card title="New recovery codes" description="Codes are only shown immediately after generation or regeneration.">
          {generatedCodes.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-slate-700 px-5 py-8 text-sm text-slate-400">
              No newly generated codes are available in this session.
            </div>
          ) : (
            <div className="space-y-4">
              <div className="grid gap-2 sm:grid-cols-2">
                {generatedCodes.map((code) => (
                  <code key={code} className="rounded-2xl border border-slate-800 bg-slate-950 px-3 py-2 text-center text-sm font-semibold text-emerald-200">
                    {code}
                  </code>
                ))}
              </div>
              <SecondaryButton type="button" onClick={() => void copyCodes()} className="inline-flex w-full items-center justify-center gap-2">
                <Copy aria-hidden="true" className="size-4" />
                <span>Copy codes</span>
              </SecondaryButton>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}

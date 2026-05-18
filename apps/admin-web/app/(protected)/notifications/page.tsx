"use client";

import { useEffect, useState } from "react";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { NotificationDashboard, NotificationJobDetail, NotificationJobListItem, NotificationProviderHealth } from "@/lib/types";
import { Badge, Card, EmptyState, ErrorBanner, Field, Input, LoadingState, PageHeader, PrimaryButton, SecondaryButton, Select, SuccessBanner } from "@/components/ui";

const statusOptions = ["Pending", "Failed", "DeadLetter", "Abandoned", "Sent"];

type NotificationJobsEnvelope = {
  items: NotificationJobListItem[];
};

type ProcessNotificationsResponse = {
  processedCount: number;
};

type NotificationJobFilters = {
  status: string;
  eventType: string;
  createdFrom: string;
  createdTo: string;
};

function statusTone(status: string): "default" | "success" | "warning" | "danger" {
  if (status === "Sent") return "success";
  if (status === "Failed" || status === "DeadLetter") return "danger";
  if (status === "Pending" || status === "Abandoned") return "warning";
  return "default";
}

function toUtcQueryValue(value: string) {
  if (!value) return null;
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString();
}

function buildJobsPath(filters: NotificationJobFilters) {
  const query = new URLSearchParams();
  if (filters.status) query.set("status", filters.status);
  if (filters.eventType.trim()) query.set("eventType", filters.eventType.trim());

  const createdFrom = toUtcQueryValue(filters.createdFrom);
  const createdTo = toUtcQueryValue(filters.createdTo);
  if (createdFrom) query.set("createdFrom", createdFrom);
  if (createdTo) query.set("createdTo", createdTo);

  const queryString = query.toString();
  return queryString ? `/api/admin/notifications/jobs?${queryString}` : "/api/admin/notifications/jobs";
}

export default function NotificationsPage() {
  const [jobs, setJobs] = useState<NotificationJobListItem[]>([]);
  const [dashboard, setDashboard] = useState<NotificationDashboard | null>(null);
  const [providerHealth, setProviderHealth] = useState<NotificationProviderHealth | null>(null);
  const [status, setStatus] = useState("");
  const [eventType, setEventType] = useState("");
  const [createdFrom, setCreatedFrom] = useState("");
  const [createdTo, setCreatedTo] = useState("");
  const [selectedJob, setSelectedJob] = useState<NotificationJobDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(false);
  const [loadingJobId, setLoadingJobId] = useState<string | null>(null);
  const [mutatingJobId, setMutatingJobId] = useState<string | null>(null);

  async function loadJobs(nextFilters: NotificationJobFilters = { status, eventType, createdFrom, createdTo }) {
    setError(null);
    setIsLoading(true);

    try {
      const [jobsResponse, dashboardData, healthData] = await Promise.all([
        apiRequest<NotificationJobsEnvelope>(buildJobsPath(nextFilters)),
        apiRequest<NotificationDashboard>("/api/admin/notifications/dashboard").catch(() => null),
        apiRequest<NotificationProviderHealth>("/api/admin/notifications/provider/health").catch(() => null),
      ]);
      setJobs(jobsResponse.items);
      setDashboard(dashboardData);
      setProviderHealth(healthData);
      setSelectedJob(null);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load notification jobs.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadJobs({ status, eventType, createdFrom, createdTo });
  }, [status, eventType, createdFrom, createdTo]);

  async function processNotifications() {
    setError(null);
    setSuccess(null);
    setIsProcessing(true);

    try {
      const response = await apiRequest<ProcessNotificationsResponse>("/api/admin/notifications/process", {
        method: "POST",
        body: JSON.stringify({})
      });
      setSuccess(`Processed ${response.processedCount} notification${response.processedCount === 1 ? "" : "s"}.`);
      await loadJobs();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to process notifications.");
    } finally {
      setIsProcessing(false);
    }
  }

  async function toggleAttempts(jobId: string) {
    if (selectedJob?.id === jobId) {
      setSelectedJob(null);
      return;
    }

    setError(null);
    setSuccess(null);
    setLoadingJobId(jobId);

    try {
      const response = await apiRequest<NotificationJobDetail>(`/api/admin/notifications/jobs/${jobId}`);
      setSelectedJob(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load delivery attempts.");
    } finally {
      setLoadingJobId(null);
    }
  }

  async function mutateJob(jobId: string, action: "requeue" | "abandon") {
    setError(null);
    setSuccess(null);
    setMutatingJobId(jobId);

    try {
      await apiRequest<NotificationJobListItem>(`/api/admin/notifications/jobs/${jobId}/${action}`, {
        method: "POST",
        body: JSON.stringify({})
      });
      setSuccess(action === "requeue" ? "Notification job requeued." : "Notification job abandoned.");
      await loadJobs();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : `Failed to ${action} notification job.`);
    } finally {
      setMutatingJobId(null);
    }
  }

  function clearFilters() {
    setStatus("");
    setEventType("");
    setCreatedFrom("");
    setCreatedTo("");
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader
        eyebrow="Notifications"
        title="Notification jobs"
        description="Monitor delivery state, process pending notifications and manage permanently failed notifications."
        action={<PrimaryButton type="button" onClick={processNotifications} disabled={isProcessing}>{isProcessing ? "Processing..." : "Process notifications"}</PrimaryButton>}
      />

      {dashboard ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <Card title="Total jobs">
            <p className="text-3xl font-bold text-white">
              {dashboard.statusCounts.reduce((sum, s) => sum + s.count, 0)}
            </p>
            <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-400">
              {dashboard.statusCounts.map((s) => (
                <span key={s.status}>{s.status}: <strong className="text-slate-200">{s.count}</strong></span>
              ))}
            </div>
          </Card>
          <Card title="Dead-letter by type">
            {dashboard.deadLetterByEventType.length === 0 ? (
              <p className="text-sm text-slate-400">None</p>
            ) : (
              <div className="space-y-1 text-xs">
                {dashboard.deadLetterByEventType.slice(0, 5).map((d) => (
                  <div key={d.eventType} className="flex justify-between">
                    <span className="text-slate-400">{d.eventType}</span>
                    <strong className="text-rose-300">{d.count}</strong>
                  </div>
                ))}
              </div>
            )}
          </Card>
          <Card title="Dead-letter age">
            <dl className="space-y-1 text-xs">
              <div className="flex justify-between"><span className="text-slate-400">Today</span><strong className="text-white">{dashboard.deadLetterToday}</strong></div>
              <div className="flex justify-between"><span className="text-slate-400">Last 7 days</span><strong className="text-white">{dashboard.deadLetterLast7Days}</strong></div>
              <div className="flex justify-between"><span className="text-slate-400">Older</span><strong className="text-white">{dashboard.deadLetterOlder}</strong></div>
            </dl>
          </Card>
          <Card title="Provider & delivery">
            <dl className="space-y-1 text-xs">
              <div className="flex justify-between"><span className="text-slate-400">Provider</span><strong className="text-white">{providerHealth?.providerType ?? "-"}</strong></div>
              <div className="flex justify-between"><span className="text-slate-400">Success rate</span><strong className="text-white">{dashboard.successRate}%</strong></div>
              <div className="flex justify-between"><span className="text-slate-400">Attempts</span><strong className="text-white">{dashboard.totalDeliveryAttempts}</strong></div>
              <div className="flex justify-between"><span className="text-slate-400">Configured</span><strong className={providerHealth?.isConfigured ? "text-emerald-300" : "text-rose-300"}>{providerHealth?.isConfigured ? "Yes" : "No"}</strong></div>
            </dl>
          </Card>
        </div>
      ) : null}

      <ErrorBanner message={error} />
      <SuccessBanner message={success} />

      <Card title="Filters">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-[220px_minmax(220px,1fr)_220px_220px_auto_auto]">
          <Field label="Status">
            <Select value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">All statuses</option>
              {statusOptions.map((option) => (
                <option key={option} value={option}>{option}</option>
              ))}
            </Select>
          </Field>
          <Field label="Event type">
            <Input value={eventType} onChange={(event) => setEventType(event.target.value)} placeholder="VisitClosed" />
          </Field>
          <Field label="Created from">
            <Input type="datetime-local" value={createdFrom} onChange={(event) => setCreatedFrom(event.target.value)} />
          </Field>
          <Field label="Created to">
            <Input type="datetime-local" value={createdTo} onChange={(event) => setCreatedTo(event.target.value)} />
          </Field>
          <SecondaryButton type="button" className="md:self-end" onClick={clearFilters} disabled={isLoading}>
            Clear
          </SecondaryButton>
          <SecondaryButton type="button" className="md:self-end" onClick={() => void loadJobs()} disabled={isLoading}>
            Refresh
          </SecondaryButton>
        </div>
      </Card>

      <Card title="Jobs">
        {isLoading ? <LoadingState label="Loading notification jobs..." /> : null}
        {!isLoading && jobs.length === 0 ? <EmptyState title="No notification jobs found" description="Process pending notifications or adjust the filters." /> : null}
        {!isLoading && jobs.length > 0 ? (
          <div className="grid gap-3">
            {jobs.map((job) => {
              const canRequeue = job.status === "DeadLetter" || job.status === "Abandoned";
              const canAbandon = job.status === "DeadLetter";
              const isMutating = mutatingJobId === job.id;
              const isLoadingAttempts = loadingJobId === job.id;
              const details = selectedJob?.id === job.id ? selectedJob : null;

              return (
                <div key={job.id} className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-medium text-white">{job.sourceEventType || "Unknown event"}</h3>
                        <Badge tone={statusTone(job.status)}>{job.status}</Badge>
                      </div>
                      <p className="mt-1 break-all text-xs text-slate-500">{job.id}</p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <SecondaryButton type="button" onClick={() => void toggleAttempts(job.id)} disabled={isLoadingAttempts}>
                        {details ? "Hide attempts" : isLoadingAttempts ? "Loading..." : "Attempts"}
                      </SecondaryButton>
                      {canRequeue ? (
                        <PrimaryButton type="button" onClick={() => void mutateJob(job.id, "requeue")} disabled={isMutating}>
                          Requeue
                        </PrimaryButton>
                      ) : null}
                      {canAbandon ? (
                        <SecondaryButton type="button" onClick={() => void mutateJob(job.id, "abandon")} disabled={isMutating}>
                          Abandon
                        </SecondaryButton>
                      ) : null}
                    </div>
                  </div>

                  <dl className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Recipient</dt>
                      <dd className="mt-1 break-all text-slate-200">{job.recipient}</dd>
                    </div>
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Channel</dt>
                      <dd className="mt-1 text-slate-200">{job.channel}</dd>
                    </div>
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Attempts</dt>
                      <dd className="mt-1 text-slate-200">{job.attemptCount}</dd>
                    </div>
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Created</dt>
                      <dd className="mt-1 text-slate-200">{formatDateTime(job.createdAt)}</dd>
                    </div>
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Sent</dt>
                      <dd className="mt-1 text-slate-200">{formatDateTime(job.sentAt)}</dd>
                    </div>
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Next attempt</dt>
                      <dd className="mt-1 text-slate-200">{formatDateTime(job.nextAttemptAt)}</dd>
                    </div>
                    <div>
                      <dt className="text-xs uppercase text-slate-500">Dead-lettered</dt>
                      <dd className="mt-1 text-slate-200">{formatDateTime(job.deadLetteredAt)}</dd>
                    </div>
                  </dl>

                  {job.lastErrorMessage ? (
                    <p className="mt-4 rounded-2xl border border-rose-900/50 bg-rose-950/30 px-3 py-2 text-xs text-rose-200">{job.lastErrorMessage}</p>
                  ) : null}

                  {details ? (
                    <div className="mt-4 border-t border-slate-800 pt-4">
                      <div className="flex flex-wrap items-center justify-between gap-3">
                        <h4 className="text-sm font-medium text-white">Delivery attempts</h4>
                        <span className="text-xs text-slate-500">{details.attempts.length} recorded</span>
                      </div>
                      {details.attempts.length === 0 ? (
                        <p className="mt-3 rounded-2xl border border-dashed border-slate-800 px-4 py-3 text-xs text-slate-400">No delivery attempts recorded.</p>
                      ) : (
                        <div className="mt-3 overflow-x-auto">
                          <table className="min-w-full text-left text-xs">
                            <thead className="text-slate-500">
                              <tr>
                                <th className="whitespace-nowrap py-2 pr-4 font-medium">Attempt</th>
                                <th className="whitespace-nowrap py-2 pr-4 font-medium">Status</th>
                                <th className="whitespace-nowrap py-2 pr-4 font-medium">Attempted</th>
                                <th className="py-2 font-medium">Error</th>
                              </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-800 text-slate-200">
                              {details.attempts.map((attempt) => (
                                <tr key={attempt.id}>
                                  <td className="whitespace-nowrap py-2 pr-4">{attempt.attemptNo}</td>
                                  <td className="whitespace-nowrap py-2 pr-4"><Badge tone={statusTone(attempt.status)}>{attempt.status}</Badge></td>
                                  <td className="whitespace-nowrap py-2 pr-4">{formatDateTime(attempt.attemptedAt)}</td>
                                  <td className="min-w-[240px] py-2 text-slate-300">{attempt.errorMessage || "-"}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

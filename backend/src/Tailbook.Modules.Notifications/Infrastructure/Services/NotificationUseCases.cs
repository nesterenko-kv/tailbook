using System.Diagnostics;
using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications.Infrastructure.Options;
using Tailbook.Modules.Notifications.Infrastructure.Telemetry;

namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class NotificationUseCases(
    AppDbContext dbContext,
    INotificationSink notificationSink,
    IOptions<NotificationsOptions> options,
    TimeProvider? timeProvider = null,
    IAuditTrailService? auditTrailService = null,
    ISensitivePayloadProtector? sensitivePayloadProtector = null) : INotificationReadService
{
    private const string RedactedValue = "[redacted]";
    private readonly NotificationsOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public NotificationUseCases(AppDbContext dbContext, INotificationSink notificationSink)
        : this(dbContext, notificationSink, Microsoft.Extensions.Options.Options.Create(new NotificationsOptions()), TimeProvider.System)
    {
    }

    public Task<int> ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        return ProcessOutboxAsync(NotificationTelemetry.TriggerManual, cancellationToken);
    }

    public async Task<int> ProcessOutboxAsync(string trigger, CancellationToken cancellationToken)
    {
        var stopwatch = ValueStopwatch.StartNew();
        using var activity = NotificationTelemetry.StartOutboxProcessingActivity(trigger);
        var processed = 0;
        var sentCount = 0;
        var failedCount = 0;
        var deadLetterCount = 0;
        var ignoredCount = 0;
        var skippedRetryCount = 0;
        var cycleResult = NotificationTelemetry.ResultIdle;

        try
        {
            var messages = await dbContext.Set<OutboxMessage>()
                .Where(x => x.ProcessedAt == null)
                .OrderBy(x => x.OccurredAt)
                .Take(100)
                .ToListAsync(cancellationToken);
            NotificationTelemetry.SetOutboxAvailableCount(activity, messages.Count);

            if (messages.Count == 0)
            {
                return 0;
            }

            var templates = await dbContext.Set<NotificationTemplate>()
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var message in messages)
            {
                var utcNow = _timeProvider.GetUtcNow();
                var templateCode = ResolveTemplateCode(message.EventType);
                if (templateCode is null || !templates.TryGetValue(templateCode, out var template))
                {
                    message.ProcessedAt = utcNow;
                    processed++;
                    ignoredCount++;
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeIgnored);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var job = await dbContext.Set<NotificationJob>()
                    .SingleOrDefaultAsync(x => x.SourceEventMessageId == message.Id, cancellationToken);

                if (job is null)
                {
                    var body = RenderJobContent(template.BodyTemplate, message.PayloadJson, templateCode);
                    var subject = RenderJobContent(template.SubjectTemplate, message.PayloadJson, templateCode);
                    job = new NotificationJob
                    {
                        Id = Guid.NewGuid(),
                        SourceEventType = message.EventType,
                        SourceEventMessageId = message.Id,
                        TemplateId = template.Id,
                        Channel = template.Channel,
                        Recipient = ResolveRecipient(message.PayloadJson) ?? "front-desk",
                        Subject = subject,
                        Body = body,
                        Status = NotificationStatusCodes.Pending,
                        AttemptCount = 0,
                        CreatedAt = utcNow
                    };

                    dbContext.Set<NotificationJob>().Add(job);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                else if (job.Status == NotificationStatusCodes.Sent)
                {
                    message.ProcessedAt = utcNow;
                    processed++;
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeAlreadyFinal);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }
                else if (job.Status == NotificationStatusCodes.DeadLetter)
                {
                    message.ProcessedAt ??= utcNow;
                    processed++;
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeAlreadyFinal);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }
                else if (job.Status == NotificationStatusCodes.Abandoned)
                {
                    message.ProcessedAt ??= utcNow;
                    processed++;
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeAlreadyFinal);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }
                else if (job.AttemptCount >= _options.MaxDeliveryAttempts)
                {
                    MarkDeadLetter(job, message, job.LastErrorMessage ?? "Notification delivery attempts exhausted before retry.", utcNow);
                    processed++;
                    deadLetterCount++;
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeDeadLetter);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                if (job.NextAttemptAt is { } nextAttemptAt && nextAttemptAt > utcNow)
                {
                    skippedRetryCount++;
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeSkippedRetry);
                    continue;
                }

                var deliveryAttemptNo = await GetNextDeliveryAttemptNoAsync(job.Id, cancellationToken);
                try
                {
                    var envelope = CreateDispatchEnvelope(job, template, message.PayloadJson, utcNow, templateCode);
                    await notificationSink.SendAsync(envelope, cancellationToken);
                    job.AttemptCount += 1;
                    job.Status = NotificationStatusCodes.Sent;
                    job.LastErrorMessage = null;
                    job.NextAttemptAt = null;
                    job.DeadLetteredAt = null;
                    job.SentAt = utcNow;
                    dbContext.Set<NotificationDeliveryAttempt>().Add(new NotificationDeliveryAttempt { Id = Guid.NewGuid(), NotificationJobId = job.Id, AttemptNo = deliveryAttemptNo, Status = NotificationStatusCodes.Sent, AttemptedAt = job.SentAt.Value });
                    message.ProcessedAt = utcNow;
                    sentCount++;
                    NotificationTelemetry.RecordDeliveryAttempt(NotificationStatusCodes.Sent, job.Channel);
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeSent);
                }
                catch (Exception ex)
                {
                    job.AttemptCount += 1;
                    job.LastErrorMessage = TruncateError(ex.Message);
                    var attemptStatus = NotificationStatusCodes.Failed;
                    if (job.AttemptCount >= _options.MaxDeliveryAttempts)
                    {
                        attemptStatus = NotificationStatusCodes.DeadLetter;
                        MarkDeadLetter(job, message, job.LastErrorMessage, utcNow);
                        deadLetterCount++;
                    }
                    else
                    {
                        job.Status = NotificationStatusCodes.Failed;
                        job.NextAttemptAt = utcNow.Add(CalculateRetryDelay(job.AttemptCount));
                        job.DeadLetteredAt = null;
                        failedCount++;
                    }

                    dbContext.Set<NotificationDeliveryAttempt>().Add(new NotificationDeliveryAttempt { Id = Guid.NewGuid(), NotificationJobId = job.Id, AttemptNo = deliveryAttemptNo, Status = attemptStatus, ErrorMessage = job.LastErrorMessage, AttemptedAt = utcNow });
                    NotificationTelemetry.RecordDeliveryAttempt(attemptStatus, job.Channel);
                    NotificationTelemetry.RecordOutboxMessageOutcome(attemptStatus == NotificationStatusCodes.DeadLetter
                        ? NotificationTelemetry.OutcomeDeadLetter
                        : NotificationTelemetry.OutcomeFailed);
                }

                processed++;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            cycleResult = processed > 0
                ? NotificationTelemetry.ResultProcessed
                : skippedRetryCount > 0
                    ? NotificationTelemetry.ResultSkipped
                    : NotificationTelemetry.ResultIdle;
            return processed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cycleResult = NotificationTelemetry.ResultCanceled;
            throw;
        }
        catch (Exception ex)
        {
            cycleResult = NotificationTelemetry.ResultError;
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
        finally
        {
            var duration = stopwatch.GetElapsedTime();
            NotificationTelemetry.SetOutboxProcessingCounts(activity, processed, sentCount, failedCount, deadLetterCount, ignoredCount, skippedRetryCount);
            NotificationTelemetry.SetOutboxProcessingResult(activity, cycleResult, duration);
            NotificationTelemetry.RecordOutboxProcessingCycle(trigger, processed, duration, cycleResult);
        }
    }

    public async Task<ProcessBrokerEventResult> ProcessBrokerEventAsync(
        string eventType,
        string payloadJson,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var stopwatch = ValueStopwatch.StartNew();
        var utcNow = _timeProvider.GetUtcNow();
        var result = new ProcessBrokerEventResult();

        using var activity = NotificationTelemetry.StartOutboxProcessingActivity("broker");

        try
        {
            var templates = await dbContext.Set<NotificationTemplate>()
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var templateCode = ResolveTemplateCode(eventType);
            if (templateCode is null || !templates.TryGetValue(templateCode, out var template))
            {
                NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeIgnored);
                return result with { Outcome = "ignored" };
            }

            var message = new OutboxMessage
            {
                Id = messageId,
                EventType = eventType,
                PayloadJson = payloadJson
            };

            var job = await dbContext.Set<NotificationJob>()
                .SingleOrDefaultAsync(x => x.SourceEventMessageId == messageId, cancellationToken);

            if (job is null)
            {
                var body = RenderJobContent(template.BodyTemplate, payloadJson, templateCode);
                var subject = RenderJobContent(template.SubjectTemplate, payloadJson, templateCode);
                job = new NotificationJob
                {
                    Id = Guid.NewGuid(),
                    SourceEventType = eventType,
                    SourceEventMessageId = messageId,
                    TemplateId = template.Id,
                    Channel = template.Channel,
                    Recipient = ResolveRecipient(payloadJson) ?? "front-desk",
                    Subject = subject,
                    Body = body,
                    Status = NotificationStatusCodes.Pending,
                    AttemptCount = 0,
                    CreatedAt = utcNow
                };

                dbContext.Set<NotificationJob>().Add(job);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (job.Status is NotificationStatusCodes.Sent
                or NotificationStatusCodes.DeadLetter
                or NotificationStatusCodes.Abandoned)
            {
                NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeAlreadyFinal);
                return result with { Outcome = "already_final" };
            }

            if (job.NextAttemptAt is { } nextAttemptAt && nextAttemptAt > utcNow)
            {
                NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeSkippedRetry);
                return result with { Outcome = "skipped_retry" };
            }

            var deliveryAttemptNo = await GetNextDeliveryAttemptNoAsync(job.Id, cancellationToken);

            try
            {
                var envelope = CreateDispatchEnvelope(job, template, payloadJson, utcNow, templateCode);
                await notificationSink.SendAsync(envelope, cancellationToken);

                job.AttemptCount += 1;
                job.Status = NotificationStatusCodes.Sent;
                job.LastErrorMessage = null;
                job.NextAttemptAt = null;
                job.DeadLetteredAt = null;
                job.SentAt = utcNow;

                dbContext.Set<NotificationDeliveryAttempt>().Add(
                    new NotificationDeliveryAttempt
                    {
                        Id = Guid.NewGuid(),
                        NotificationJobId = job.Id,
                        AttemptNo = deliveryAttemptNo,
                        Status = NotificationStatusCodes.Sent,
                        AttemptedAt = utcNow
                    });

                await dbContext.SaveChangesAsync(cancellationToken);

                NotificationTelemetry.RecordDeliveryAttempt(NotificationStatusCodes.Sent, job.Channel);
                NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeSent);

                return result with { Outcome = "sent" };
            }
            catch (Exception ex)
            {
                job.AttemptCount += 1;
                job.LastErrorMessage = TruncateError(ex.Message);

                if (job.AttemptCount >= _options.MaxDeliveryAttempts)
                {
                    job.Status = NotificationStatusCodes.DeadLetter;
                    job.NextAttemptAt = null;
                    job.DeadLetteredAt = utcNow;
                    await dbContext.SaveChangesAsync(cancellationToken);

                    NotificationTelemetry.RecordDeliveryAttempt(NotificationStatusCodes.DeadLetter, job.Channel);
                    NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeDeadLetter);

                    return result with { Outcome = "dead_letter", ErrorMessage = ex.Message };
                }

                job.Status = NotificationStatusCodes.Failed;
                job.NextAttemptAt = utcNow.Add(CalculateRetryDelay(job.AttemptCount));
                job.DeadLetteredAt = null;

                dbContext.Set<NotificationDeliveryAttempt>().Add(
                    new NotificationDeliveryAttempt
                    {
                        Id = Guid.NewGuid(),
                        NotificationJobId = job.Id,
                        AttemptNo = deliveryAttemptNo,
                        Status = NotificationStatusCodes.Failed,
                        ErrorMessage = job.LastErrorMessage,
                        AttemptedAt = utcNow
                    });

                await dbContext.SaveChangesAsync(cancellationToken);

                NotificationTelemetry.RecordDeliveryAttempt(NotificationStatusCodes.Failed, job.Channel);
                NotificationTelemetry.RecordOutboxMessageOutcome(NotificationTelemetry.OutcomeFailed);

                return result with { Outcome = "failed", ErrorMessage = ex.Message };
            }
        }
        finally
        {
            var duration = stopwatch.GetElapsedTime();
            NotificationTelemetry.SetOutboxProcessingCounts(activity, result.Outcome is not null ? 1 : 0, result.Outcome == "sent" ? 1 : 0, result.Outcome == "failed" ? 1 : 0, result.Outcome == "dead_letter" ? 1 : 0, result.Outcome == "ignored" ? 1 : 0, result.Outcome == "skipped_retry" ? 1 : 0);
            NotificationTelemetry.SetOutboxProcessingResult(activity, result.Outcome ?? "error", duration);
            NotificationTelemetry.RecordOutboxProcessingCycle("broker", 1, duration, result.Outcome ?? "error");
        }
    }

    public async Task<IReadOnlyCollection<NotificationJobListItemView>> ListJobsAsync(NotificationJobListQuery query, CancellationToken cancellationToken)
    {
        var jobs = dbContext.Set<NotificationJob>().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            jobs = jobs.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            var eventType = query.EventType.Trim();
            jobs = jobs.Where(x => x.SourceEventType.Contains(eventType));
        }

        if (query.CreatedFrom.HasValue)
        {
            jobs = jobs.Where(x => x.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            jobs = jobs.Where(x => x.CreatedAt <= query.CreatedTo.Value);
        }

        return await jobs.OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new NotificationJobListItemView(x.Id, x.SourceEventType, x.Channel, x.Recipient, x.Status, x.AttemptCount, x.LastErrorMessage, x.CreatedAt, x.SentAt, x.NextAttemptAt, x.DeadLetteredAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationDashboardView> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var todayStart = now.Date;
        var sevenDaysAgo = todayStart.AddDays(-7);

        var jobs = dbContext.Set<NotificationJob>().AsNoTracking();

        var statusCounts = await jobs
            .GroupBy(x => x.Status)
            .Select(g => new NotificationStatusCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var deadLetterJobs = jobs.Where(x => x.Status == NotificationStatusCodes.DeadLetter);

        var deadLetterByEvent = await deadLetterJobs
            .GroupBy(x => x.SourceEventType)
            .Select(g => new NotificationDeadLetterByEvent(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var deadLetterToday = await deadLetterJobs.CountAsync(x => x.DeadLetteredAt >= todayStart, cancellationToken);
        var deadLetterLast7Days = await deadLetterJobs.CountAsync(x => x.DeadLetteredAt >= sevenDaysAgo && x.DeadLetteredAt < todayStart, cancellationToken);
        var deadLetterOlder = await deadLetterJobs.CountAsync(x => x.DeadLetteredAt < sevenDaysAgo, cancellationToken);

        var attempts = dbContext.Set<NotificationDeliveryAttempt>().AsNoTracking();
        var totalDeliveryAttempts = await attempts.CountAsync(cancellationToken);
        var successfulDeliveries = await attempts.CountAsync(x => x.Status == NotificationStatusCodes.Sent, cancellationToken);
        var successRate = totalDeliveryAttempts > 0 ? Math.Round((double)successfulDeliveries / totalDeliveryAttempts * 100, 1) : 0;

        return new NotificationDashboardView(
            statusCounts,
            deadLetterByEvent,
            deadLetterToday,
            deadLetterLast7Days,
            deadLetterOlder,
            totalDeliveryAttempts,
            successfulDeliveries,
            successRate);
    }

    public async Task<NotificationProviderHealthView> GetProviderHealthAsync(CancellationToken cancellationToken)
    {
        var lastAttempt = await dbContext.Set<NotificationDeliveryAttempt>()
            .AsNoTracking()
            .OrderByDescending(x => x.AttemptedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var recentAttempts = dbContext.Set<NotificationDeliveryAttempt>()
            .AsNoTracking()
            .Where(x => lastAttempt != null ? x.AttemptedAt >= lastAttempt.AttemptedAt.AddDays(-1) : false);

        var lastDayAttempts = await recentAttempts.CountAsync(cancellationToken);
        var lastDayFailures = await recentAttempts.CountAsync(x => x.Status != NotificationStatusCodes.Sent, cancellationToken);

        return new NotificationProviderHealthView(
            _options.Provider,
            NotificationsOptions.IsSmtpProvider(_options.Provider)
                ? !string.IsNullOrWhiteSpace(_options.SmtpHost)
                : !string.IsNullOrWhiteSpace(_options.LocalFilePath),
            lastAttempt?.Status ?? "none",
            lastDayAttempts,
            lastDayFailures,
            lastAttempt?.ErrorMessage);
    }

    public async Task<NotificationJobDetailView?> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await dbContext.Set<NotificationJob>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        if (job is null)
        {
            return null;
        }

        var attempts = await dbContext.Set<NotificationDeliveryAttempt>()
            .AsNoTracking()
            .Where(x => x.NotificationJobId == jobId)
            .OrderByDescending(x => x.AttemptNo)
            .Select(x => new NotificationDeliveryAttemptView(x.Id, x.AttemptNo, x.Status, x.ErrorMessage, x.AttemptedAt))
            .ToListAsync(cancellationToken);

        return new NotificationJobDetailView(
            job.Id,
            job.SourceEventType,
            job.Channel,
            job.Recipient,
            job.Status,
            job.AttemptCount,
            job.LastErrorMessage,
            job.CreatedAt,
            job.SentAt,
            job.NextAttemptAt,
            job.DeadLetteredAt,
            attempts);
    }

    public async Task<ErrorOr<NotificationJobListItemView>> RequeueJobAsync(Guid jobId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var job = await dbContext.Set<NotificationJob>().SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
        {
            return Error.NotFound("Notifications.JobNotFound", "Notification job does not exist.");
        }

        if (job.Status is not (NotificationStatusCodes.DeadLetter or NotificationStatusCodes.Abandoned))
        {
            return Error.Conflict("Notifications.JobNotStopped", "Only dead-lettered or abandoned notification jobs can be requeued.");
        }

        var message = await GetSourceOutboxMessageAsync(job, cancellationToken);
        if (message is null)
        {
            return Error.Conflict("Notifications.SourceOutboxMissing", "Notification job cannot be requeued because its source outbox message is missing.");
        }

        var before = ToAuditSnapshot(job);
        job.Status = NotificationStatusCodes.Pending;
        job.AttemptCount = 0;
        job.NextAttemptAt = null;
        job.DeadLetteredAt = null;
        job.SentAt = null;
        message.ProcessedAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(job.Id, "REQUEUE", actorUserId, before, ToAuditSnapshot(job), cancellationToken);

        return ToListItem(job);
    }

    public async Task<ErrorOr<NotificationJobListItemView>> AbandonJobAsync(Guid jobId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var job = await dbContext.Set<NotificationJob>().SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
        {
            return Error.NotFound("Notifications.JobNotFound", "Notification job does not exist.");
        }

        if (job.Status != NotificationStatusCodes.DeadLetter)
        {
            return Error.Conflict("Notifications.JobNotDeadLetter", "Only dead-lettered notification jobs can be abandoned.");
        }

        var before = ToAuditSnapshot(job);
        job.Status = NotificationStatusCodes.Abandoned;
        job.NextAttemptAt = null;

        if (await GetSourceOutboxMessageAsync(job, cancellationToken) is { } message)
        {
            message.ProcessedAt ??= _timeProvider.GetUtcNow();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordAuditAsync(job.Id, "ABANDON", actorUserId, before, ToAuditSnapshot(job), cancellationToken);

        return ToListItem(job);
    }

    private void MarkDeadLetter(NotificationJob job, OutboxMessage message, string errorMessage, DateTimeOffset utcNow)
    {
        job.Status = NotificationStatusCodes.DeadLetter;
        job.LastErrorMessage = TruncateError(errorMessage);
        job.NextAttemptAt = null;
        job.DeadLetteredAt = utcNow;
        message.ProcessedAt = utcNow;
    }

    private async Task<OutboxMessage?> GetSourceOutboxMessageAsync(NotificationJob job, CancellationToken cancellationToken)
    {
        if (job.SourceEventMessageId is null)
        {
            return null;
        }

        return await dbContext.Set<OutboxMessage>()
            .SingleOrDefaultAsync(x => x.Id == job.SourceEventMessageId, cancellationToken);
    }

    private async Task<int> GetNextDeliveryAttemptNoAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var maxAttemptNo = await dbContext.Set<NotificationDeliveryAttempt>()
            .Where(x => x.NotificationJobId == jobId)
            .MaxAsync(x => (int?)x.AttemptNo, cancellationToken);

        return (maxAttemptNo ?? 0) + 1;
    }

    private async Task RecordAuditAsync(Guid jobId, string actionCode, Guid? actorUserId, string beforeJson, string afterJson, CancellationToken cancellationToken)
    {
        if (auditTrailService is null)
        {
            return;
        }

        await auditTrailService.RecordAsync("notifications", "notification_job", jobId.ToString("D"), actionCode, actorUserId, beforeJson, afterJson, cancellationToken);
    }

    private static string ToAuditSnapshot(NotificationJob job)
    {
        return JsonSerializer.Serialize(new
        {
            job.Id,
            job.SourceEventType,
            job.Status,
            job.AttemptCount,
            job.NextAttemptAt,
            job.DeadLetteredAt
        });
    }

    private static NotificationJobListItemView ToListItem(NotificationJob job)
    {
        return new NotificationJobListItemView(job.Id, job.SourceEventType, job.Channel, job.Recipient, job.Status, job.AttemptCount, job.LastErrorMessage, job.CreatedAt, job.SentAt, job.NextAttemptAt, job.DeadLetteredAt);
    }

    private NotificationDispatchEnvelope CreateDispatchEnvelope(
        NotificationJob job,
        NotificationTemplate template,
        string payloadJson,
        DateTimeOffset utcNow,
        string templateCode)
    {
        if (!IsPasswordResetTemplate(templateCode) && !IsMfaEmailOtpTemplate(templateCode))
        {
            return new NotificationDispatchEnvelope(job.Id, job.Channel, job.Recipient, job.Subject, job.Body, utcNow);
        }

        if (IsMfaEmailOtpTemplate(templateCode))
        {
            var mfaReplacements = CreateMfaEmailOtpDispatchReplacements(payloadJson);
            var mfaSubject = RenderTemplate(template.SubjectTemplate, payloadJson, mfaReplacements, SensitiveMfaEmailOtpPayloadProperties);
            var mfaBody = RenderTemplate(template.BodyTemplate, payloadJson, mfaReplacements, SensitiveMfaEmailOtpPayloadProperties);
            return new NotificationDispatchEnvelope(job.Id, job.Channel, job.Recipient, mfaSubject, mfaBody, utcNow);
        }

        var replacements = CreatePasswordResetDispatchReplacements(payloadJson);
        var subject = RenderTemplate(template.SubjectTemplate, payloadJson, replacements, SensitivePasswordResetPayloadProperties);
        var body = RenderTemplate(template.BodyTemplate, payloadJson, replacements, SensitivePasswordResetPayloadProperties);
        return new NotificationDispatchEnvelope(job.Id, job.Channel, job.Recipient, subject, body, utcNow);
    }

    private string RenderJobContent(string template, string payloadJson, string templateCode)
    {
        if (!IsPasswordResetTemplate(templateCode) && !IsMfaEmailOtpTemplate(templateCode))
        {
            return RenderTemplate(template, payloadJson);
        }

        if (IsMfaEmailOtpTemplate(templateCode))
        {
            return RenderTemplate(
                template,
                payloadJson,
                MfaEmailOtpRedactionReplacements,
                SensitiveMfaEmailOtpPayloadProperties);
        }

        return RenderTemplate(
            template,
            payloadJson,
            PasswordResetRedactionReplacements,
            SensitivePasswordResetPayloadProperties);
    }

    private TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var exponent = Math.Min(Math.Max(0, attemptCount - 1), 20);
        var multiplier = Math.Pow(2, exponent);
        var delaySeconds = Math.Min(_options.RetryMaxDelaySeconds, _options.RetryBaseDelaySeconds * multiplier);
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private static string TruncateError(string message)
    {
        return message.Length > 1024 ? message[..1024] : message;
    }

    private static string? ResolveTemplateCode(string eventType)
    {
        if (eventType.Contains("AppointmentCreated", StringComparison.OrdinalIgnoreCase)) return "APPOINTMENT_CREATED";
        if (eventType.Contains("VisitClosed", StringComparison.OrdinalIgnoreCase)) return "VISIT_CLOSED";
        if (eventType.Contains("PasswordResetRequested", StringComparison.OrdinalIgnoreCase)) return "PASSWORD_RESET_REQUESTED";
        if (eventType.Contains("MfaEmailOtpChallengeCreated", StringComparison.OrdinalIgnoreCase)) return "MFA_EMAIL_OTP_CHALLENGE";
        return null;
    }

    private static bool IsPasswordResetTemplate(string templateCode)
    {
        return string.Equals(templateCode, "PASSWORD_RESET_REQUESTED", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMfaEmailOtpTemplate(string templateCode)
    {
        return string.Equals(templateCode, "MFA_EMAIL_OTP_CHALLENGE", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveRecipient(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        if (document.RootElement.TryGetProperty("email", out var camelEmail))
        {
            return camelEmail.GetString();
        }

        if (document.RootElement.TryGetProperty("Email", out var pascalEmail))
        {
            return pascalEmail.GetString();
        }

        return null;
    }

    private Dictionary<string, string> CreatePasswordResetDispatchReplacements(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var protectedResetLink = ReadStringProperty(document.RootElement, "ProtectedResetLink");
        var resetLink = ReadStringProperty(document.RootElement, "ResetLink");
        var resetToken = ReadStringProperty(document.RootElement, "ResetToken");

        if (!string.IsNullOrWhiteSpace(protectedResetLink))
        {
            if (sensitivePayloadProtector is null)
            {
                throw new InvalidOperationException("Sensitive payload protector is not configured.");
            }

            resetLink = sensitivePayloadProtector.Unprotect(SensitivePayloadPurposes.PasswordResetLink, protectedResetLink);
        }

        if (string.IsNullOrWhiteSpace(resetToken) && !string.IsNullOrWhiteSpace(resetLink))
        {
            resetToken = TryReadTokenFromResetLink(resetLink);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ProtectedResetLink"] = RedactedValue,
            ["ResetLink"] = string.IsNullOrWhiteSpace(resetLink) ? RedactedValue : resetLink,
            ["ResetToken"] = string.IsNullOrWhiteSpace(resetToken) ? RedactedValue : resetToken
        };
    }

    private Dictionary<string, string> CreateMfaEmailOtpDispatchReplacements(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var protectedCode = ReadStringProperty(document.RootElement, "ProtectedCode");
        var code = ReadStringProperty(document.RootElement, "Code");

        if (!string.IsNullOrWhiteSpace(protectedCode))
        {
            if (sensitivePayloadProtector is null)
            {
                throw new InvalidOperationException("Sensitive payload protector is not configured.");
            }

            code = sensitivePayloadProtector.Unprotect(SensitivePayloadPurposes.MfaEmailOtpCode, protectedCode);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ProtectedCode"] = RedactedValue,
            ["Code"] = string.IsNullOrWhiteSpace(code) ? RedactedValue : code
        };
    }

    private static string? ReadStringProperty(JsonElement root, string propertyName)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static string? TryReadTokenFromResetLink(string resetLink)
    {
        if (!Uri.TryCreate(resetLink, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var query = uri.Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), "token", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private static string RenderTemplate(
        string template,
        string payloadJson,
        IReadOnlyDictionary<string, string>? replacements = null,
        ISet<string>? skippedPayloadProperties = null)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var result = template;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (skippedPayloadProperties?.Contains(property.Name) == true)
            {
                continue;
            }

            result = result.Replace("{{" + property.Name + "}}", property.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        if (replacements is not null)
        {
            foreach (var replacement in replacements)
            {
                result = result.Replace("{{" + replacement.Key + "}}", replacement.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }

    private static readonly HashSet<string> SensitivePasswordResetPayloadProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProtectedResetLink",
        "ResetLink",
        "ResetToken"
    };

    private static readonly Dictionary<string, string> PasswordResetRedactionReplacements = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProtectedResetLink"] = RedactedValue,
        ["ResetLink"] = RedactedValue,
        ["ResetToken"] = RedactedValue
    };

    private static readonly HashSet<string> SensitiveMfaEmailOtpPayloadProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProtectedCode",
        "Code"
    };

    private static readonly Dictionary<string, string> MfaEmailOtpRedactionReplacements = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProtectedCode"] = RedactedValue,
        ["Code"] = RedactedValue
    };
}

public sealed record ProcessBrokerEventResult
{
    public string? Outcome { get; init; }
    public string? ErrorMessage { get; init; }
}

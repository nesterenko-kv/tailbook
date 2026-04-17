using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications.Domain;

namespace Tailbook.Modules.Notifications.Application;

public sealed class NotificationsQueries(
    AppDbContext dbContext,
    LocalNotificationSink localNotificationSink)
{
    public async Task<PagedResult<NotificationJobView>> ListJobsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch { <= 0 => 20, > 100 => 100, _ => pageSize };

        var query = dbContext.Set<NotificationJob>().AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new NotificationJobView(x.Id, x.EventType, x.Channel, x.Recipient, x.Subject, x.Status, x.SourceOutboxMessageId, x.CreatedAtUtc, x.ProcessedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<NotificationJobView>(items, safePage, safePageSize, totalCount);
    }

    public async Task<OutboxProcessingResultView> ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        var outboxMessages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var processed = 0;
        var createdJobs = 0;

        foreach (var message in outboxMessages)
        {
            processed++;
            var notification = BuildNotification(message);
            if (notification is not null)
            {
                var utcNow = DateTime.UtcNow;
                var job = new NotificationJob
                {
                    Id = Guid.NewGuid(),
                    EventType = message.EventType,
                    Channel = "LocalLog",
                    Recipient = notification.Value.Recipient,
                    Subject = notification.Value.Subject,
                    Body = notification.Value.Body,
                    Status = "Sent",
                    SourceOutboxMessageId = message.Id,
                    CreatedAtUtc = utcNow,
                    ProcessedAtUtc = utcNow
                };

                dbContext.Set<NotificationJob>().Add(job);
                dbContext.Set<NotificationDeliveryAttempt>().Add(new NotificationDeliveryAttempt
                {
                    Id = Guid.NewGuid(),
                    NotificationJobId = job.Id,
                    AttemptNo = 1,
                    Status = "Sent",
                    AttemptedAtUtc = utcNow
                });

                await localNotificationSink.WriteAsync(new
                {
                    job.Id,
                    job.EventType,
                    job.Channel,
                    job.Recipient,
                    job.Subject,
                    job.Body,
                    job.CreatedAtUtc
                }, cancellationToken);

                createdJobs++;
            }

            message.ProcessedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new OutboxProcessingResultView(processed, createdJobs);
    }

    private static NotificationEnvelope? BuildNotification(OutboxMessage message)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(message.PayloadJson) ? "{}" : message.PayloadJson);
        var root = document.RootElement;

        if (string.Equals(message.EventType, "booking.appointment.created", StringComparison.OrdinalIgnoreCase))
        {
            var appointmentId = root.TryGetProperty("appointmentId", out var appointmentElement) ? appointmentElement.GetString() : null;
            var startAtUtc = root.TryGetProperty("startAtUtc", out var startElement) ? startElement.GetString() : null;
            return new NotificationEnvelope("frontdesk@local", "Appointment created", $"Appointment {appointmentId} was created for {startAtUtc} UTC.");
        }

        if (string.Equals(message.EventType, "visit.closed", StringComparison.OrdinalIgnoreCase))
        {
            var visitId = root.TryGetProperty("visitId", out var visitElement) ? visitElement.GetString() : null;
            var finalTotal = root.TryGetProperty("finalTotalAmount", out var totalElement) ? totalElement.GetDecimal() : 0m;
            return new NotificationEnvelope("frontdesk@local", "Visit closed", $"Visit {visitId} was closed. Final total: {finalTotal:0.00}.");
        }

        return new NotificationEnvelope("ops@local", $"Outbox event: {message.EventType}", $"Unhandled event from module {message.ModuleCode}: {message.EventType}");
    }

    private readonly record struct NotificationEnvelope(string Recipient, string Subject, string Body);
}

public sealed record NotificationJobView(Guid Id, string EventType, string Channel, string Recipient, string Subject, string Status, Guid? SourceOutboxMessageId, DateTime CreatedAtUtc, DateTime? ProcessedAtUtc);
public sealed record OutboxProcessingResultView(int ProcessedMessages, int CreatedJobs);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

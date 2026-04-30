using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications.Contracts;

namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class NotificationQueries(AppDbContext dbContext, INotificationSink notificationSink) : INotificationQueries
{
    public async Task<int> ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        var templates = await dbContext.Set<NotificationTemplate>()
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var processed = 0;
        foreach (var message in messages)
        {
            var templateCode = ResolveTemplateCode(message.EventType);
            if (templateCode is null || !templates.TryGetValue(templateCode, out var template))
            {
                message.ProcessedAtUtc = DateTime.UtcNow;
                processed++;
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            var job = await dbContext.Set<NotificationJob>()
                .SingleOrDefaultAsync(x => x.SourceEventMessageId == message.Id, cancellationToken);

            if (job is null)
            {
                var body = RenderTemplate(template.BodyTemplate, message.PayloadJson);
                var subject = RenderTemplate(template.SubjectTemplate, message.PayloadJson);
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
                    CreatedAtUtc = DateTime.UtcNow
                };

                dbContext.Set<NotificationJob>().Add(job);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (job.Status == NotificationStatusCodes.Sent)
            {
                message.ProcessedAtUtc = DateTime.UtcNow;
                processed++;
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                await notificationSink.SendAsync(new NotificationDispatchEnvelope(job.Id, job.Channel, job.Recipient, job.Subject, job.Body, DateTime.UtcNow), cancellationToken);
                job.AttemptCount += 1;
                job.Status = NotificationStatusCodes.Sent;
                job.LastErrorMessage = null;
                job.SentAtUtc = DateTime.UtcNow;
                dbContext.Set<NotificationDeliveryAttempt>().Add(new NotificationDeliveryAttempt { Id = Guid.NewGuid(), NotificationJobId = job.Id, AttemptNo = job.AttemptCount, Status = NotificationStatusCodes.Sent, AttemptedAtUtc = job.SentAtUtc.Value });
                message.ProcessedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                job.AttemptCount += 1;
                job.Status = NotificationStatusCodes.Failed;
                job.LastErrorMessage = TruncateError(ex.Message);
                dbContext.Set<NotificationDeliveryAttempt>().Add(new NotificationDeliveryAttempt { Id = Guid.NewGuid(), NotificationJobId = job.Id, AttemptNo = job.AttemptCount, Status = NotificationStatusCodes.Failed, ErrorMessage = job.LastErrorMessage, AttemptedAtUtc = DateTime.UtcNow });
            }

            processed++;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return processed;
    }

    public async Task<IReadOnlyCollection<NotificationJobListItemView>> ListJobsAsync(string? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<NotificationJob>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status.Trim());
        }

        return await query.OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => new NotificationJobListItemView(x.Id, x.SourceEventType, x.Channel, x.Recipient, x.Status, x.AttemptCount, x.LastErrorMessage, x.CreatedAtUtc, x.SentAtUtc))
            .ToListAsync(cancellationToken);
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
        return null;
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

    private static string RenderTemplate(string template, string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var result = template;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            result = result.Replace("{{" + property.Name + "}}", property.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }
}

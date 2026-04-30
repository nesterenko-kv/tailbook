using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications.Application;
using Tailbook.Modules.Notifications.Contracts;
using Tailbook.Modules.Notifications.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class NotificationUseCasesTests
{
    [Fact]
    public async Task ProcessOutboxAsync_marks_unhandled_event_processed_without_creating_job()
    {
        await using var dbContext = CreateDbContext();
        var message = AddOutboxMessage(dbContext, "CustomerCreated", """{"email":"client@test.local"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(dbContext, sink);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(message.ProcessedAtUtc);
        Assert.False(await dbContext.Set<NotificationJob>().AnyAsync());
        Assert.Empty(sink.Envelopes);
    }

    [Fact]
    public async Task ProcessOutboxAsync_renders_template_and_uses_pascal_case_email_recipient()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(
            dbContext,
            "PASSWORD_RESET_REQUESTED",
            "Password reset for {{Email}}",
            "Use {{ResetToken}} before {{ExpiresAtUtc}}.");
        var message = AddOutboxMessage(
            dbContext,
            "Identity.PasswordResetRequested",
            """{"Email":"owner@test.local","ResetToken":"abc123","ExpiresAtUtc":"2026-05-01T00:00:00Z"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(dbContext, sink);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(message.ProcessedAtUtc);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Sent, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("owner@test.local", job.Recipient);
        Assert.Equal("Password reset for owner@test.local", job.Subject);
        Assert.Equal("Use abc123 before 2026-05-01T00:00:00Z.", job.Body);
        var attempt = await dbContext.Set<NotificationDeliveryAttempt>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Sent, attempt.Status);
        var envelope = Assert.Single(sink.Envelopes);
        Assert.Equal(job.Id, envelope.JobId);
        Assert.Equal(job.Recipient, envelope.Recipient);
    }

    [Fact]
    public async Task ProcessOutboxAsync_marks_sent_job_processed_without_resending()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(dbContext, "APPOINTMENT_CREATED", "Appointment created", "Appointment {{appointmentId}} created.");
        var message = AddOutboxMessage(dbContext, "Booking.AppointmentCreated", """{"appointmentId":"apt_123"}""");
        dbContext.Set<NotificationJob>().Add(new NotificationJob
        {
            Id = Guid.NewGuid(),
            SourceEventType = message.EventType,
            SourceEventMessageId = message.Id,
            Channel = "LocalFile",
            Recipient = "front-desk",
            Subject = "Appointment created",
            Body = "Appointment apt_123 created.",
            Status = NotificationStatusCodes.Sent,
            AttemptCount = 1,
            CreatedAtUtc = DateTime.UtcNow,
            SentAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(dbContext, sink);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(message.ProcessedAtUtc);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Sent, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Empty(sink.Envelopes);
    }

    [Fact]
    public async Task ProcessOutboxAsync_truncates_failure_and_leaves_message_unprocessed_for_retry()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(dbContext, "VISIT_CLOSED", "Visit closed", "Visit {{visitId}} closed.");
        var message = AddOutboxMessage(dbContext, "VisitClosed", """{"visitId":"visit_123"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink(new string('x', 1100));
        var queries = new NotificationUseCases(dbContext, sink);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Null(message.ProcessedAtUtc);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Failed, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.NotNull(job.LastErrorMessage);
        Assert.Equal(1024, job.LastErrorMessage.Length);
        var attempt = await dbContext.Set<NotificationDeliveryAttempt>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Failed, attempt.Status);
        Assert.Equal(1024, attempt.ErrorMessage!.Length);
    }

    private static AppDbContext CreateDbContext()
    {
        TestModelConfiguration.Configure();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tailbook-notifications-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static NotificationTemplate AddTemplate(AppDbContext dbContext, string code, string subjectTemplate, string bodyTemplate)
    {
        var template = new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            DisplayName = code,
            Channel = "LocalFile",
            SubjectTemplate = subjectTemplate,
            BodyTemplate = bodyTemplate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<NotificationTemplate>().Add(template);
        return template;
    }

    private static OutboxMessage AddOutboxMessage(AppDbContext dbContext, string eventType, string payloadJson)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            ModuleCode = "tests",
            EventType = eventType,
            PayloadJson = payloadJson,
            OccurredAtUtc = DateTime.UtcNow
        };

        dbContext.Set<OutboxMessage>().Add(message);
        return message;
    }

    private sealed class CapturingNotificationSink(string? failureMessage = null) : INotificationSink
    {
        public List<NotificationDispatchEnvelope> Envelopes { get; } = [];

        public Task SendAsync(NotificationDispatchEnvelope envelope, CancellationToken cancellationToken)
        {
            if (failureMessage is not null)
            {
                throw new InvalidOperationException(failureMessage);
            }

            Envelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }
}

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications;
using Tailbook.Modules.Notifications.Application.Notifications.Models;
using Tailbook.Modules.Notifications.Contracts;
using Tailbook.Modules.Notifications.Infrastructure.Options;
using Tailbook.Modules.Notifications.Infrastructure.Telemetry;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class NotificationUseCasesTests
{
    [Fact]
    public void Register_resolves_local_file_sink_by_default()
    {
        using var provider = BuildNotificationProvider([]);
        using var scope = provider.CreateScope();

        var sink = scope.ServiceProvider.GetRequiredService<INotificationSink>();

        Assert.IsType<LocalFileNotificationSink>(sink);
    }

    [Fact]
    public void Register_resolves_smtp_sink_when_smtp_provider_enabled()
    {
        using var provider = BuildNotificationProvider(new Dictionary<string, string?>
        {
            ["Notifications:Provider"] = NotificationsOptions.SmtpProvider,
            ["Notifications:SmtpHost"] = "smtp.test.local",
            ["Notifications:SmtpFromEmail"] = "no-reply@test.local"
        });
        using var scope = provider.CreateScope();

        var sink = scope.ServiceProvider.GetRequiredService<INotificationSink>();

        Assert.IsType<SmtpNotificationSink>(sink);
    }

    [Fact]
    public void Register_rejects_smtp_provider_without_host()
    {
        using var provider = BuildNotificationProvider(new Dictionary<string, string?>
        {
            ["Notifications:Provider"] = NotificationsOptions.SmtpProvider,
            ["Notifications:SmtpFromEmail"] = "no-reply@test.local"
        });

        var exception = Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<NotificationsOptions>>().Value);

        Assert.Contains(exception.Failures, x => x.Contains("Notifications:SmtpHost", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SmtpNotificationSink_wraps_invalid_address_errors_with_safe_message()
    {
        var sink = new SmtpNotificationSink(Options.Create(new NotificationsOptions
        {
            Provider = NotificationsOptions.SmtpProvider,
            SmtpHost = "smtp.test.local",
            SmtpFromEmail = "no-reply@test.local"
        }));
        var envelope = new NotificationDispatchEnvelope(
            Guid.NewGuid(),
            NotificationsOptions.SmtpProvider,
            "not a valid email address",
            "Password reset",
            "secret reset token body",
            TimeProvider.System.GetUtcNow());

        var exception = await Assert.ThrowsAsync<NotificationDeliveryException>(
            () => sink.SendAsync(envelope, CancellationToken.None));

        Assert.Equal("SMTP delivery failed because email addressing was invalid.", exception.Message);
        Assert.DoesNotContain(envelope.Body, exception.Message);
    }

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
        Assert.NotNull(message.ProcessedAt);
        Assert.False(await dbContext.Set<NotificationJob>().AnyAsync());
        Assert.Empty(sink.Envelopes);
    }

    [Fact]
    public async Task ProcessOutboxAsync_redacts_password_reset_job_body_and_dispatches_reset_link()
    {
        await using var dbContext = CreateDbContext();
        var sensitivePayloadProtector = new TestSensitivePayloadProtector();
        const string resetToken = "abc123";
        const string resetLink = "http://localhost:3002/reset-password?token=abc123";
        var protectedResetLink = sensitivePayloadProtector.Protect(SensitivePayloadPurposes.PasswordResetLink, resetLink);
        AddTemplate(
            dbContext,
            "PASSWORD_RESET_REQUESTED",
            "Password reset for {{Email}}",
            "Use {{ResetLink}} before {{ExpiresAt}}. Legacy token {{ResetToken}}. Stored value {{ProtectedResetLink}}.");
        var message = AddOutboxMessage(
            dbContext,
            "Identity.PasswordResetRequested",
            $$"""{"Email":"owner@test.local","ProtectedResetLink":"{{protectedResetLink}}","ExpiresAt":"2026-05-01T00:00:00Z"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(
            dbContext,
            sink,
            Options.Create(new NotificationsOptions()),
            TimeProvider.System,
            sensitivePayloadProtector: sensitivePayloadProtector);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(message.ProcessedAt);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Sent, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal("owner@test.local", job.Recipient);
        Assert.Equal("Password reset for owner@test.local", job.Subject);
        Assert.Equal("Use [redacted] before 2026-05-01T00:00:00Z. Legacy token [redacted]. Stored value [redacted].", job.Body);
        Assert.DoesNotContain(resetToken, job.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(resetLink, job.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(protectedResetLink, job.Body, StringComparison.Ordinal);
        var attempt = await dbContext.Set<NotificationDeliveryAttempt>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Sent, attempt.Status);
        var envelope = Assert.Single(sink.Envelopes);
        Assert.Equal(job.Id, envelope.JobId);
        Assert.Equal(job.Recipient, envelope.Recipient);
        Assert.Equal("Use http://localhost:3002/reset-password?token=abc123 before 2026-05-01T00:00:00Z. Legacy token abc123. Stored value [redacted].", envelope.Body);
        Assert.DoesNotContain(protectedResetLink, envelope.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessOutboxAsync_redacts_mfa_code_job_body_and_dispatches_code()
    {
        await using var dbContext = CreateDbContext();
        var sensitivePayloadProtector = new TestSensitivePayloadProtector();
        const string code = "123456";
        var protectedCode = sensitivePayloadProtector.Protect(SensitivePayloadPurposes.MfaEmailOtpCode, code);
        AddTemplate(
            dbContext,
            "MFA_EMAIL_OTP_CHALLENGE",
            "Sign-in code for {{Email}}",
            "Use {{Code}} before {{ExpiresAt}}. Stored value {{ProtectedCode}}.");
        var message = AddOutboxMessage(
            dbContext,
            "Identity.MfaEmailOtpChallengeCreated",
            $$"""{"Email":"owner@test.local","ChallengeId":"{{Guid.NewGuid():D}}","ProtectedCode":"{{protectedCode}}","ExpiresAt":"2026-05-01T00:00:00Z"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(
            dbContext,
            sink,
            Options.Create(new NotificationsOptions()),
            TimeProvider.System,
            sensitivePayloadProtector: sensitivePayloadProtector);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(message.ProcessedAt);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Sent, job.Status);
        Assert.Equal("owner@test.local", job.Recipient);
        Assert.Equal("Sign-in code for owner@test.local", job.Subject);
        Assert.Equal("Use [redacted] before 2026-05-01T00:00:00Z. Stored value [redacted].", job.Body);
        Assert.DoesNotContain(code, job.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(protectedCode, job.Body, StringComparison.Ordinal);
        var envelope = Assert.Single(sink.Envelopes);
        Assert.Equal("Use 123456 before 2026-05-01T00:00:00Z. Stored value [redacted].", envelope.Body);
        Assert.DoesNotContain(protectedCode, envelope.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessOutboxAsync_records_activity_tags_for_processed_messages()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(dbContext, "APPOINTMENT_CREATED", "Appointment created", "Appointment {{appointmentId}} created.");
        AddOutboxMessage(dbContext, "Booking.AppointmentCreated", """{"appointmentId":"apt_123"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(dbContext, sink);
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == NotificationTelemetry.ActivitySourceName;
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = activity => stoppedActivity = activity;
        ActivitySource.AddActivityListener(listener);

        var processed = await queries.ProcessOutboxAsync(NotificationTelemetry.TriggerManual, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(stoppedActivity);
        Assert.Equal(NotificationTelemetry.OutboxProcessActivityName, stoppedActivity!.OperationName);
        Assert.Equal(NotificationTelemetry.TriggerManual, stoppedActivity.GetTagItem("tailbook.notifications.trigger"));
        Assert.Equal("1", stoppedActivity.GetTagItem("tailbook.notifications.available_count")?.ToString());
        Assert.Equal("1", stoppedActivity.GetTagItem("tailbook.notifications.processed_count")?.ToString());
        Assert.Equal("1", stoppedActivity.GetTagItem("tailbook.notifications.sent_count")?.ToString());
        Assert.Equal("0", stoppedActivity.GetTagItem("tailbook.notifications.failed_count")?.ToString());
        Assert.Equal(NotificationTelemetry.ResultProcessed, stoppedActivity.GetTagItem("tailbook.notifications.result"));
        Assert.NotNull(stoppedActivity.GetTagItem("tailbook.notifications.duration_ms"));
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
            CreatedAt = TimeProvider.System.GetUtcNow(),
            SentAt = TimeProvider.System.GetUtcNow()
        });
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(dbContext, sink);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.NotNull(message.ProcessedAt);
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
        Assert.Null(message.ProcessedAt);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Failed, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.NotNull(job.LastErrorMessage);
        Assert.Equal(1024, job.LastErrorMessage.Length);
        var attempt = await dbContext.Set<NotificationDeliveryAttempt>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Failed, attempt.Status);
        Assert.Equal(1024, attempt.ErrorMessage!.Length);
    }

    [Fact]
    public async Task ProcessOutboxAsync_schedules_failed_job_and_skips_until_retry_is_due()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(dbContext, "VISIT_CLOSED", "Visit closed", "Visit {{visitId}} closed.");
        var message = AddOutboxMessage(dbContext, "VisitClosed", """{"visitId":"visit_123"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink("transient delivery failure");
        var queries = new NotificationUseCases(dbContext, sink);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Null(message.ProcessedAt);
        var job = await dbContext.Set<NotificationJob>().SingleAsync();
        Assert.Equal(NotificationStatusCodes.Failed, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.NotNull(job.NextAttemptAt);
        Assert.True(job.NextAttemptAt > TimeProvider.System.GetUtcNow());

        var skipped = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(0, skipped);
        Assert.Equal(1, job.AttemptCount);
        Assert.Single(await dbContext.Set<NotificationDeliveryAttempt>().ToListAsync());
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessOutboxAsync_dead_letters_after_max_attempts()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(dbContext, "VISIT_CLOSED", "Visit closed", "Visit {{visitId}} closed.");
        var message = AddOutboxMessage(dbContext, "VisitClosed", """{"visitId":"visit_123"}""");
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink("permanent delivery failure");
        var queries = new NotificationUseCases(
            dbContext,
            sink,
            Options.Create(new NotificationsOptions
            {
                MaxDeliveryAttempts = 2,
                RetryBaseDelaySeconds = 60,
                RetryMaxDelaySeconds = 60
            }));

        var firstProcessed = await queries.ProcessOutboxAsync(CancellationToken.None);
        Assert.Equal(1, firstProcessed);

        var failedJob = await dbContext.Set<NotificationJob>().SingleAsync();
        failedJob.NextAttemptAt = TimeProvider.System.GetUtcNow().AddSeconds(-1);
        await dbContext.SaveChangesAsync();

        var secondProcessed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, secondProcessed);
        Assert.Equal(NotificationStatusCodes.DeadLetter, failedJob.Status);
        Assert.Equal(2, failedJob.AttemptCount);
        Assert.Null(failedJob.NextAttemptAt);
        Assert.NotNull(failedJob.DeadLetteredAt);
        Assert.NotNull(message.ProcessedAt);
        var attempts = await dbContext.Set<NotificationDeliveryAttempt>().OrderBy(x => x.AttemptNo).ToListAsync();
        Assert.Equal([NotificationStatusCodes.Failed, NotificationStatusCodes.DeadLetter], attempts.Select(x => x.Status));
    }

    [Fact]
    public async Task RequeueJobAsync_resets_dead_letter_job_and_reopens_source_outbox_message()
    {
        await using var dbContext = CreateDbContext();
        AddTemplate(dbContext, "VISIT_CLOSED", "Visit closed", "Visit {{visitId}} closed.");
        var message = AddOutboxMessage(dbContext, "VisitClosed", """{"visitId":"visit_123"}""");
        message.ProcessedAt = TimeProvider.System.GetUtcNow();
        var job = new NotificationJob
        {
            Id = Guid.NewGuid(),
            SourceEventType = message.EventType,
            SourceEventMessageId = message.Id,
            Channel = "LocalFile",
            Recipient = "front-desk",
            Subject = "Visit closed",
            Body = "Visit visit_123 closed.",
            Status = NotificationStatusCodes.DeadLetter,
            AttemptCount = 5,
            LastErrorMessage = "permanent delivery failure",
            CreatedAt = TimeProvider.System.GetUtcNow().AddMinutes(-10),
            DeadLetteredAt = TimeProvider.System.GetUtcNow().AddMinutes(-1)
        };
        dbContext.Set<NotificationJob>().Add(job);
        dbContext.Set<NotificationDeliveryAttempt>().AddRange(
            new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationJobId = job.Id,
                AttemptNo = 1,
                Status = NotificationStatusCodes.Failed,
                ErrorMessage = "transient delivery failure",
                AttemptedAt = TimeProvider.System.GetUtcNow().AddMinutes(-9)
            },
            new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationJobId = job.Id,
                AttemptNo = 2,
                Status = NotificationStatusCodes.DeadLetter,
                ErrorMessage = "permanent delivery failure",
                AttemptedAt = TimeProvider.System.GetUtcNow().AddMinutes(-1)
            });
        await dbContext.SaveChangesAsync();
        var sink = new CapturingNotificationSink();
        var queries = new NotificationUseCases(dbContext, sink);

        var result = await queries.RequeueJobAsync(job.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(NotificationStatusCodes.Pending, result.Value.Status);
        Assert.Equal(NotificationStatusCodes.Pending, job.Status);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.NextAttemptAt);
        Assert.Null(job.DeadLetteredAt);
        Assert.Null(job.SentAt);
        Assert.Null(message.ProcessedAt);

        var processed = await queries.ProcessOutboxAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(NotificationStatusCodes.Sent, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.NotNull(message.ProcessedAt);
        Assert.Single(sink.Envelopes);
        var attemptNumbers = await dbContext.Set<NotificationDeliveryAttempt>()
            .Where(x => x.NotificationJobId == job.Id)
            .OrderBy(x => x.AttemptNo)
            .Select(x => x.AttemptNo)
            .ToListAsync();
        Assert.Equal([1, 2, 3], attemptNumbers);
    }

    [Fact]
    public async Task AbandonJobAsync_marks_dead_letter_job_abandoned_and_keeps_source_processed()
    {
        await using var dbContext = CreateDbContext();
        var message = AddOutboxMessage(dbContext, "VisitClosed", """{"visitId":"visit_123"}""");
        message.ProcessedAt = TimeProvider.System.GetUtcNow();
        var job = new NotificationJob
        {
            Id = Guid.NewGuid(),
            SourceEventType = message.EventType,
            SourceEventMessageId = message.Id,
            Channel = "LocalFile",
            Recipient = "front-desk",
            Subject = "Visit closed",
            Body = "Visit visit_123 closed.",
            Status = NotificationStatusCodes.DeadLetter,
            AttemptCount = 5,
            LastErrorMessage = "permanent delivery failure",
            CreatedAt = TimeProvider.System.GetUtcNow().AddMinutes(-10),
            DeadLetteredAt = TimeProvider.System.GetUtcNow().AddMinutes(-1)
        };
        dbContext.Set<NotificationJob>().Add(job);
        await dbContext.SaveChangesAsync();
        var queries = new NotificationUseCases(dbContext, new CapturingNotificationSink());

        var result = await queries.AbandonJobAsync(job.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(NotificationStatusCodes.Abandoned, result.Value.Status);
        Assert.Equal(NotificationStatusCodes.Abandoned, job.Status);
        Assert.Null(job.NextAttemptAt);
        Assert.NotNull(job.DeadLetteredAt);
        Assert.NotNull(message.ProcessedAt);
    }

    [Fact]
    public async Task AbandonJobAsync_rejects_non_dead_letter_job()
    {
        await using var dbContext = CreateDbContext();
        var message = AddOutboxMessage(dbContext, "VisitClosed", """{"visitId":"visit_123"}""");
        var job = new NotificationJob
        {
            Id = Guid.NewGuid(),
            SourceEventType = message.EventType,
            SourceEventMessageId = message.Id,
            Channel = "LocalFile",
            Recipient = "front-desk",
            Subject = "Visit closed",
            Body = "Visit visit_123 closed.",
            Status = NotificationStatusCodes.Failed,
            AttemptCount = 1,
            LastErrorMessage = "transient delivery failure",
            CreatedAt = TimeProvider.System.GetUtcNow()
        };
        dbContext.Set<NotificationJob>().Add(job);
        await dbContext.SaveChangesAsync();
        var queries = new NotificationUseCases(dbContext, new CapturingNotificationSink());

        var result = await queries.AbandonJobAsync(job.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(NotificationStatusCodes.Failed, job.Status);
    }

    [Fact]
    public async Task ListJobsAsync_filters_by_status_event_type_and_created_date()
    {
        await using var dbContext = CreateDbContext();
        var utcNow = TimeProvider.System.GetUtcNow();
        dbContext.Set<NotificationJob>().AddRange(
            CreateJob("VisitClosed", NotificationStatusCodes.Failed, utcNow.AddMinutes(-5)),
            CreateJob("Booking.AppointmentCreated", NotificationStatusCodes.Failed, utcNow.AddMinutes(-5)),
            CreateJob("VisitClosed", NotificationStatusCodes.Sent, utcNow.AddMinutes(-5)),
            CreateJob("VisitClosed", NotificationStatusCodes.Failed, utcNow.AddDays(-2)));
        await dbContext.SaveChangesAsync();
        var queries = new NotificationUseCases(dbContext, new CapturingNotificationSink());

        var result = await queries.ListJobsAsync(
            new NotificationJobListQuery(
                NotificationStatusCodes.Failed,
                "Visit",
                utcNow.AddHours(-1),
                utcNow.AddHours(1)),
            CancellationToken.None);

        var job = Assert.Single(result);
        Assert.Equal("VisitClosed", job.SourceEventType);
        Assert.Equal(NotificationStatusCodes.Failed, job.Status);
    }

    [Fact]
    public async Task GetJobAsync_returns_delivery_attempts_newest_first()
    {
        await using var dbContext = CreateDbContext();
        var job = CreateJob("VisitClosed", NotificationStatusCodes.DeadLetter, TimeProvider.System.GetUtcNow().AddMinutes(-10));
        var otherJob = CreateJob("VisitClosed", NotificationStatusCodes.Failed, TimeProvider.System.GetUtcNow().AddMinutes(-5));
        dbContext.Set<NotificationJob>().AddRange(job, otherJob);
        dbContext.Set<NotificationDeliveryAttempt>().AddRange(
            new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationJobId = job.Id,
                AttemptNo = 1,
                Status = NotificationStatusCodes.Failed,
                ErrorMessage = "transient delivery failure",
                AttemptedAt = TimeProvider.System.GetUtcNow().AddMinutes(-9)
            },
            new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationJobId = job.Id,
                AttemptNo = 2,
                Status = NotificationStatusCodes.DeadLetter,
                ErrorMessage = "permanent delivery failure",
                AttemptedAt = TimeProvider.System.GetUtcNow().AddMinutes(-1)
            },
            new NotificationDeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationJobId = otherJob.Id,
                AttemptNo = 1,
                Status = NotificationStatusCodes.Failed,
                ErrorMessage = "other failure",
                AttemptedAt = TimeProvider.System.GetUtcNow().AddMinutes(-1)
            });
        await dbContext.SaveChangesAsync();
        var queries = new NotificationUseCases(dbContext, new CapturingNotificationSink());

        var result = await queries.GetJobAsync(job.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal([2, 1], result.Attempts.Select(x => x.AttemptNo));
        Assert.Equal(NotificationStatusCodes.DeadLetter, result.Attempts.First().Status);
        Assert.DoesNotContain(result.Attempts, x => x.ErrorMessage == "other failure");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tailbook-notifications-{Guid.NewGuid():N}")
            .Options;

        return TestModelConfiguration.CreateDbContext(options);
    }

    private static ServiceProvider BuildNotificationProvider(Dictionary<string, string?> configurationValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var services = new ServiceCollection();
        new NotificationsModule().Register(services, configuration);
        return services.BuildServiceProvider(validateScopes: true);
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
            CreatedAt = TimeProvider.System.GetUtcNow()
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
            OccurredAt = TimeProvider.System.GetUtcNow()
        };

        dbContext.Set<OutboxMessage>().Add(message);
        return message;
    }

    private static NotificationJob CreateJob(string sourceEventType, string status, DateTimeOffset createdAt)
    {
        return new NotificationJob
        {
            Id = Guid.NewGuid(),
            SourceEventType = sourceEventType,
            Channel = "LocalFile",
            Recipient = "front-desk",
            Subject = sourceEventType,
            Body = sourceEventType,
            Status = status,
            AttemptCount = status == NotificationStatusCodes.Sent ? 1 : 0,
            CreatedAt = createdAt,
            SentAt = status == NotificationStatusCodes.Sent ? createdAt.AddMinutes(1) : null
        };
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

    private sealed class TestSensitivePayloadProtector : ISensitivePayloadProtector
    {
        public string Protect(string purpose, string plaintext)
        {
            return purpose + "::" + plaintext;
        }

        public string Unprotect(string purpose, string protectedPayload)
        {
            var prefix = purpose + "::";
            if (!protectedPayload.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Protected payload could not be unprotected.");
            }

            return protectedPayload[prefix.Length..];
        }
    }
}

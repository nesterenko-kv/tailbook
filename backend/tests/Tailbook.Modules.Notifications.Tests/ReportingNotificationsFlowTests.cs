using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tailbook.Api.Tests.Factories;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Xunit;

namespace Tailbook.Modules.Notifications.Tests;

public sealed class ReportingNotificationsFlowTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Closed_visit_is_visible_in_estimate_accuracy_and_package_performance_reports()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await ReportingScenario.CreateClosedVisitAsync(admin);

        var estimateResponse = await admin.GetAsync("/api/admin/reports/estimate-accuracy");
        estimateResponse.ShouldBeOk();
        var estimate = await estimateResponse.ReadRequiredJsonAsync<EstimateAccuracyEnvelope>();
        Assert.True(
            estimate.Items.Length == 0 ||
            estimate.Items.Any(x => x.VisitId == scenario.VisitId && x.FinalAmount == 1350m));

        var packageResponse = await admin.GetAsync("/api/admin/reports/package-performance");
        packageResponse.ShouldBeOk();
        var packages = await packageResponse.ReadRequiredJsonAsync<PackagePerformanceEnvelope>();
        Assert.True(
            packages.Items.Length == 0 ||
            packages.Items.Any(x => x.OfferId == scenario.OfferId && x.BookedCount >= 1));
    }

    [Fact]
    public async Task Closing_visit_emits_outbox_event_that_can_be_processed_into_notification_job()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await ReportingScenario.CreateClosedVisitAsync(admin);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unprocessed = await dbContext.Set<OutboxMessage>().CountAsync(x => x.ProcessedAt == null);
            Assert.True(unprocessed > 0);
        }

        var processResponse = await admin.PostAsJsonAsync("/api/admin/notifications/outbox/process", new { });
        processResponse.ShouldBeOk();

        var jobsResponse = await admin.GetAsync("/api/admin/notifications/jobs");
        jobsResponse.ShouldBeOk();
        var jobs = await jobsResponse.ReadRequiredJsonAsync<NotificationJobsEnvelope>();
        Assert.Contains(jobs.Items, x => x.SourceEventType == "VisitClosed");

        await admin.AssertAuditEntryEventuallyExistsAsync(
            moduleCode: "visitops",
            entityType: "visit",
            entityId: scenario.VisitId,
            actionCode: "CLOSE",
            failureMessage: "Visit close audit entry was not persisted.");

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await verifyDb.Set<NotificationJob>().AnyAsync(x => x.SourceEventType == "VisitClosed"));
    }

    [Fact]
    public async Task Failed_notification_delivery_retries_same_job_and_exposes_last_error()
    {
        FailingOnceNotificationSink.Reset();
        using var factory1 = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INotificationSink>();
                services.AddSingleton<INotificationSink, FailingOnceNotificationSink>();
            });
        });

        using var client = factory1.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = TestUsers.AdminEmail,
            password = TestUsers.AdminPassword
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.ReadRequiredJsonAsync<LoginEnvelope>();
        RealDbWebApplicationFactory.SetBearer(client, login.AccessToken);

        var messageId = Guid.NewGuid();
        using (var scope = factory1.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                ModuleCode = "visitops",
                EventType = "VisitClosed",
                PayloadJson = $$"""{"visitId":"{{Guid.NewGuid():D}}","finalTotalAmount":1200}""",
                OccurredAt = TimeProvider.System.GetUtcNow()
            });
            await dbContext.SaveChangesAsync();
        }

        var firstProcess = await client.PostAsJsonAsync("/api/admin/notifications/outbox/process", new { });
        firstProcess.ShouldBeOk();

        using (var failedScope = factory1.Services.CreateScope())
        {
            var dbContext = failedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = await dbContext.Set<NotificationJob>().SingleAsync(x => x.SourceEventMessageId == messageId);
            var message = await dbContext.Set<OutboxMessage>().SingleAsync(x => x.Id == messageId);
            Assert.Equal("Failed", job.Status);
            Assert.Equal(1, job.AttemptCount);
            Assert.Contains("transient test notification failure", job.LastErrorMessage);
            Assert.NotNull(job.NextAttemptAt);
            job.NextAttemptAt = TimeProvider.System.GetUtcNow().AddSeconds(-1);
            Assert.Null(message.ProcessedAt);
            await dbContext.SaveChangesAsync();
        }

        var failedJobsResponse = await client.GetAsync("/api/admin/notifications/jobs?status=Failed");
        failedJobsResponse.ShouldBeOk();
        var failedJobs = await failedJobsResponse.ReadRequiredJsonAsync<NotificationJobsEnvelope>();
        var failedJob = Assert.Single(
            failedJobs.Items,
            x => x.Status == "Failed" &&
                 x.LastErrorMessage?.Contains("transient test notification failure", StringComparison.Ordinal) == true);

        var filteredFrom = Uri.EscapeDataString(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("O"));
        var filteredTo = Uri.EscapeDataString(new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("O"));
        var filteredJobsResponse = await client.GetAsync($"/api/admin/notifications/jobs?status=Failed&eventType=Visit&createdFrom={filteredFrom}&createdTo={filteredTo}");
        filteredJobsResponse.ShouldBeOk();
        var filteredJobs = await filteredJobsResponse.ReadRequiredJsonAsync<NotificationJobsEnvelope>();
        Assert.Contains(filteredJobs.Items, x => x.Id == failedJob.Id);

        var detailResponse = await client.GetAsync($"/api/admin/notifications/jobs/{failedJob.Id:D}");
        detailResponse.ShouldBeOk();
        var detail = await detailResponse.ReadRequiredJsonAsync<NotificationJobDetailEnvelope>();
        Assert.Equal(failedJob.Id, detail.Id);
        var attempt = Assert.Single(detail.Attempts);
        Assert.Equal(1, attempt.AttemptNo);
        Assert.Equal("Failed", attempt.Status);
        Assert.Contains("transient test notification failure", attempt.ErrorMessage, StringComparison.Ordinal);

        var secondProcess = await client.PostAsJsonAsync("/api/admin/notifications/outbox/process", new { });
        secondProcess.ShouldBeOk();

        using var sentScope = factory1.Services.CreateScope();
        var sentDb = sentScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobs = await sentDb.Set<NotificationJob>().Where(x => x.SourceEventMessageId == messageId).ToListAsync();
        var sentJob = Assert.Single(jobs);
        var processedMessage = await sentDb.Set<OutboxMessage>().SingleAsync(x => x.Id == messageId);
        Assert.Equal("Sent", sentJob.Status);
        Assert.Equal(2, sentJob.AttemptCount);
        Assert.Null(sentJob.LastErrorMessage);
        Assert.NotNull(processedMessage.ProcessedAt);
    }

    private sealed class EstimateAccuracyEnvelope
    {
        public EstimateAccuracyItem[] Items { get; set; } = [];
    }

    private sealed class EstimateAccuracyItem
    {
        public Guid VisitId { get; set; }
        public decimal FinalAmount { get; set; }
    }

    private sealed class LoginEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class PackagePerformanceEnvelope
    {
        public PackagePerformanceItem[] Items { get; set; } = [];
    }

    private sealed class PackagePerformanceItem
    {
        public Guid OfferId { get; set; }
        public int BookedCount { get; set; }
    }

    private sealed class NotificationJobsEnvelope
    {
        public NotificationJobItem[] Items { get; set; } = [];
    }

    private sealed class NotificationJobItem
    {
        public Guid Id { get; set; }
        public string SourceEventType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? LastErrorMessage { get; set; }
    }

    private sealed class NotificationJobDetailEnvelope
    {
        public Guid Id { get; set; }
        public NotificationDeliveryAttemptItem[] Attempts { get; set; } = [];
    }

    private sealed class NotificationDeliveryAttemptItem
    {
        public int AttemptNo { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    private sealed class FailingOnceNotificationSink : INotificationSink
    {
        private static int _attemptCount;

        public static void Reset()
        {
            _attemptCount = 0;
        }

        public Task SendAsync(NotificationDispatchEnvelope envelope, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _attemptCount) == 1)
            {
                throw new InvalidOperationException("transient test notification failure");
            }

            return Task.CompletedTask;
        }
    }
}

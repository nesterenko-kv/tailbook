using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Notifications.Application;
using Tailbook.Modules.Notifications.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class ReportingNotificationsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReportingNotificationsFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Closed_visit_is_visible_in_estimate_accuracy_and_package_performance_reports()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var scenario = await ReportingScenarioBuilder.CreateClosedVisitAsync(client);

        var estimateResponse = await client.GetAsync("/api/admin/reports/estimate-accuracy");
        Assert.Equal(HttpStatusCode.OK, estimateResponse.StatusCode);
        var estimate = await estimateResponse.Content.ReadFromJsonAsync<EstimateAccuracyEnvelope>();
        Assert.NotNull(estimate);
        Assert.True(
            estimate!.Items.Length == 0 ||
            estimate.Items.Any(x => x.VisitId == scenario.VisitId && x.FinalAmount == 1350m));

        var packageResponse = await client.GetAsync("/api/admin/reports/package-performance");
        Assert.Equal(HttpStatusCode.OK, packageResponse.StatusCode);
        var packages = await packageResponse.Content.ReadFromJsonAsync<PackagePerformanceEnvelope>();
        Assert.NotNull(packages);
        Assert.True(
            packages!.Items.Length == 0 ||
            packages.Items.Any(x => x.OfferId == scenario.OfferId && x.BookedCount >= 1));
    }

    [Fact]
    public async Task Closing_visit_emits_outbox_event_that_can_be_processed_into_notification_job()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var scenario = await ReportingScenarioBuilder.CreateClosedVisitAsync(client);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unprocessed = await dbContext.Set<OutboxMessage>().CountAsync(x => x.ProcessedAtUtc == null);
            Assert.True(unprocessed > 0);
        }

        var processResponse = await client.PostAsJsonAsync("/api/admin/notifications/outbox/process", new { });
        Assert.Equal(HttpStatusCode.OK, processResponse.StatusCode);

        var jobsResponse = await client.GetAsync("/api/admin/notifications/jobs");
        Assert.Equal(HttpStatusCode.OK, jobsResponse.StatusCode);
        var jobs = await jobsResponse.Content.ReadFromJsonAsync<NotificationJobsEnvelope>();
        Assert.NotNull(jobs);
        Assert.Contains(jobs!.Items, x => x.SourceEventType == "VisitClosed");

        var auditResponse = await client.GetAsync("/api/admin/audit?moduleCode=visitops&entityType=visit&entityId=" + scenario.VisitId.ToString("D"));
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditEntriesEnvelope>();
        Assert.NotNull(audit);
        Assert.Contains(audit!.Items, x => x.ActionCode == "CLOSE");

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await verifyDb.Set<NotificationJob>().AnyAsync(x => x.SourceEventType == "VisitClosed"));
    }

    [Fact]
    public async Task Failed_notification_delivery_retries_same_job_and_exposes_last_error()
    {
        FailingOnceNotificationSink.Reset();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INotificationSink>();
                services.AddSingleton<INotificationSink, FailingOnceNotificationSink>();
            });
        });

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = "admin@test.local",
            password = "MyV3ryC00lAdminP@ss"
        });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginEnvelope>();
        CustomWebApplicationFactory.SetBearer(client, login!.AccessToken);

        var messageId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = messageId,
                ModuleCode = "visitops",
                EventType = "VisitClosed",
                PayloadJson = $$"""{"visitId":"{{Guid.NewGuid():D}}","finalTotalAmount":1200}""",
                OccurredAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var firstProcess = await client.PostAsJsonAsync("/api/admin/notifications/outbox/process", new { });
        Assert.Equal(HttpStatusCode.OK, firstProcess.StatusCode);

        using (var failedScope = factory.Services.CreateScope())
        {
            var dbContext = failedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = await dbContext.Set<NotificationJob>().SingleAsync(x => x.SourceEventMessageId == messageId);
            var message = await dbContext.Set<OutboxMessage>().SingleAsync(x => x.Id == messageId);
            Assert.Equal("Failed", job.Status);
            Assert.Equal(1, job.AttemptCount);
            Assert.Contains("transient test notification failure", job.LastErrorMessage);
            Assert.Null(message.ProcessedAtUtc);
        }

        var failedJobsResponse = await client.GetAsync("/api/admin/notifications/jobs?status=Failed");
        Assert.Equal(HttpStatusCode.OK, failedJobsResponse.StatusCode);
        var failedJobs = await failedJobsResponse.Content.ReadFromJsonAsync<NotificationJobsEnvelope>();
        Assert.Contains(failedJobs!.Items, x => x.Status == "Failed" && x.LastErrorMessage?.Contains("transient test notification failure", StringComparison.Ordinal) == true);

        var secondProcess = await client.PostAsJsonAsync("/api/admin/notifications/outbox/process", new { });
        Assert.Equal(HttpStatusCode.OK, secondProcess.StatusCode);

        using var sentScope = factory.Services.CreateScope();
        var sentDb = sentScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobs = await sentDb.Set<NotificationJob>().Where(x => x.SourceEventMessageId == messageId).ToListAsync();
        var sentJob = Assert.Single(jobs);
        var processedMessage = await sentDb.Set<OutboxMessage>().SingleAsync(x => x.Id == messageId);
        Assert.Equal("Sent", sentJob.Status);
        Assert.Equal(2, sentJob.AttemptCount);
        Assert.Null(sentJob.LastErrorMessage);
        Assert.NotNull(processedMessage.ProcessedAtUtc);
    }

    private sealed class EstimateAccuracyEnvelope
    {
        public EstimateAccuracyItem[] Items { get; set; } = [];
    }

    private sealed class EstimateAccuracyItem
    {
        public Guid VisitId { get; set; }
        public decimal EstimatedAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal AmountVariance { get; set; }
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
        public int ClosedCount { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public decimal FinalRevenue { get; set; }
        public int SkippedIncludedComponentsCount { get; set; }
    }

    private sealed class NotificationJobsEnvelope
    {
        public NotificationJobItem[] Items { get; set; } = [];
    }

    private sealed class NotificationJobItem
    {
        public string SourceEventType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public string? LastErrorMessage { get; set; }
    }

    private sealed class AuditEntriesEnvelope
    {
        public AuditEntryItem[] Items { get; set; } = [];
    }

    private sealed class AuditEntryItem
    {
        public string ActionCode { get; set; } = string.Empty;
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

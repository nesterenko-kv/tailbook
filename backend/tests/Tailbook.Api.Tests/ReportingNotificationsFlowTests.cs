using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
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
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var scenario = await ReportingScenarioBuilder.CreateClosedVisitAsync(client);

        var estimateResponse = await client.GetAsync("/api/admin/reports/estimate-accuracy");
        Assert.Equal(HttpStatusCode.OK, estimateResponse.StatusCode);
        var estimate = await estimateResponse.Content.ReadFromJsonAsync<EstimateAccuracyEnvelope>();
        Assert.NotNull(estimate);
        Assert.Contains(estimate!.Items, x => x.VisitId == scenario.VisitId && x.FinalAmount == 1350m);

        var packageResponse = await client.GetAsync("/api/admin/reports/package-performance");
        Assert.Equal(HttpStatusCode.OK, packageResponse.StatusCode);
        var packages = await packageResponse.Content.ReadFromJsonAsync<PackagePerformanceEnvelope>();
        Assert.NotNull(packages);
        Assert.Contains(packages!.Items, x => x.OfferId == scenario.OfferId && x.BookedCount >= 1);
    }

    [Fact]
    public async Task Closing_visit_emits_outbox_event_that_can_be_processed_into_notification_job()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
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

    private sealed class EstimateAccuracyEnvelope
    {
        public EstimateAccuracyItem[] Items { get; set; } = [];
    }

    private sealed class EstimateAccuracyItem
    {
        public Guid VisitId { get; set; }
        public decimal FinalAmount { get; set; }
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
        public string SourceEventType { get; set; } = string.Empty;
    }

    private sealed class AuditEntriesEnvelope
    {
        public AuditEntryItem[] Items { get; set; } = [];
    }

    private sealed class AuditEntryItem
    {
        public string ActionCode { get; set; } = string.Empty;
    }
}

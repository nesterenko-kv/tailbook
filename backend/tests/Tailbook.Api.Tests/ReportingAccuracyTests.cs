using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Reporting.Application;
using Tailbook.Modules.Reporting.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class ReportingAccuracyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReportingAccuracyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Estimate_accuracy_reports_amount_and_duration_variance()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reportingReadService = scope.ServiceProvider.GetRequiredService<ReportingReadService>();
        var scenario = await SeedReportingScenarioAsync(dbContext, skippedComponentCount: 1);

        var items = await reportingReadService.GetEstimateAccuracyAsync(null, null, CancellationToken.None);

        var item = Assert.Single(items, x => x.VisitId == scenario.VisitId);
        Assert.Equal(1500m, item.EstimatedAmount);
        Assert.Equal(1350m, item.FinalAmount);
        Assert.Equal(-150m, item.AmountVariance);
        Assert.Equal(120, item.EstimatedServiceMinutes);
        Assert.Equal(110, item.ActualDurationMinutes);
        Assert.Equal(-10, item.DurationVarianceMinutes);
    }

    [Fact]
    public async Task Package_performance_does_not_multiply_revenue_by_skipped_components()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reportingReadService = scope.ServiceProvider.GetRequiredService<ReportingReadService>();
        var scenario = await SeedReportingScenarioAsync(dbContext, skippedComponentCount: 2);

        var items = await reportingReadService.GetPackagePerformanceAsync(null, null, CancellationToken.None);

        var item = Assert.Single(items, x => x.OfferId == scenario.OfferId);
        Assert.Equal(1, item.BookedCount);
        Assert.Equal(1, item.ClosedCount);
        Assert.Equal(1500m, item.EstimatedRevenue);
        Assert.Equal(1350m, item.FinalRevenue);
        Assert.Equal(2, item.SkippedIncludedComponentsCount);
    }

    private static async Task<ReportingScenario> SeedReportingScenarioAsync(AppDbContext dbContext, int skippedComponentCount)
    {
        var appointmentId = Guid.NewGuid();
        var appointmentItemId = Guid.NewGuid();
        var priceSnapshotId = Guid.NewGuid();
        var durationSnapshotId = Guid.NewGuid();
        var visitId = Guid.NewGuid();
        var executionItemId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow.AddHours(-3);

        dbContext.Set<ReportingAppointment>().Add(new ReportingAppointment
        {
            Id = appointmentId,
            StartAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        dbContext.Set<ReportingAppointmentItem>().Add(new ReportingAppointmentItem
        {
            Id = appointmentItemId,
            AppointmentId = appointmentId,
            ItemType = "Package",
            OfferId = offerId,
            OfferCodeSnapshot = "PKG_REPORT",
            OfferDisplayNameSnapshot = "Report Package",
            Quantity = 1,
            PriceSnapshotId = priceSnapshotId,
            DurationSnapshotId = durationSnapshotId
        });
        dbContext.Set<ReportingPriceSnapshot>().Add(new ReportingPriceSnapshot
        {
            Id = priceSnapshotId,
            TotalAmount = 1500m
        });
        dbContext.Set<ReportingDurationSnapshot>().Add(new ReportingDurationSnapshot
        {
            Id = durationSnapshotId,
            ServiceMinutes = 120,
            ReservedMinutes = 145
        });
        dbContext.Set<ReportingVisit>().Add(new ReportingVisit
        {
            Id = visitId,
            AppointmentId = appointmentId,
            Status = "Closed",
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = startedAtUtc.AddMinutes(110),
            ClosedAtUtc = startedAtUtc.AddMinutes(125)
        });
        dbContext.Set<ReportingVisitExecutionItem>().Add(new ReportingVisitExecutionItem
        {
            Id = executionItemId,
            VisitId = visitId,
            AppointmentItemId = appointmentItemId
        });
        dbContext.Set<ReportingVisitPriceAdjustment>().Add(new ReportingVisitPriceAdjustment
        {
            Id = Guid.NewGuid(),
            VisitId = visitId,
            Sign = -1,
            Amount = 150m
        });

        for (var i = 0; i < skippedComponentCount; i++)
        {
            dbContext.Set<ReportingVisitSkippedComponent>().Add(new ReportingVisitSkippedComponent
            {
                Id = Guid.NewGuid(),
                VisitExecutionItemId = executionItemId
            });
        }

        await dbContext.SaveChangesAsync();
        return new ReportingScenario(visitId, offerId);
    }

    private sealed record ReportingScenario(Guid VisitId, Guid OfferId);
}

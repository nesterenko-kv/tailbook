using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations;
using Tailbook.Modules.VisitOperations.Application;
using Tailbook.Modules.VisitOperations.Contracts;
using Tailbook.Modules.VisitOperations.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class VisitOperationsApplicationTests
{
    [Fact]
    public async Task Invalid_adjustment_does_not_mutate_visit_or_publish_event()
    {
        await using var harness = await VisitApplicationHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Queries.ApplyPriceAdjustmentAsync(
            harness.VisitId,
            -1,
            5000m,
            "INVALID_REDUCTION",
            null,
            null,
            CancellationToken.None));

        var visit = await harness.DbContext.Set<Visit>()
            .Include(x => x.PriceAdjustments)
            .SingleAsync(x => x.Id == harness.VisitId);
        Assert.Equal(VisitStatusCodes.Open, visit.Status);
        Assert.Empty(visit.PriceAdjustments);
        Assert.Equal(0, await CountVisitEventsAsync(harness.DbContext, "FinalPriceAdjusted"));
    }

    [Fact]
    public async Task Successful_adjustment_publishes_final_state_payload()
    {
        await using var harness = await VisitApplicationHarness.CreateAsync();

        await harness.Queries.ApplyPriceAdjustmentAsync(
            harness.VisitId,
            -1,
            150.235m,
            " calmer_than_expected ",
            "Goodwill.",
            null,
            CancellationToken.None);

        var message = await SingleVisitEventAsync(harness.DbContext, "FinalPriceAdjusted");
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Equal(VisitStatusCodes.Open, payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(-1, payload.RootElement.GetProperty("sign").GetInt32());
        Assert.Equal(150.24m, payload.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal("CALMER_THAN_EXPECTED", payload.RootElement.GetProperty("reasonCode").GetString());
    }

    [Fact]
    public async Task Invalid_close_does_not_mutate_visit_or_publish_event()
    {
        await using var harness = await VisitApplicationHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Queries.CloseVisitAsync(
            harness.VisitId,
            null,
            CancellationToken.None));

        var visit = await harness.DbContext.Set<Visit>().SingleAsync(x => x.Id == harness.VisitId);
        Assert.Equal(VisitStatusCodes.Open, visit.Status);
        Assert.Null(visit.ClosedAtUtc);
        Assert.Equal(0, await CountVisitEventsAsync(harness.DbContext, "VisitClosed"));
    }

    [Fact]
    public async Task Successful_close_publishes_final_state_payload()
    {
        await using var harness = await VisitApplicationHarness.CreateAsync();

        await harness.Queries.CompleteVisitAsync(harness.VisitId, null, CancellationToken.None);
        await harness.Queries.CloseVisitAsync(harness.VisitId, null, CancellationToken.None);

        var message = await SingleVisitEventAsync(harness.DbContext, "VisitClosed");
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Equal(VisitStatusCodes.Closed, payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(1500m, payload.RootElement.GetProperty("finalTotalAmount").GetDecimal());
        Assert.True(payload.RootElement.GetProperty("closedAtUtc").GetDateTime() > default(DateTime));
    }

    private static Task<int> CountVisitEventsAsync(AppDbContext dbContext, string eventType)
    {
        return dbContext.OutboxMessages.CountAsync(x => x.ModuleCode == "visitops" && x.EventType == eventType);
    }

    private static async Task<Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration.OutboxMessage> SingleVisitEventAsync(
        AppDbContext dbContext,
        string eventType)
    {
        return await dbContext.OutboxMessages.SingleAsync(x => x.ModuleCode == "visitops" && x.EventType == eventType);
    }

    private sealed class VisitApplicationHarness : IAsyncDisposable
    {
        private VisitApplicationHarness(AppDbContext dbContext, VisitQueries queries, Guid visitId, Guid executionItemId)
        {
            DbContext = dbContext;
            Queries = queries;
            VisitId = visitId;
            ExecutionItemId = executionItemId;
        }

        public AppDbContext DbContext { get; }
        public VisitQueries Queries { get; }
        public Guid VisitId { get; }
        public Guid ExecutionItemId { get; }

        public static async Task<VisitApplicationHarness> CreateAsync()
        {
            new VisitOperationsModule().ConfigurePersistence();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"visit-application-{Guid.NewGuid():N}")
                .Options;

            var dbContext = new AppDbContext(options);
            var appointmentVisitService = new StubAppointmentVisitService();
            var queries = new VisitQueries(
                dbContext,
                appointmentVisitService,
                new StubVisitCatalogReadService(),
                new StubPetSummaryReadService(appointmentVisitService.PetId),
                new NoOpAccessAuditService(),
                new NoOpAuditTrailService(),
                new OutboxPublisher(dbContext));

            var visit = Visit.CheckIn(
                Guid.NewGuid(),
                appointmentVisitService.AppointmentId,
                [new VisitExecutionItemDraft(
                    appointmentVisitService.AppointmentItemId,
                    "Package",
                    Guid.NewGuid(),
                    appointmentVisitService.OfferVersionId,
                    "BASIC",
                    "Basic Groom",
                    1,
                    1500m,
                    90,
                    120)],
                null,
                DateTime.SpecifyKind(DateTime.Parse("2026-04-24T07:00:00"), DateTimeKind.Utc));

            dbContext.Set<Visit>().Add(visit);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            return new VisitApplicationHarness(dbContext, queries, visit.Id, visit.ExecutionItems.Single().Id);
        }

        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }
    }

    private sealed class StubAppointmentVisitService : IAppointmentVisitService
    {
        public Guid AppointmentId { get; } = Guid.NewGuid();
        public Guid PetId { get; } = Guid.NewGuid();
        public Guid GroomerId { get; } = Guid.NewGuid();
        public Guid AppointmentItemId { get; } = Guid.NewGuid();
        public Guid OfferVersionId { get; } = Guid.NewGuid();

        public Task<VisitAppointmentInfo?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<VisitAppointmentInfo?>(appointmentId == AppointmentId
                ? CreateAppointment()
                : null);
        }

        public Task<IReadOnlyDictionary<Guid, VisitAppointmentInfo>> ListAppointmentsAsync(
            IReadOnlyCollection<Guid> appointmentIds,
            DateTime? fromUtc,
            DateTime? toUtc,
            Guid? groomerId,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, VisitAppointmentInfo> result = appointmentIds.Contains(AppointmentId)
                ? new Dictionary<Guid, VisitAppointmentInfo> { [AppointmentId] = CreateAppointment() }
                : new Dictionary<Guid, VisitAppointmentInfo>();
            return Task.FromResult(result);
        }

        public Task MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private VisitAppointmentInfo CreateAppointment()
        {
            return new VisitAppointmentInfo(
                AppointmentId,
                null,
                PetId,
                GroomerId,
                DateTime.SpecifyKind(DateTime.Parse("2026-04-24T07:00:00"), DateTimeKind.Utc),
                DateTime.SpecifyKind(DateTime.Parse("2026-04-24T09:00:00"), DateTimeKind.Utc),
                "CheckedIn",
                1,
                [new VisitAppointmentItemInfo(
                    AppointmentItemId,
                    "Package",
                    Guid.NewGuid(),
                    OfferVersionId,
                    "BASIC",
                    "Basic Groom",
                    1,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    1500m,
                    90,
                    120)]);
        }
    }

    private sealed class StubVisitCatalogReadService : IVisitCatalogReadService
    {
        public Task<IReadOnlyCollection<OfferExecutionComponentInfo>> GetIncludedComponentsAsync(Guid offerVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<OfferExecutionComponentInfo>>([]);
        }

        public Task<OfferExecutionComponentInfo?> GetComponentAsync(Guid offerVersionComponentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<OfferExecutionComponentInfo?>(null);
        }

        public Task<ProcedureReadModel?> GetProcedureAsync(Guid procedureId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ProcedureReadModel?>(null);
        }
    }

    private sealed class StubPetSummaryReadService(Guid petId) : IPetSummaryReadService
    {
        public Task<PetSummaryReadModel?> GetPetSummaryAsync(Guid requestedPetId, CancellationToken cancellationToken)
        {
            return Task.FromResult<PetSummaryReadModel?>(requestedPetId == petId
                ? new PetSummaryReadModel(petId, "Milo", null, "DOG", "Dog", "Samoyed", "DOUBLE_COAT", "LARGE")
                : null);
        }
    }

    private sealed class NoOpAccessAuditService : IAccessAuditService
    {
        public Task RecordAsync(string resourceType, string resourceId, string actionCode, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpAuditTrailService : IAuditTrailService
    {
        public Task RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

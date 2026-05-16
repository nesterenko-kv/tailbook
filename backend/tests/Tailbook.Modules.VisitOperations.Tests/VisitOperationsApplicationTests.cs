using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.Api.Tests;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.VisitOperations.Contracts;
using Xunit;

namespace Tailbook.Modules.VisitOperations.Tests;

public sealed class VisitOperationsApplicationTests
{
    [Fact]
    public async Task Invalid_adjustment_does_not_mutate_visit_or_publish_event()
    {
        await using var harness = await VisitApplicationHarness.CreateAsync();

        var result = await harness.ApplyPriceAdjustmentHandler.ExecuteAsync(
            new ApplyVisitPriceAdjustmentUseCaseCommand(
                harness.VisitId,
                -1,
                5000m,
                "INVALID_REDUCTION",
                null,
                TargetItemId: null,
                Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "VisitOperations.NegativeFinalTotal");

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

        await harness.ApplyPriceAdjustmentHandler.ExecuteAsync(
            new ApplyVisitPriceAdjustmentUseCaseCommand(
                harness.VisitId,
                -1,
                150.235m,
                " calmer_than_expected ",
                "Goodwill.",
                TargetItemId: null,
                Guid.Empty),
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

        var result = await harness.CloseVisitHandler.ExecuteAsync(
            new CloseVisitUseCaseCommand(harness.VisitId, Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "VisitOperations.VisitCloseFailed");

        var visit = await harness.DbContext.Set<Visit>().SingleAsync(x => x.Id == harness.VisitId);
        Assert.Equal(VisitStatusCodes.Open, visit.Status);
        Assert.Null(visit.ClosedAt);
        Assert.Equal(0, await CountVisitEventsAsync(harness.DbContext, "VisitClosed"));
    }

    [Fact]
    public async Task Successful_close_publishes_final_state_payload()
    {
        await using var harness = await VisitApplicationHarness.CreateAsync();

        await harness.CompleteVisitHandler.ExecuteAsync(new CompleteVisitUseCaseCommand(harness.VisitId, Guid.Empty), CancellationToken.None);
        await harness.CloseVisitHandler.ExecuteAsync(new CloseVisitUseCaseCommand(harness.VisitId, Guid.Empty), CancellationToken.None);

        var message = await SingleVisitEventAsync(harness.DbContext, "VisitClosed");
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Equal(VisitStatusCodes.Closed, payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(1500m, payload.RootElement.GetProperty("finalTotalAmount").GetDecimal());
        Assert.True(payload.RootElement.GetProperty("closedAt").GetDateTimeOffset() > default(DateTimeOffset));
    }

    private static Task<int> CountVisitEventsAsync(AppDbContext dbContext, string eventType)
    {
        return dbContext.OutboxMessages.CountAsync(x => x.ModuleCode == "visitops" && x.EventType == eventType);
    }

    private static async Task<OutboxMessage> SingleVisitEventAsync(
        AppDbContext dbContext,
        string eventType)
    {
        return await dbContext.OutboxMessages.SingleAsync(x => x.ModuleCode == "visitops" && x.EventType == eventType);
    }

    private sealed class VisitApplicationHarness : IAsyncDisposable
    {
        private VisitApplicationHarness(
            AppDbContext dbContext,
            ApplyVisitPriceAdjustmentUseCaseCommandHandler applyPriceAdjustmentHandler,
            CompleteVisitUseCaseCommandHandler completeVisitHandler,
            CloseVisitUseCaseCommandHandler closeVisitHandler,
            Guid visitId,
            Guid executionItemId)
        {
            DbContext = dbContext;
            ApplyPriceAdjustmentHandler = applyPriceAdjustmentHandler;
            CompleteVisitHandler = completeVisitHandler;
            CloseVisitHandler = closeVisitHandler;
            VisitId = visitId;
            ExecutionItemId = executionItemId;
        }

        public AppDbContext DbContext { get; }
        public ApplyVisitPriceAdjustmentUseCaseCommandHandler ApplyPriceAdjustmentHandler { get; }
        public CompleteVisitUseCaseCommandHandler CompleteVisitHandler { get; }
        public CloseVisitUseCaseCommandHandler CloseVisitHandler { get; }
        public Guid VisitId { get; }
        public Guid ExecutionItemId { get; }

        public static async Task<VisitApplicationHarness> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"visit-application-{Guid.NewGuid():N}")
                .Options;

            var dbContext = TestModelConfiguration.CreateDbContext(options);
            var appointmentVisitService = new StubAppointmentVisitService();
            var timeProvider = TimeProvider.System;
            var visitCatalogReadService = new StubVisitCatalogReadService();
            var auditTrailService = new NoOpAuditTrailService();
            var outboxPublisher = new OutboxPublisher(dbContext, timeProvider);
            var visitReadService = new VisitReadService(
                dbContext,
                appointmentVisitService,
                visitCatalogReadService,
                new StubPetSummaryReadService(appointmentVisitService.PetId),
                new NoOpAccessAuditService());
            var applyPriceAdjustmentHandler = new ApplyVisitPriceAdjustmentUseCaseCommandHandler(
                dbContext,
                visitReadService,
                auditTrailService,
                outboxPublisher,
                timeProvider);
            var completeVisitHandler = new CompleteVisitUseCaseCommandHandler(
                dbContext,
                visitReadService,
                appointmentVisitService,
                visitCatalogReadService,
                auditTrailService,
                outboxPublisher,
                timeProvider);
            var closeVisitHandler = new CloseVisitUseCaseCommandHandler(
                dbContext,
                visitReadService,
                appointmentVisitService,
                auditTrailService,
                outboxPublisher,
                timeProvider);

            var visitResult = Visit.CheckIn(
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
                DateTimeOffset.Parse("2026-04-24T07:00:00").ToUniversalTime());
            Assert.False(visitResult.IsError, string.Join("; ", visitResult.Errors.Select(error => error.Description)));
            var visit = visitResult.Value;

            dbContext.Set<Visit>().Add(visit);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();

            return new VisitApplicationHarness(
                dbContext,
                applyPriceAdjustmentHandler,
                completeVisitHandler,
                closeVisitHandler,
                visit.Id,
                visit.ExecutionItems.Single().Id);
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
            return Task.FromResult(appointmentId == AppointmentId
                ? CreateAppointment()
                : null);
        }

        public Task<IReadOnlyDictionary<Guid, VisitAppointmentInfo>> ListAppointmentsAsync(
            IReadOnlyCollection<Guid> appointmentIds,
            DateTimeOffset? from,
            DateTimeOffset? to,
            Guid? groomerId,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, VisitAppointmentInfo> result = appointmentIds.Contains(AppointmentId)
                ? new Dictionary<Guid, VisitAppointmentInfo> { [AppointmentId] = CreateAppointment() }
                : new Dictionary<Guid, VisitAppointmentInfo>();
            return Task.FromResult(result);
        }

        public Task<ErrorOr<Success>> MarkCheckedInAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<Success>>(Result.Success);
        }

        public Task<ErrorOr<Success>> MarkInProgressAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<Success>>(Result.Success);
        }

        public Task<ErrorOr<Success>> MarkCompletedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<Success>>(Result.Success);
        }

        public Task<ErrorOr<Success>> MarkClosedAsync(Guid appointmentId, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<Success>>(Result.Success);
        }

        private VisitAppointmentInfo CreateAppointment()
        {
            return new VisitAppointmentInfo(
                AppointmentId,
                null,
                PetId,
                GroomerId,
                DateTimeOffset.Parse("2026-04-24T07:00:00").ToUniversalTime(),
                DateTimeOffset.Parse("2026-04-24T09:00:00").ToUniversalTime(),
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
            return Task.FromResult(requestedPetId == petId
                ? new PetSummaryReadModel(petId, "Milo", null, "DOG", "Dog", "Samoyed", "DOUBLE_COAT", "LARGE")
                : null);
        }

        public Task<IReadOnlyCollection<PetSummaryReadModel>> ListPetSummariesByClientAsync(Guid clientId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PetSummaryReadModel>>([]);
        }

        public Task<IReadOnlyCollection<Guid>> SearchPetIdsAsync(string? search, int maxResults, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Guid>>([petId]);
        }
    }

    private sealed class NoOpAccessAuditService : IAccessAuditService
    {
        public ValueTask RecordAsync(string resourceType, string resourceId, string actionCode, Guid? actorUserId, CancellationToken cancellationToken)
        {
            return default;
        }
    }

    private sealed class NoOpAuditTrailService : IAuditTrailService
    {
        public ValueTask RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken)
        {
            return default;
        }
    }
}

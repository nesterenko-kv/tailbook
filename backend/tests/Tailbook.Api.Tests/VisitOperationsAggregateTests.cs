using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations;
using Tailbook.Modules.VisitOperations.Contracts;
using Tailbook.Modules.VisitOperations.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class VisitOperationsAggregateTests
{
    [Fact]
    public void Check_in_builds_open_visit_with_execution_items()
    {
        var visitId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();

        var visit = Visit.CheckIn(
            visitId,
            appointmentId,
            [CreateExecutionItemDraft()],
            actorUserId,
            Utc("2026-04-24T07:00:00Z"));

        Assert.Equal(visitId, visit.Id);
        Assert.Equal(appointmentId, visit.AppointmentId);
        Assert.Equal(VisitStatusCodes.Open, visit.Status);
        Assert.Equal(actorUserId, visit.CreatedByUserId);
        var item = Assert.Single(visit.ExecutionItems);
        Assert.Equal(visit.Id, item.VisitId);
        Assert.Equal("Package", item.ItemType);
        Assert.Equal(1500m, visit.AppointmentTotalAmount);
    }

    [Fact]
    public void Check_in_rejects_missing_ids_and_empty_items()
    {
        Assert.Throws<InvalidOperationException>(() => Visit.CheckIn(
            Guid.Empty,
            Guid.NewGuid(),
            [CreateExecutionItemDraft()],
            null,
            Utc("2026-04-24T07:00:00Z")));

        Assert.Throws<InvalidOperationException>(() => Visit.CheckIn(
            Guid.NewGuid(),
            Guid.Empty,
            [CreateExecutionItemDraft()],
            null,
            Utc("2026-04-24T07:00:00Z")));

        Assert.Throws<InvalidOperationException>(() => Visit.CheckIn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            null,
            Utc("2026-04-24T07:00:00Z")));
    }

    [Fact]
    public void Check_in_rejects_invalid_execution_item_data()
    {
        AssertInvalidExecutionItem(CreateExecutionItemDraft(appointmentItemId: Guid.Empty));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(itemType: " "));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(offerId: Guid.Empty));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(offerVersionId: Guid.Empty));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(offerCodeSnapshot: " "));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(offerDisplayNameSnapshot: " "));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(quantity: 0));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(priceAmountSnapshot: -1m));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(serviceMinutesSnapshot: 0));
        AssertInvalidExecutionItem(CreateExecutionItemDraft(reservedMinutesSnapshot: 0));
    }

    [Fact]
    public void Child_collections_cannot_be_externally_mutated()
    {
        var visit = CreateVisit();

        var executionItems = Assert.IsAssignableFrom<ICollection<VisitExecutionItem>>(visit.ExecutionItems);
        var priceAdjustments = Assert.IsAssignableFrom<ICollection<VisitPriceAdjustment>>(visit.PriceAdjustments);
        var item = Assert.Single(visit.ExecutionItems);
        var performed = Assert.IsAssignableFrom<ICollection<VisitPerformedProcedure>>(item.PerformedProcedures);
        var skipped = Assert.IsAssignableFrom<ICollection<VisitSkippedComponent>>(item.SkippedComponents);

        Assert.True(executionItems.IsReadOnly);
        Assert.True(priceAdjustments.IsReadOnly);
        Assert.True(performed.IsReadOnly);
        Assert.True(skipped.IsReadOnly);
        Assert.Throws<NotSupportedException>(executionItems.Clear);
        Assert.Throws<NotSupportedException>(priceAdjustments.Clear);
        Assert.Throws<NotSupportedException>(performed.Clear);
        Assert.Throws<NotSupportedException>(skipped.Clear);
    }

    [Fact]
    public void Recording_performed_procedure_starts_visit_and_blocks_duplicates()
    {
        var visit = CreateVisit();
        var item = visit.ExecutionItems.Single();

        var performed = visit.RecordPerformedProcedure(
            item.Id,
            CreatePerformedProcedureDraft(note: " done "),
            null,
            Utc("2026-04-24T07:15:00Z"));

        Assert.Equal(VisitStatusCodes.InProgress, visit.Status);
        Assert.Equal(Utc("2026-04-24T07:15:00Z"), visit.StartedAtUtc);
        Assert.Equal("done", performed.Note);
        Assert.Single(item.PerformedProcedures);
        Assert.Throws<InvalidOperationException>(() => visit.RecordPerformedProcedure(
            item.Id,
            CreatePerformedProcedureDraft(),
            null,
            Utc("2026-04-24T07:20:00Z")));
    }

    [Fact]
    public void Recording_skipped_component_normalizes_reason_and_blocks_duplicates()
    {
        var visit = CreateVisit();
        var item = visit.ExecutionItems.Single();

        var skipped = visit.RecordSkippedComponent(
            item.Id,
            CreateSkippedComponentDraft(omissionReasonCode: " pet_stressed ", note: "  Too stressed. "),
            null,
            Utc("2026-04-24T07:20:00Z"));

        Assert.Equal(VisitStatusCodes.InProgress, visit.Status);
        Assert.Equal("PET_STRESSED", skipped.OmissionReasonCode);
        Assert.Equal("Too stressed.", skipped.Note);
        Assert.Single(item.SkippedComponents);
        Assert.Throws<InvalidOperationException>(() => visit.RecordSkippedComponent(
            item.Id,
            CreateSkippedComponentDraft(),
            null,
            Utc("2026-04-24T07:25:00Z")));
    }

    [Fact]
    public void Price_adjustment_rounds_normalizes_and_blocks_negative_final_total()
    {
        var visit = CreateVisit();

        var adjustment = visit.ApplyPriceAdjustment(
            new VisitPriceAdjustmentDraft(-1, 150.235m, " calmer_than_expected ", "  Goodwill. "),
            null,
            Utc("2026-04-24T08:00:00Z"));

        Assert.Equal(-1, adjustment.Sign);
        Assert.Equal(150.24m, adjustment.Amount);
        Assert.Equal("CALMER_THAN_EXPECTED", adjustment.ReasonCode);
        Assert.Equal("Goodwill.", adjustment.Note);
        Assert.Equal(1349.76m, visit.FinalTotalAmount);
        Assert.Throws<InvalidOperationException>(() => visit.ApplyPriceAdjustment(
            new VisitPriceAdjustmentDraft(-1, 5000m, "INVALID_REDUCTION", null),
            null,
            Utc("2026-04-24T08:05:00Z")));
    }

    [Fact]
    public void Lifecycle_allows_complete_then_close()
    {
        var visit = CreateVisit();

        visit.Complete(null, Utc("2026-04-24T09:00:00Z"));
        Assert.Equal(VisitStatusCodes.AwaitingFinalization, visit.Status);
        Assert.Equal(Utc("2026-04-24T09:00:00Z"), visit.CompletedAtUtc);

        visit.Close(null, Utc("2026-04-24T09:15:00Z"));
        Assert.Equal(VisitStatusCodes.Closed, visit.Status);
        Assert.Equal(Utc("2026-04-24T09:15:00Z"), visit.ClosedAtUtc);
    }

    [Fact]
    public void Lifecycle_rejects_forbidden_paths_and_terminal_mutations()
    {
        var visit = CreateVisit();
        var itemId = visit.ExecutionItems.Single().Id;

        Assert.Throws<InvalidOperationException>(() => visit.Close(null, Utc("2026-04-24T08:30:00Z")));

        visit.Complete(null, Utc("2026-04-24T09:00:00Z"));
        Assert.Throws<InvalidOperationException>(() => visit.RecordPerformedProcedure(
            itemId,
            CreatePerformedProcedureDraft(),
            null,
            Utc("2026-04-24T09:05:00Z")));

        visit.Close(null, Utc("2026-04-24T09:15:00Z"));
        Assert.Throws<InvalidOperationException>(() => visit.Complete(null, Utc("2026-04-24T09:20:00Z")));
        Assert.Throws<InvalidOperationException>(() => visit.ApplyPriceAdjustment(
            new VisitPriceAdjustmentDraft(1, 50m, "SURCHARGE", null),
            null,
            Utc("2026-04-24T09:25:00Z")));
    }

    [Fact]
    public async Task Visit_aggregate_round_trips_through_ef_core()
    {
        TestModelConfiguration.Configure();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"visit-aggregate-{Guid.NewGuid():N}")
            .Options;

        var visit = CreateVisit();
        var itemId = visit.ExecutionItems.Single().Id;
        visit.RecordPerformedProcedure(itemId, CreatePerformedProcedureDraft(), null, Utc("2026-04-24T07:15:00Z"));
        visit.RecordSkippedComponent(itemId, CreateSkippedComponentDraft(), null, Utc("2026-04-24T07:20:00Z"));
        visit.ApplyPriceAdjustment(new VisitPriceAdjustmentDraft(-1, 150m, "CALMER_THAN_EXPECTED", null), null, Utc("2026-04-24T08:00:00Z"));

        await using (var dbContext = new AppDbContext(options))
        {
            dbContext.Set<Visit>().Add(visit);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var loaded = await dbContext.Set<Visit>()
                .AsNoTracking()
                .Include(x => x.ExecutionItems)
                .ThenInclude(x => x.PerformedProcedures)
                .Include(x => x.ExecutionItems)
                .ThenInclude(x => x.SkippedComponents)
                .Include(x => x.PriceAdjustments)
                .SingleAsync(x => x.Id == visit.Id);

            Assert.Equal(VisitStatusCodes.InProgress, loaded.Status);
            Assert.Equal(1350m, loaded.FinalTotalAmount);
            var loadedItem = Assert.Single(loaded.ExecutionItems);
            Assert.Single(loadedItem.PerformedProcedures);
            Assert.Single(loadedItem.SkippedComponents);
            Assert.Single(loaded.PriceAdjustments);
        }
    }

    private static void AssertInvalidExecutionItem(VisitExecutionItemDraft draft)
    {
        Assert.Throws<InvalidOperationException>(() => Visit.CheckIn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [draft],
            null,
            Utc("2026-04-24T07:00:00Z")));
    }

    private static Visit CreateVisit()
    {
        return Visit.CheckIn(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [CreateExecutionItemDraft()],
            null,
            Utc("2026-04-24T07:00:00Z"));
    }

    private static VisitExecutionItemDraft CreateExecutionItemDraft(
        Guid? appointmentItemId = null,
        string itemType = " Package ",
        Guid? offerId = null,
        Guid? offerVersionId = null,
        string offerCodeSnapshot = " BASIC ",
        string offerDisplayNameSnapshot = " Basic Groom ",
        int quantity = 1,
        decimal priceAmountSnapshot = 1500m,
        int serviceMinutesSnapshot = 90,
        int reservedMinutesSnapshot = 120)
    {
        return new VisitExecutionItemDraft(
            appointmentItemId ?? Guid.NewGuid(),
            itemType,
            offerId ?? Guid.NewGuid(),
            offerVersionId ?? Guid.NewGuid(),
            offerCodeSnapshot,
            offerDisplayNameSnapshot,
            quantity,
            priceAmountSnapshot,
            serviceMinutesSnapshot,
            reservedMinutesSnapshot);
    }

    private static VisitPerformedProcedureDraft CreatePerformedProcedureDraft(
        Guid? procedureId = null,
        string procedureCodeSnapshot = " BATH ",
        string procedureNameSnapshot = " Bath ",
        string? note = null)
    {
        return new VisitPerformedProcedureDraft(
            procedureId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            procedureCodeSnapshot,
            procedureNameSnapshot,
            note);
    }

    private static VisitSkippedComponentDraft CreateSkippedComponentDraft(
        Guid? offerVersionComponentId = null,
        Guid? procedureId = null,
        string procedureCodeSnapshot = " DRY ",
        string procedureNameSnapshot = " Dry ",
        string? omissionReasonCode = "PET_STRESSED",
        string? note = null)
    {
        return new VisitSkippedComponentDraft(
            offerVersionComponentId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
            procedureId ?? Guid.Parse("33333333-3333-3333-3333-333333333333"),
            procedureCodeSnapshot,
            procedureNameSnapshot,
            omissionReasonCode,
            note);
    }

    private static DateTime Utc(string value)
    {
        return DateTime.Parse(value).ToUniversalTime();
    }
}

using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

public sealed class BookingSnapshotComposer(
    AppDbContext dbContext,
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogQuoteResolver catalogQuoteResolver,
    IStaffSchedulingService staffSchedulingService)
{
    public async Task<QuotePreviewView> CreatePreviewAsync(PreviewQuoteCommand command, string? actorUserId, CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken)
            ?? throw new InvalidOperationException("Pet does not exist.");

        var resolution = await catalogQuoteResolver.ResolveAsync(
            pet,
            command.Items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
            cancellationToken);

        var durationResolution = await ResolveDurationAsync(command.GroomerId, command.PetId, resolution, cancellationToken);
        var actorGuid = ParseGuid(actorUserId);

        var priceSnapshot = new PriceSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = SnapshotTypeCodes.QuotePreview,
            Currency = resolution.Currency,
            TotalAmount = resolution.TotalAmount,
            RuleSetId = resolution.PriceRuleSetId,
            CreatedByUserId = actorGuid,
            CreatedAtUtc = DateTime.UtcNow
        };

        var durationSnapshot = new DurationSnapshot
        {
            Id = Guid.NewGuid(),
            ServiceMinutes = resolution.ServiceMinutes,
            ReservedMinutes = durationResolution.EffectiveReservedMinutes,
            RuleSetId = resolution.DurationRuleSetId,
            CreatedByUserId = actorGuid,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<PriceSnapshot>().Add(priceSnapshot);
        dbContext.Set<DurationSnapshot>().Add(durationSnapshot);

        dbContext.Set<PriceSnapshotLine>().AddRange(resolution.PriceLines.Select(x => new PriceSnapshotLine
        {
            Id = Guid.NewGuid(),
            PriceSnapshotId = priceSnapshot.Id,
            LineType = x.LineType,
            Label = x.Label,
            Amount = x.Amount,
            SourceRuleId = x.SourceRuleId,
            SequenceNo = x.SequenceNo
        }));

        dbContext.Set<DurationSnapshotLine>().AddRange(durationResolution.Lines.Select(x => new DurationSnapshotLine
        {
            Id = Guid.NewGuid(),
            DurationSnapshotId = durationSnapshot.Id,
            LineType = x.LineType,
            Label = x.Label,
            Minutes = x.Minutes,
            SourceRuleId = x.SourceRuleId,
            SequenceNo = x.SequenceNo
        }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new QuotePreviewView(
            new PriceSnapshotView(
                priceSnapshot.Id,
                priceSnapshot.Currency,
                priceSnapshot.TotalAmount,
                resolution.PriceLines.Select(x => new PriceSnapshotLineView(x.LineType, x.Label, x.Amount, x.SourceRuleId, x.SequenceNo)).ToArray()),
            new DurationSnapshotView(
                durationSnapshot.Id,
                durationSnapshot.ServiceMinutes,
                durationSnapshot.ReservedMinutes,
                durationResolution.Lines.Select(x => new DurationSnapshotLineView(x.LineType, x.Label, x.Minutes, x.SourceRuleId, x.SequenceNo)).ToArray()),
            resolution.Items.Select(x => new QuotePreviewItemView(x.OfferId, x.OfferVersionId, x.OfferCode, x.OfferType, x.DisplayName, x.PriceAmount, x.ServiceMinutes, x.ReservedMinutes)).ToArray());
    }

    public async Task<AppointmentCompositionResult> ComposeAppointmentAsync(
        Guid petId,
        Guid groomerId,
        DateTime startAtUtc,
        IReadOnlyCollection<PreviewQuoteItemCommand> items,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least one appointment item is required.");
        }

        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken)
            ?? throw new InvalidOperationException("Pet does not exist.");

        var overallResolution = await catalogQuoteResolver.ResolveAsync(
            pet,
            items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
            cancellationToken);

        var availability = await staffSchedulingService.CheckAvailabilityAsync(
            groomerId,
            petId,
            overallResolution.Items.Select(x => x.OfferId).ToArray(),
            DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc),
            overallResolution.ReservedMinutes,
            null,
            cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException(string.Join(" ", availability.Reasons));
        }

        var actorGuid = ParseGuid(actorUserId);
        var perItemCompositions = new List<AppointmentItemComposition>();

        foreach (var item in items)
        {
            var singleResolution = await catalogQuoteResolver.ResolveAsync(
                pet,
                [new QuotePreviewCatalogItem(item.OfferId, item.ItemType)],
                cancellationToken);

            var resolvedItem = singleResolution.Items.Single();
            var priceSnapshot = new PriceSnapshot
            {
                Id = Guid.NewGuid(),
                SnapshotType = SnapshotTypeCodes.AppointmentEstimate,
                Currency = singleResolution.Currency,
                TotalAmount = singleResolution.TotalAmount,
                RuleSetId = singleResolution.PriceRuleSetId,
                CreatedByUserId = actorGuid,
                CreatedAtUtc = DateTime.UtcNow
            };

            var durationSnapshot = new DurationSnapshot
            {
                Id = Guid.NewGuid(),
                ServiceMinutes = singleResolution.ServiceMinutes,
                ReservedMinutes = singleResolution.ReservedMinutes,
                RuleSetId = singleResolution.DurationRuleSetId,
                CreatedByUserId = actorGuid,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Set<PriceSnapshot>().Add(priceSnapshot);
            dbContext.Set<DurationSnapshot>().Add(durationSnapshot);

            var priceLines = singleResolution.PriceLines.Select(x => new PriceSnapshotLine
            {
                Id = Guid.NewGuid(),
                PriceSnapshotId = priceSnapshot.Id,
                LineType = x.LineType,
                Label = x.Label,
                Amount = x.Amount,
                SourceRuleId = x.SourceRuleId,
                SequenceNo = x.SequenceNo
            }).ToList();

            var durationLines = singleResolution.DurationLines.Select(x => new DurationSnapshotLine
            {
                Id = Guid.NewGuid(),
                DurationSnapshotId = durationSnapshot.Id,
                LineType = x.LineType,
                Label = x.Label,
                Minutes = x.Minutes,
                SourceRuleId = x.SourceRuleId,
                SequenceNo = x.SequenceNo
            }).ToList();

            dbContext.Set<PriceSnapshotLine>().AddRange(priceLines);
            dbContext.Set<DurationSnapshotLine>().AddRange(durationLines);

            perItemCompositions.Add(new AppointmentItemComposition
            {
                OfferId = item.OfferId,
                OfferVersionId = resolvedItem.OfferVersionId,
                OfferCode = resolvedItem.OfferCode,
                OfferType = resolvedItem.OfferType,
                DisplayName = resolvedItem.DisplayName,
                PriceSnapshot = priceSnapshot,
                PriceLines = priceLines.Select(x => new PriceSnapshotLineView(x.LineType, x.Label, x.Amount, x.SourceRuleId, x.SequenceNo)).ToArray(),
                DurationSnapshot = durationSnapshot,
                DurationLines = durationLines.Select(x => new DurationSnapshotLineView(x.LineType, x.Label, x.Minutes, x.SourceRuleId, x.SequenceNo)).ToArray()
            });
        }

        var modifierMinutes = availability.CheckedReservedMinutes - overallResolution.ReservedMinutes;
        if (modifierMinutes != 0 && perItemCompositions.Count > 0)
        {
            var first = perItemCompositions[0];
            first.DurationSnapshot.ReservedMinutes = Math.Max(15, first.DurationSnapshot.ReservedMinutes + modifierMinutes);

            var nextSequenceNo = first.DurationLines.Count == 0 ? 1 : first.DurationLines.Max(x => x.SequenceNo) + 1;
            var modifierLineEntity = new DurationSnapshotLine
            {
                Id = Guid.NewGuid(),
                DurationSnapshotId = first.DurationSnapshot.Id,
                LineType = "GroomerCapabilityModifier",
                Label = $"Groomer capability modifier ({modifierMinutes:+#;-#;0} min)",
                Minutes = modifierMinutes,
                SourceRuleId = null,
                SequenceNo = nextSequenceNo
            };
            dbContext.Set<DurationSnapshotLine>().Add(modifierLineEntity);

            first.DurationLines = first.DurationLines.Concat([
                new DurationSnapshotLineView(
                    modifierLineEntity.LineType,
                    modifierLineEntity.Label,
                    modifierLineEntity.Minutes,
                    modifierLineEntity.SourceRuleId,
                    modifierLineEntity.SequenceNo)
            ]).ToArray();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AppointmentCompositionResult(
            pet.ClientId,
            DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc),
            availability.EndAtUtc,
            overallResolution.TotalAmount,
            overallResolution.ServiceMinutes,
            availability.CheckedReservedMinutes,
            perItemCompositions);
    }

    private async Task<ResolvedDurationLines> ResolveDurationAsync(Guid? groomerId, Guid petId, CatalogQuoteResolution resolution, CancellationToken cancellationToken)
    {
        var effectiveReservedMinutes = resolution.ReservedMinutes;
        var durationLines = resolution.DurationLines.ToList();

        if (groomerId is not null)
        {
            var durationResolution = await staffSchedulingService.ResolveReservedDurationAsync(
                groomerId.Value,
                petId,
                resolution.Items.Select(x => x.OfferId).ToArray(),
                resolution.ReservedMinutes,
                cancellationToken);

            effectiveReservedMinutes = durationResolution.EffectiveReservedMinutes;
            var nextSequenceNo = durationLines.Count == 0 ? 1 : durationLines.Max(x => x.SequenceNo) + 1;

            if (durationResolution.ModifierMinutes != 0)
            {
                durationLines.Add(new CatalogQuoteDurationLine(
                    Guid.Empty,
                    Guid.Empty,
                    "GroomerCapabilityModifier",
                    $"Groomer capability modifier ({durationResolution.ModifierMinutes:+#;-#;0} min)",
                    durationResolution.ModifierMinutes,
                    null,
                    nextSequenceNo));
            }
        }

        return new ResolvedDurationLines(effectiveReservedMinutes, durationLines);
    }

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private sealed record ResolvedDurationLines(int EffectiveReservedMinutes, IReadOnlyCollection<CatalogQuoteDurationLine> Lines);
}

public sealed record AppointmentCompositionResult(
    Guid? ClientId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    IReadOnlyCollection<AppointmentItemComposition> Items);

public sealed class AppointmentItemComposition
{
    public Guid OfferId { get; set; }
    public Guid OfferVersionId { get; set; }
    public string OfferCode { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PriceSnapshot PriceSnapshot { get; set; } = null!;
    public IReadOnlyCollection<PriceSnapshotLineView> PriceLines { get; set; } = [];
    public DurationSnapshot DurationSnapshot { get; set; } = null!;
    public IReadOnlyCollection<DurationSnapshotLineView> DurationLines { get; set; } = [];
}

using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class BookingSnapshotComposer(
    AppDbContext dbContext,
    IPetQuoteProfileService petQuoteProfileService,
    ICatalogQuoteResolver catalogQuoteResolver,
    IStaffSchedulingService staffSchedulingService) : IBookingSnapshotComposer
{
    public async Task<ErrorOr<QuotePreviewView>> CreatePreviewAsync(PreviewQuoteQuery command, string? actorUserId, CancellationToken cancellationToken)
    {
        var pet = await petQuoteProfileService.GetPetAsync(command.PetId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
        }

        var resolutionResult = await catalogQuoteResolver.ResolveAsync(
            pet,
            command.Items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
            cancellationToken);
        if (resolutionResult.IsError)
        {
            return resolutionResult.Errors;
        }

        var resolution = resolutionResult.Value;
        var durationResolutionResult = await ResolveDurationAsync(command.GroomerId, command.PetId, resolution, cancellationToken);
        if (durationResolutionResult.IsError)
        {
            return durationResolutionResult.Errors;
        }

        var durationResolution = durationResolutionResult.Value;
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

    public async Task<ErrorOr<AppointmentCompositionResult>> ComposeAppointmentAsync(
        Guid petId,
        Guid groomerId,
        DateTime startAtUtc,
        IReadOnlyCollection<PreviewQuoteItemQuery> items,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        var normalizedStartAtUtcResult = BookingTimeInputNormalizer.TryAssumeUtc(startAtUtc, nameof(startAtUtc));
        if (normalizedStartAtUtcResult.IsError)
        {
            return normalizedStartAtUtcResult.Errors;
        }

        var normalizedStartAtUtc = normalizedStartAtUtcResult.Value;
        if (items.Count == 0)
        {
            return Error.Validation("Booking.AppointmentItemRequired", "At least one appointment item is required.");
        }

        var pet = await petQuoteProfileService.GetPetAsync(petId, cancellationToken);
        if (pet is null)
        {
            return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
        }

        var overallResolutionResult = await catalogQuoteResolver.ResolveAsync(
            pet,
            items.Select(x => new QuotePreviewCatalogItem(x.OfferId, x.ItemType)).ToArray(),
            cancellationToken);
        if (overallResolutionResult.IsError)
        {
            return overallResolutionResult.Errors;
        }

        var overallResolution = overallResolutionResult.Value;

        var availabilityResult = await staffSchedulingService.CheckAvailabilityAsync(
            groomerId,
            petId,
            overallResolution.Items.Select(x => x.OfferId).ToArray(),
            normalizedStartAtUtc,
            overallResolution.ReservedMinutes,
            null,
            cancellationToken);
        if (availabilityResult.IsError)
        {
            return availabilityResult.Errors;
        }

        var availability = availabilityResult.Value;
        if (!availability.IsAvailable)
        {
            return Error.Validation("Booking.AppointmentSlotUnavailable", string.Join(" ", availability.Reasons));
        }

        var actorGuid = ParseGuid(actorUserId);
        var perItemCompositions = new List<AppointmentItemComposition>();

        foreach (var item in items)
        {
            var singleResolutionResult = await catalogQuoteResolver.ResolveAsync(
                pet,
                [new QuotePreviewCatalogItem(item.OfferId, item.ItemType)],
                cancellationToken);
            if (singleResolutionResult.IsError)
            {
                return singleResolutionResult.Errors;
            }

            var singleResolution = singleResolutionResult.Value;
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
            normalizedStartAtUtc,
            availability.EndAtUtc,
            overallResolution.TotalAmount,
            overallResolution.ServiceMinutes,
            availability.CheckedReservedMinutes,
            perItemCompositions);
    }

    private async Task<ErrorOr<ResolvedDurationLines>> ResolveDurationAsync(Guid? groomerId, Guid petId, CatalogQuoteResolution resolution, CancellationToken cancellationToken)
    {
        var effectiveReservedMinutes = resolution.ReservedMinutes;
        var durationLines = resolution.DurationLines.ToList();

        if (groomerId is not null)
        {
            var durationResolutionResult = await staffSchedulingService.ResolveReservedDurationAsync(
                groomerId.Value,
                petId,
                resolution.Items.Select(x => x.OfferId).ToArray(),
                resolution.ReservedMinutes,
                cancellationToken);
            if (durationResolutionResult.IsError)
            {
                return durationResolutionResult.Errors;
            }

            var durationResolution = durationResolutionResult.Value;
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

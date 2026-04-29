namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class VisitExecutionItem
{
    private readonly List<VisitPerformedProcedure> _performedProcedures = [];
    private readonly List<VisitSkippedComponent> _skippedComponents = [];

    private VisitExecutionItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid VisitId { get; private set; }
    public Guid AppointmentItemId { get; private set; }
    public string ItemType { get; private set; } = string.Empty;
    public Guid OfferId { get; private set; }
    public Guid OfferVersionId { get; private set; }
    public string OfferCodeSnapshot { get; private set; } = string.Empty;
    public string OfferDisplayNameSnapshot { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal PriceAmountSnapshot { get; private set; }
    public int ServiceMinutesSnapshot { get; private set; }
    public int ReservedMinutesSnapshot { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<VisitPerformedProcedure> PerformedProcedures => _performedProcedures.AsReadOnly();
    public IReadOnlyCollection<VisitSkippedComponent> SkippedComponents => _skippedComponents.AsReadOnly();
    public decimal TotalAmount => PriceAmountSnapshot * Quantity;
    public int TotalServiceMinutes => ServiceMinutesSnapshot * Quantity;
    public int TotalReservedMinutes => ReservedMinutesSnapshot * Quantity;

    internal static VisitExecutionItem Create(
        Guid id,
        Guid visitId,
        VisitExecutionItemDraft item,
        DateTime createdAtUtc)
    {
        if (item is null)
        {
            throw new InvalidOperationException("Visit execution item is required.");
        }

        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Visit execution item id is required.");
        }

        if (visitId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit execution item must belong to a visit.");
        }

        if (item.AppointmentItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit execution item must reference an appointment item.");
        }

        if (string.IsNullOrWhiteSpace(item.ItemType))
        {
            throw new InvalidOperationException("Visit execution item type is required.");
        }

        if (item.OfferId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit execution item must reference an offer.");
        }

        if (item.OfferVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit execution item must reference an offer version.");
        }

        if (string.IsNullOrWhiteSpace(item.OfferCodeSnapshot))
        {
            throw new InvalidOperationException("Visit execution item must include an offer code snapshot.");
        }

        if (string.IsNullOrWhiteSpace(item.OfferDisplayNameSnapshot))
        {
            throw new InvalidOperationException("Visit execution item must include an offer display name snapshot.");
        }

        if (item.Quantity <= 0)
        {
            throw new InvalidOperationException("Visit execution item quantity must be greater than zero.");
        }

        if (item.PriceAmountSnapshot < 0)
        {
            throw new InvalidOperationException("Visit execution item price cannot be negative.");
        }

        if (item.ServiceMinutesSnapshot <= 0)
        {
            throw new InvalidOperationException("Visit execution item service minutes must be greater than zero.");
        }

        if (item.ReservedMinutesSnapshot <= 0)
        {
            throw new InvalidOperationException("Visit execution item reserved minutes must be greater than zero.");
        }

        return new VisitExecutionItem
        {
            Id = id,
            VisitId = visitId,
            AppointmentItemId = item.AppointmentItemId,
            ItemType = item.ItemType.Trim(),
            OfferId = item.OfferId,
            OfferVersionId = item.OfferVersionId,
            OfferCodeSnapshot = item.OfferCodeSnapshot.Trim(),
            OfferDisplayNameSnapshot = item.OfferDisplayNameSnapshot.Trim(),
            Quantity = item.Quantity,
            PriceAmountSnapshot = item.PriceAmountSnapshot,
            ServiceMinutesSnapshot = item.ServiceMinutesSnapshot,
            ReservedMinutesSnapshot = item.ReservedMinutesSnapshot,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }

    internal VisitPerformedProcedure RecordPerformedProcedure(
        VisitPerformedProcedureDraft procedure,
        Guid? actorUserId,
        DateTime recordedAtUtc)
    {
        if (procedure is null)
        {
            throw new InvalidOperationException("Performed procedure is required.");
        }

        if (_performedProcedures.Any(x => x.ProcedureId == procedure.ProcedureId))
        {
            throw new InvalidOperationException("Procedure has already been recorded for this execution item.");
        }

        var performedProcedure = VisitPerformedProcedure.Create(
            Guid.NewGuid(),
            Id,
            procedure,
            actorUserId,
            recordedAtUtc);

        _performedProcedures.Add(performedProcedure);
        return performedProcedure;
    }

    internal VisitSkippedComponent RecordSkippedComponent(
        VisitSkippedComponentDraft component,
        Guid? actorUserId,
        DateTime recordedAtUtc)
    {
        if (component is null)
        {
            throw new InvalidOperationException("Skipped component is required.");
        }

        if (_skippedComponents.Any(x => x.OfferVersionComponentId == component.OfferVersionComponentId))
        {
            throw new InvalidOperationException("Component has already been marked as skipped for this execution item.");
        }

        var skippedComponent = VisitSkippedComponent.Create(
            Guid.NewGuid(),
            Id,
            component,
            actorUserId,
            recordedAtUtc);

        _skippedComponents.Add(skippedComponent);
        return skippedComponent;
    }
}

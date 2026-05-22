using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Domain.Entities;

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
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<VisitPerformedProcedure> PerformedProcedures => _performedProcedures.AsReadOnly();
    public IReadOnlyCollection<VisitSkippedComponent> SkippedComponents => _skippedComponents.AsReadOnly();
    public decimal TotalAmount => PriceAmountSnapshot * Quantity;
    public int TotalServiceMinutes => ServiceMinutesSnapshot * Quantity;
    public int TotalReservedMinutes => ReservedMinutesSnapshot * Quantity;

    internal static ErrorOr<VisitExecutionItem> Create(
        Guid id,
        Guid visitId,
        VisitExecutionItemDraft item,
        DateTimeOffset createdAt)
    {
        List<Error> errors = [];

        if (item is null)
        {
            return Error.Validation("VisitOperations.ExecutionItemRequired", "Visit execution item is required.");
        }

        if (id == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemIdRequired", "Visit execution item id is required."));
        }

        if (visitId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemVisitRequired", "Visit execution item must belong to a visit."));
        }

        if (item.AppointmentItemId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemAppointmentItemRequired", "Visit execution item must reference an appointment item."));
        }

        if (string.IsNullOrWhiteSpace(item.ItemType))
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemTypeRequired", "Visit execution item type is required."));
        }

        if (item.OfferId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemOfferRequired", "Visit execution item must reference an offer."));
        }

        if (item.OfferVersionId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemOfferVersionRequired", "Visit execution item must reference an offer version."));
        }

        if (string.IsNullOrWhiteSpace(item.OfferCodeSnapshot))
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemOfferCodeRequired", "Visit execution item must include an offer code snapshot."));
        }

        if (string.IsNullOrWhiteSpace(item.OfferDisplayNameSnapshot))
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemOfferDisplayNameRequired", "Visit execution item must include an offer display name snapshot."));
        }

        if (item.Quantity <= 0)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemQuantityInvalid", "Visit execution item quantity must be greater than zero."));
        }

        if (item.PriceAmountSnapshot < 0)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemPriceInvalid", "Visit execution item price cannot be negative."));
        }

        if (item.ServiceMinutesSnapshot <= 0)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemServiceMinutesInvalid", "Visit execution item service minutes must be greater than zero."));
        }

        if (item.ReservedMinutesSnapshot <= 0)
        {
            errors.Add(Error.Validation("VisitOperations.ExecutionItemReservedMinutesInvalid", "Visit execution item reserved minutes must be greater than zero."));
        }

        if (errors.Count > 0)
        {
            return errors;
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
            CreatedAt = createdAt.ToUniversalTime()
        };
    }

    internal ErrorOr<VisitPerformedProcedure> RecordPerformedProcedure(
        VisitPerformedProcedureDraft procedure,
        Guid? actorUserId,
        DateTimeOffset recordedAt)
    {
        if (procedure is null)
        {
            return Error.Validation("VisitOperations.PerformedProcedureRequired", "Performed procedure is required.");
        }

        if (_performedProcedures.Any(x => x.ProcedureId == procedure.ProcedureId))
        {
            return Error.Conflict("VisitOperations.ProcedureAlreadyRecorded", "Procedure has already been recorded for this execution item.");
        }

        var performedProcedure = VisitPerformedProcedure.Create(
            Guid.NewGuid(),
            Id,
            procedure,
            actorUserId,
            recordedAt);
        if (performedProcedure.IsError)
        {
            return performedProcedure.Errors;
        }

        _performedProcedures.Add(performedProcedure.Value);
        return performedProcedure.Value;
    }

    internal ErrorOr<VisitSkippedComponent> RecordSkippedComponent(
        VisitSkippedComponentDraft component,
        Guid? actorUserId,
        DateTimeOffset recordedAt)
    {
        if (component is null)
        {
            return Error.Validation("VisitOperations.SkippedComponentRequired", "Skipped component is required.");
        }

        if (_skippedComponents.Any(x => x.OfferVersionComponentId == component.OfferVersionComponentId))
        {
            return Error.Conflict("VisitOperations.ComponentAlreadySkipped", "Component has already been marked as skipped for this execution item.");
        }

        var skippedComponent = VisitSkippedComponent.Create(
            Guid.NewGuid(),
            Id,
            component,
            actorUserId,
            recordedAt);
        if (skippedComponent.IsError)
        {
            return skippedComponent.Errors;
        }

        _skippedComponents.Add(skippedComponent.Value);
        return skippedComponent.Value;
    }
}

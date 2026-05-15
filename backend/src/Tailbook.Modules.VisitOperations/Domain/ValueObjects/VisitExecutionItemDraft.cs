namespace Tailbook.Modules.VisitOperations.Domain.ValueObjects;

public sealed record VisitExecutionItemDraft(
    Guid AppointmentItemId,
    string ItemType,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCodeSnapshot,
    string OfferDisplayNameSnapshot,
    int Quantity,
    decimal PriceAmountSnapshot,
    int ServiceMinutesSnapshot,
    int ReservedMinutesSnapshot);

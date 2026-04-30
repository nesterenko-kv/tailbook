namespace Tailbook.Modules.VisitOperations.Domain.ValueObjects;

public sealed record VisitPriceAdjustmentDraft(
    int Sign,
    decimal Amount,
    string? ReasonCode,
    string? Note);

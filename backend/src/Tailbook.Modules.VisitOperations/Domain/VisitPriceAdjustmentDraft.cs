namespace Tailbook.Modules.VisitOperations.Domain;

public sealed record VisitPriceAdjustmentDraft(
    int Sign,
    decimal Amount,
    string? ReasonCode,
    string? Note);

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record VisitPriceAdjustmentView(Guid Id, int Sign, decimal Amount, string ReasonCode, string? Note, DateTimeOffset CreatedAt);
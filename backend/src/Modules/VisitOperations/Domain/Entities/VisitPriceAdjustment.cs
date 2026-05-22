using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Domain.Entities;

public sealed class VisitPriceAdjustment
{
    private VisitPriceAdjustment()
    {
    }

    public Guid Id { get; private set; }
    public Guid VisitId { get; private set; }
    public Guid? TargetItemId { get; private set; }
    public int Sign { get; private set; }
    public decimal Amount { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal static ErrorOr<VisitPriceAdjustment> Create(
        Guid id,
        Guid visitId,
        Guid? targetItemId,
        int sign,
        decimal amount,
        string? reasonCode,
        string? note,
        Guid? createdByUserId,
        DateTimeOffset createdAt)
    {
        List<Error> errors = [];

        if (id == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.PriceAdjustmentIdRequired", "Visit price adjustment id is required."));
        }

        if (visitId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.PriceAdjustmentVisitRequired", "Visit price adjustment must belong to a visit."));
        }

        if (sign is not -1 and not 1)
        {
            errors.Add(Error.Validation("VisitOperations.InvalidAdjustmentSign", "Adjustment sign must be either -1 or 1."));
        }

        if (amount <= 0)
        {
            errors.Add(Error.Validation("VisitOperations.InvalidAdjustmentAmount", "Adjustment amount must be greater than zero."));
        }

        var normalizedReasonCode = NormalizeRequiredCode(reasonCode, "Adjustment reason code is required.");
        if (normalizedReasonCode.IsError)
        {
            errors.AddRange(normalizedReasonCode.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new VisitPriceAdjustment
        {
            Id = id,
            VisitId = visitId,
            TargetItemId = targetItemId,
            Sign = sign,
            Amount = amount,
            ReasonCode = normalizedReasonCode.Value,
            Note = NormalizeOptional(note),
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt.ToUniversalTime()
        };
    }

    private static ErrorOr<string> NormalizeRequiredCode(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            return Error.Validation("VisitOperations.RequiredCodeMissing", message);
        }

        return normalized.ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

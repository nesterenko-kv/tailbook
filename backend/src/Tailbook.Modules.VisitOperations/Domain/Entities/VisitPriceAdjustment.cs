namespace Tailbook.Modules.VisitOperations.Domain.Entities;

public sealed class VisitPriceAdjustment
{
    private VisitPriceAdjustment()
    {
    }

    public Guid Id { get; private set; }
    public Guid VisitId { get; private set; }
    public int Sign { get; private set; }
    public decimal Amount { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    internal static VisitPriceAdjustment Create(
        Guid id,
        Guid visitId,
        int sign,
        decimal amount,
        string? reasonCode,
        string? note,
        Guid? createdByUserId,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Visit price adjustment id is required.");
        }

        if (visitId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit price adjustment must belong to a visit.");
        }

        if (sign is not -1 and not 1)
        {
            throw new InvalidOperationException("Adjustment sign must be either -1 or 1.");
        }

        if (amount < 0)
        {
            throw new InvalidOperationException("Adjustment amount must be greater than zero.");
        }

        return new VisitPriceAdjustment
        {
            Id = id,
            VisitId = visitId,
            Sign = sign,
            Amount = amount,
            ReasonCode = NormalizeRequiredCode(reasonCode, "Adjustment reason code is required."),
            Note = NormalizeOptional(note),
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }

    private static string NormalizeRequiredCode(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            throw new InvalidOperationException(message);
        }

        return normalized.ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

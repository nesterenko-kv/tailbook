using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class OfferVersionComponent
{
    public Guid Id { get; set; }
    public Guid OfferVersionId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ComponentRole { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool DefaultExpected { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    internal static ErrorOr<OfferVersionComponent> Create(
        Guid id,
        Guid offerVersionId,
        Guid procedureId,
        string componentRole,
        int sequenceNo,
        bool defaultExpected,
        DateTimeOffset utcNow)
    {
        var normalizedRole = NormalizeRole(componentRole);
        if (normalizedRole.IsError)
        {
            return normalizedRole.Errors;
        }

        return new OfferVersionComponent
        {
            Id = id,
            OfferVersionId = offerVersionId,
            ProcedureId = procedureId,
            ComponentRole = normalizedRole.Value,
            SequenceNo = sequenceNo,
            DefaultExpected = defaultExpected,
            CreatedAt = utcNow.ToUniversalTime()
        };
    }

    public static ErrorOr<string> NormalizeRole(string componentRole)
    {
        var normalized = componentRole.Trim();
        var match = OfferComponentRoleCodes.All.SingleOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match is null ? CatalogErrors.UnknownComponentRole(componentRole) : match;
    }
}

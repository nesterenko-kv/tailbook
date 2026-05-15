using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Entities;

public sealed class OfferVersion
{
    private readonly List<OfferVersionComponent> _components = [];

    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public int VersionNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public string? PolicyText { get; set; }
    public string? ChangeNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public IReadOnlyCollection<OfferVersionComponent> Components => _components.AsReadOnly();
    public bool IsDraft => string.Equals(Status, OfferVersionStatusCodes.Draft, StringComparison.OrdinalIgnoreCase);

    internal static OfferVersion CreateDraft(
        Guid id,
        Guid offerId,
        int versionNo,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        string? policyText,
        string? changeNote,
        DateTimeOffset utcNow)
    {
        return new OfferVersion
        {
            Id = id,
            OfferId = offerId,
            VersionNo = versionNo,
            Status = OfferVersionStatusCodes.Draft,
            ValidFrom = validFrom.ToUniversalTime(),
            ValidTo = validTo?.ToUniversalTime(),
            PolicyText = policyText,
            ChangeNote = changeNote,
            CreatedAt = utcNow.ToUniversalTime(),
            PublishedAt = null
        };
    }

    public ErrorOr<OfferVersionComponent> AddComponent(
        Guid id,
        Guid procedureId,
        string componentRole,
        int sequenceNo,
        bool defaultExpected,
        DateTimeOffset utcNow)
    {
        if (!string.Equals(Status, OfferVersionStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.OfferVersionImmutable;
        }

        if (_components.Any(x => x.SequenceNo == sequenceNo))
        {
            return CatalogErrors.ComponentSequenceExists;
        }

        if (_components.Any(x => x.ProcedureId == procedureId))
        {
            return CatalogErrors.ComponentProcedureExists;
        }

        var component = OfferVersionComponent.Create(id, Id, procedureId, componentRole, sequenceNo, defaultExpected, utcNow);
        if (component.IsError)
        {
            return component.Errors;
        }

        _components.Add(component.Value);
        return component.Value;
    }

    internal ErrorOr<Success> Publish(bool requiresComponents, DateTimeOffset utcNow)
    {
        if (!string.Equals(Status, OfferVersionStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.OfferVersionNotDraft;
        }

        if (requiresComponents && _components.Count == 0)
        {
            return CatalogErrors.PackageOfferVersionEmpty;
        }

        Status = OfferVersionStatusCodes.Published;
        PublishedAt = utcNow.ToUniversalTime();
        return Result.Success;
    }
}

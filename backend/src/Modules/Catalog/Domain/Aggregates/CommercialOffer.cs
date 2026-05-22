using ErrorOr;

namespace Tailbook.Modules.Catalog.Domain.Aggregates;

public sealed class CommercialOffer
{
    private readonly List<OfferVersion> _versions = [];

    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public IReadOnlyCollection<OfferVersion> Versions => _versions.AsReadOnly();

    public static ErrorOr<CommercialOffer> Create(Guid id, string code, string offerType, string displayName, DateTimeOffset utcNow)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode.IsError)
        {
            return normalizedCode.Errors;
        }

        var normalizedOfferType = NormalizeOfferType(offerType);
        if (normalizedOfferType.IsError)
        {
            return normalizedOfferType.Errors;
        }

        var now = utcNow.ToUniversalTime();
        return new CommercialOffer
        {
            Id = id,
            Code = normalizedCode.Value,
            OfferType = normalizedOfferType.Value,
            DisplayName = displayName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public ErrorOr<OfferVersion> CreateVersion(
        Guid versionId,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo,
        string? policyText,
        string? changeNote,
        DateTimeOffset utcNow)
    {
        var nextVersionNo = _versions.Count == 0 ? 1 : _versions.Max(x => x.VersionNo) + 1;
        var version = OfferVersion.CreateDraft(
            versionId,
            Id,
            nextVersionNo,
            validFrom ?? utcNow,
            validTo,
            NormalizeOptional(policyText),
            NormalizeOptional(changeNote),
            utcNow);

        _versions.Add(version);
        return version;
    }

    public ErrorOr<OfferVersionComponent> AddComponent(
        Guid versionId,
        Guid procedureId,
        string componentRole,
        int sequenceNo,
        bool defaultExpected,
        DateTimeOffset utcNow)
    {
        var version = _versions.SingleOrDefault(x => x.Id == versionId);
        if (version is null)
        {
            return CatalogErrors.OfferVersionNotFound;
        }

        if (!string.Equals(OfferType, OfferTypeCodes.Package, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.OfferVersionNotPackage;
        }

        return version.AddComponent(
            Guid.NewGuid(),
            procedureId,
            componentRole,
            sequenceNo,
            defaultExpected,
            utcNow);
    }

    public ErrorOr<Success> EnsureVersionCanAcceptComponent(Guid versionId)
    {
        var version = _versions.SingleOrDefault(x => x.Id == versionId);
        if (version is null)
        {
            return CatalogErrors.OfferVersionNotFound;
        }

        if (!version.IsDraft)
        {
            return CatalogErrors.OfferVersionImmutable;
        }

        if (!string.Equals(OfferType, OfferTypeCodes.Package, StringComparison.OrdinalIgnoreCase))
        {
            return CatalogErrors.OfferVersionNotPackage;
        }

        return Result.Success;
    }

    public ErrorOr<Success> PublishVersion(Guid versionId, DateTimeOffset utcNow)
    {
        var version = _versions.SingleOrDefault(x => x.Id == versionId);
        if (version is null)
        {
            return CatalogErrors.OfferVersionNotFound;
        }

        return version.Publish(
            string.Equals(OfferType, OfferTypeCodes.Package, StringComparison.OrdinalIgnoreCase),
            utcNow);
    }

    public static ErrorOr<string> NormalizeCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant().Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return CatalogErrors.CodeRequired;
        }

        return normalized;
    }

    private static ErrorOr<string> NormalizeOfferType(string offerType)
    {
        var normalized = offerType.Trim();
        var match = OfferTypeCodes.All.SingleOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match is null ? CatalogErrors.UnknownOfferType(offerType) : match;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

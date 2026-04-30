using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Contracts;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogQueries(AppDbContext dbContext) : ICatalogQueries
{
    public async Task<IReadOnlyCollection<ProcedureView>> ListProceduresAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<ProcedureCatalogItem>()
            .OrderBy(x => x.Name)
            .Select(x => new ProcedureView(x.Id, x.Code, x.Name, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<ProcedureView>> CreateProcedureAsync(string code, string name, CancellationToken cancellationToken)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode.IsError)
        {
            return normalizedCode.Errors;
        }

        var displayName = name.Trim();

        var duplicate = await dbContext.Set<ProcedureCatalogItem>()
            .AnyAsync(x => x.Code == normalizedCode.Value, cancellationToken);
        if (duplicate)
        {
            return Error.Conflict("Catalog.ProcedureCodeExists", $"A procedure with code '{normalizedCode.Value}' already exists.");
        }

        var utcNow = DateTime.UtcNow;
        var entity = new ProcedureCatalogItem
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode.Value,
            Name = displayName,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<ProcedureCatalogItem>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProcedureView(entity.Id, entity.Code, entity.Name, entity.IsActive, entity.CreatedAtUtc, entity.UpdatedAtUtc);
    }

    public async Task<IReadOnlyCollection<OfferListItemView>> ListOffersAsync(CancellationToken cancellationToken)
    {
        var offers = await dbContext.Set<CommercialOffer>()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var offerIds = offers.Select(x => x.Id).ToArray();
        var versions = await dbContext.Set<OfferVersion>()
            .Where(x => offerIds.Contains(x.OfferId))
            .ToListAsync(cancellationToken);

        return offers.Select(x => new OfferListItemView(
            x.Id,
            x.Code,
            x.OfferType,
            x.DisplayName,
            x.IsActive,
            versions.Count(v => v.OfferId == x.Id),
            versions.Any(v => v.OfferId == x.Id && v.Status == OfferVersionStatusCodes.Published),
            x.CreatedAtUtc,
            x.UpdatedAtUtc)).ToArray();
    }

    public async Task<OfferDetailView?> GetOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == offerId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var versions = await dbContext.Set<OfferVersion>()
            .Where(x => x.OfferId == offerId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

        var versionIds = versions.Select(x => x.Id).ToArray();
        var components = await dbContext.Set<OfferVersionComponent>()
            .Where(x => versionIds.Contains(x.OfferVersionId))
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(cancellationToken);

        var procedureIds = components.Select(x => x.ProcedureId).Distinct().ToArray();
        var procedures = await dbContext.Set<ProcedureCatalogItem>()
            .Where(x => procedureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new OfferDetailView(
            offer.Id,
            offer.Code,
            offer.OfferType,
            offer.DisplayName,
            offer.IsActive,
            versions.Select(version => new OfferVersionView(
                version.Id,
                version.OfferId,
                version.VersionNo,
                version.Status,
                version.ValidFromUtc,
                version.ValidToUtc,
                version.PolicyText,
                version.ChangeNote,
                version.CreatedAtUtc,
                version.PublishedAtUtc,
                components.Where(component => component.OfferVersionId == version.Id)
                    .Select(component =>
                    {
                        var procedure = procedures[component.ProcedureId];
                        return new OfferVersionComponentView(
                            component.Id,
                            component.OfferVersionId,
                            component.ProcedureId,
                            procedure.Code,
                            procedure.Name,
                            component.ComponentRole,
                            component.SequenceNo,
                            component.DefaultExpected,
                            component.CreatedAtUtc);
                    })
                    .ToArray()))
                .ToArray(),
            offer.CreatedAtUtc,
            offer.UpdatedAtUtc);
    }

    public async Task<ErrorOr<OfferDetailView>> CreateOfferAsync(string code, string offerType, string displayName, CancellationToken cancellationToken)
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

        var normalizedDisplayName = displayName.Trim();

        var duplicate = await dbContext.Set<CommercialOffer>()
            .AnyAsync(x => x.Code == normalizedCode.Value, cancellationToken);
        if (duplicate)
        {
            return Error.Conflict("Catalog.OfferCodeExists", $"An offer with code '{normalizedCode.Value}' already exists.");
        }

        var utcNow = DateTime.UtcNow;
        var offer = new CommercialOffer
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode.Value,
            OfferType = normalizedOfferType.Value,
            DisplayName = normalizedDisplayName,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<CommercialOffer>().Add(offer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetOfferAsync(offer.Id, cancellationToken))!;
    }

    public async Task<OfferVersionView?> CreateOfferVersionAsync(Guid offerId, DateTime? validFromUtc, DateTime? validToUtc, string? policyText, string? changeNote, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == offerId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var nextVersionNo = (await dbContext.Set<OfferVersion>()
            .Where(x => x.OfferId == offerId)
            .MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;

        var utcNow = DateTime.UtcNow;
        var version = new OfferVersion
        {
            Id = Guid.NewGuid(),
            OfferId = offerId,
            VersionNo = nextVersionNo,
            Status = OfferVersionStatusCodes.Draft,
            ValidFromUtc = validFromUtc ?? utcNow,
            ValidToUtc = validToUtc,
            PolicyText = NormalizeOptional(policyText),
            ChangeNote = NormalizeOptional(changeNote),
            CreatedAtUtc = utcNow,
            PublishedAtUtc = null
        };

        dbContext.Set<OfferVersion>().Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OfferVersionView(version.Id, version.OfferId, version.VersionNo, version.Status, version.ValidFromUtc, version.ValidToUtc, version.PolicyText, version.ChangeNote, version.CreatedAtUtc, version.PublishedAtUtc, []);
    }

    public async Task<ErrorOr<OfferVersionComponentView>> AddComponentAsync(Guid versionId, Guid procedureId, string componentRole, int sequenceNo, bool defaultExpected, CancellationToken cancellationToken)
    {
        var version = await dbContext.Set<OfferVersion>().SingleOrDefaultAsync(x => x.Id == versionId, cancellationToken);
        if (version is null)
        {
            return Error.NotFound("Catalog.OfferVersionNotFound", "Offer version does not exist.");
        }

        if (!string.Equals(version.Status, OfferVersionStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict("Catalog.OfferVersionImmutable", "Published or archived offer versions are immutable.");
        }

        var offer = await dbContext.Set<CommercialOffer>().SingleAsync(x => x.Id == version.OfferId, cancellationToken);
        if (!string.Equals(offer.OfferType, OfferTypeCodes.Package, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Catalog.OfferVersionNotPackage", "Only package offer versions can have operational components.");
        }

        var normalizedRole = NormalizeComponentRole(componentRole);
        if (normalizedRole.IsError)
        {
            return normalizedRole.Errors;
        }

        var procedure = await dbContext.Set<ProcedureCatalogItem>().SingleOrDefaultAsync(x => x.Id == procedureId, cancellationToken);
        if (procedure is null)
        {
            return Error.NotFound("Catalog.ProcedureNotFound", "Procedure does not exist.");
        }

        var duplicateSequence = await dbContext.Set<OfferVersionComponent>()
            .AnyAsync(x => x.OfferVersionId == versionId && x.SequenceNo == sequenceNo, cancellationToken);
        if (duplicateSequence)
        {
            return Error.Conflict("Catalog.ComponentSequenceExists", "A component with the same sequence number already exists in this version.");
        }

        var duplicateProcedure = await dbContext.Set<OfferVersionComponent>()
            .AnyAsync(x => x.OfferVersionId == versionId && x.ProcedureId == procedureId, cancellationToken);
        if (duplicateProcedure)
        {
            return Error.Conflict("Catalog.ComponentProcedureExists", "The same procedure cannot be added twice to one offer version.");
        }

        var entity = new OfferVersionComponent
        {
            Id = Guid.NewGuid(),
            OfferVersionId = versionId,
            ProcedureId = procedureId,
            ComponentRole = normalizedRole.Value,
            SequenceNo = sequenceNo,
            DefaultExpected = defaultExpected,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Set<OfferVersionComponent>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OfferVersionComponentView(entity.Id, entity.OfferVersionId, entity.ProcedureId, procedure.Code, procedure.Name, entity.ComponentRole, entity.SequenceNo, entity.DefaultExpected, entity.CreatedAtUtc);
    }

    public async Task<ErrorOr<OfferVersionView>> PublishOfferVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var version = await dbContext.Set<OfferVersion>().SingleOrDefaultAsync(x => x.Id == versionId, cancellationToken);
        if (version is null)
        {
            return Error.NotFound("Catalog.OfferVersionNotFound", "Offer version does not exist.");
        }

        if (!string.Equals(version.Status, OfferVersionStatusCodes.Draft, StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict("Catalog.OfferVersionNotDraft", "Only draft offer versions can be published.");
        }

        var offer = await dbContext.Set<CommercialOffer>().SingleAsync(x => x.Id == version.OfferId, cancellationToken);
        if (string.Equals(offer.OfferType, OfferTypeCodes.Package, StringComparison.OrdinalIgnoreCase))
        {
            var hasComponents = await dbContext.Set<OfferVersionComponent>().AnyAsync(x => x.OfferVersionId == versionId, cancellationToken);
            if (!hasComponents)
            {
                return Error.Validation("Catalog.PackageOfferVersionEmpty", "Package offer versions must contain at least one component before publication.");
            }
        }

        version.Status = OfferVersionStatusCodes.Published;
        version.PublishedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var components = await dbContext.Set<OfferVersionComponent>()
            .Where(x => x.OfferVersionId == versionId)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(cancellationToken);
        var procedureIds = components.Select(x => x.ProcedureId).Distinct().ToArray();
        var procedures = await dbContext.Set<ProcedureCatalogItem>()
            .Where(x => procedureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new OfferVersionView(
            version.Id,
            version.OfferId,
            version.VersionNo,
            version.Status,
            version.ValidFromUtc,
            version.ValidToUtc,
            version.PolicyText,
            version.ChangeNote,
            version.CreatedAtUtc,
            version.PublishedAtUtc,
            components.Select(x => new OfferVersionComponentView(x.Id, x.OfferVersionId, x.ProcedureId, procedures[x.ProcedureId].Code, procedures[x.ProcedureId].Name, x.ComponentRole, x.SequenceNo, x.DefaultExpected, x.CreatedAtUtc)).ToArray());
    }

    private static ErrorOr<string> NormalizeCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant().Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Error.Validation("Catalog.CodeRequired", "Code is required.");
        }

        return normalized;
    }

    private static ErrorOr<string> NormalizeOfferType(string offerType)
    {
        var normalized = offerType.Trim();
        var match = OfferTypeCodes.All.SingleOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? Error.Validation("Catalog.UnknownOfferType", $"Unknown offer type '{offerType}'.")
            : match;
    }

    private static ErrorOr<string> NormalizeComponentRole(string componentRole)
    {
        var normalized = componentRole.Trim();
        var match = OfferComponentRoleCodes.All.SingleOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? Error.Validation("Catalog.UnknownComponentRole", $"Unknown component role '{componentRole}'.")
            : match;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

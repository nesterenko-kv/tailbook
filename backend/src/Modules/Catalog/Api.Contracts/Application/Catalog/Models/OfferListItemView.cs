namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record OfferListItemView(Guid Id, string Code, string OfferType, string DisplayName, bool IsActive, int VersionCount, bool HasPublishedVersion, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
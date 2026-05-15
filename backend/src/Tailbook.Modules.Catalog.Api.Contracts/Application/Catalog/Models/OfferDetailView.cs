namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record OfferDetailView(Guid Id, string Code, string OfferType, string DisplayName, bool IsActive, IReadOnlyCollection<OfferVersionView> Versions, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
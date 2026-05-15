namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record OfferVersionView(Guid Id, Guid OfferId, int VersionNo, string Status, DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, string? PolicyText, string? ChangeNote, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, IReadOnlyCollection<OfferVersionComponentView> Components);
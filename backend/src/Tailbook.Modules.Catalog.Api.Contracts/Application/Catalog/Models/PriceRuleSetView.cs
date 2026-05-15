namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record PriceRuleSetView(Guid Id, int VersionNo, string Status, DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, IReadOnlyCollection<PriceRuleView> Rules);
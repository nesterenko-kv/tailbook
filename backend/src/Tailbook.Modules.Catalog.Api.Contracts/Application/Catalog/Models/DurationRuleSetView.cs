namespace Tailbook.Modules.Catalog.Application.Catalog.Models;

public sealed record DurationRuleSetView(Guid Id, int VersionNo, string Status, DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, IReadOnlyCollection<DurationRuleView> Rules);
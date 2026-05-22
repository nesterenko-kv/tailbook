using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogDurationRuleSetCommand(DateTimeOffset? ValidFrom, DateTimeOffset? ValidTo) : ICommand<DurationRuleSetView>;
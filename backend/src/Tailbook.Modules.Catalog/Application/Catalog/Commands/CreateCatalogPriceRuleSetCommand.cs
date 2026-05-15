using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogPriceRuleSetCommand(DateTimeOffset? ValidFrom, DateTimeOffset? ValidTo) : ICommand<PriceRuleSetView>;
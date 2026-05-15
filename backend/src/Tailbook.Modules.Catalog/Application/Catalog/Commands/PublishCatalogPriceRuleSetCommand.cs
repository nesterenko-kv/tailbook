using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record PublishCatalogPriceRuleSetCommand(Guid RuleSetId) : ICommand<ErrorOr<PriceRuleSetView>>;
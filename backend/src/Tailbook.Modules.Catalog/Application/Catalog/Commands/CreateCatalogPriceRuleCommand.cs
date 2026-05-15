using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogPriceRuleCommand(CreatePriceRuleContextData Rule) : ICommand<ErrorOr<PriceRuleView>>;
using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record PublishCatalogDurationRuleSetCommand(Guid RuleSetId) : ICommand<ErrorOr<DurationRuleSetView>>;
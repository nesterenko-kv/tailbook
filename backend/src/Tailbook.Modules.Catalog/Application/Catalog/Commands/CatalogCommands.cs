using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogProcedureCommand(string Code, string Name) : ICommand<ErrorOr<ProcedureView>>;

public sealed record CreateCatalogOfferCommand(string Code, string OfferType, string DisplayName) : ICommand<ErrorOr<OfferDetailView>>;

public sealed record CreateCatalogOfferVersionCommand(
    Guid OfferId,
    DateTime? ValidFromUtc,
    DateTime? ValidToUtc,
    string? PolicyText,
    string? ChangeNote) : ICommand<OfferVersionView?>;

public sealed record AddCatalogOfferVersionComponentCommand(
    Guid VersionId,
    Guid ProcedureId,
    string ComponentRole,
    int SequenceNo,
    bool DefaultExpected) : ICommand<ErrorOr<OfferVersionComponentView>>;

public sealed record PublishCatalogOfferVersionCommand(Guid VersionId) : ICommand<ErrorOr<OfferVersionView>>;

public sealed record CreateCatalogPriceRuleSetCommand(DateTime? ValidFromUtc, DateTime? ValidToUtc) : ICommand<PriceRuleSetView>;

public sealed record CreateCatalogPriceRuleCommand(CreatePriceRuleCommand Rule) : ICommand<ErrorOr<PriceRuleView>>;

public sealed record PublishCatalogPriceRuleSetCommand(Guid RuleSetId) : ICommand<ErrorOr<PriceRuleSetView>>;

public sealed record CreateCatalogDurationRuleSetCommand(DateTime? ValidFromUtc, DateTime? ValidToUtc) : ICommand<DurationRuleSetView>;

public sealed record CreateCatalogDurationRuleCommand(CreateDurationRuleCommand Rule) : ICommand<ErrorOr<DurationRuleView>>;

public sealed record PublishCatalogDurationRuleSetCommand(Guid RuleSetId) : ICommand<ErrorOr<DurationRuleSetView>>;

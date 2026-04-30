using ErrorOr;
using FastEndpoints;
using Tailbook.Modules.Catalog.Application.Catalog.Commands;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogCommandHandlers(
    CatalogUseCases catalogUseCases,
    CatalogPricingUseCases catalogPricingUseCases)
    : ICommandHandler<CreateCatalogProcedureCommand, ErrorOr<ProcedureView>>,
      ICommandHandler<CreateCatalogOfferCommand, ErrorOr<OfferDetailView>>,
      ICommandHandler<CreateCatalogOfferVersionCommand, OfferVersionView?>,
      ICommandHandler<AddCatalogOfferVersionComponentCommand, ErrorOr<OfferVersionComponentView>>,
      ICommandHandler<PublishCatalogOfferVersionCommand, ErrorOr<OfferVersionView>>,
      ICommandHandler<CreateCatalogPriceRuleSetCommand, PriceRuleSetView>,
      ICommandHandler<CreateCatalogPriceRuleCommand, ErrorOr<PriceRuleView>>,
      ICommandHandler<PublishCatalogPriceRuleSetCommand, ErrorOr<PriceRuleSetView>>,
      ICommandHandler<CreateCatalogDurationRuleSetCommand, DurationRuleSetView>,
      ICommandHandler<CreateCatalogDurationRuleCommand, ErrorOr<DurationRuleView>>,
      ICommandHandler<PublishCatalogDurationRuleSetCommand, ErrorOr<DurationRuleSetView>>
{
    public Task<ErrorOr<ProcedureView>> ExecuteAsync(CreateCatalogProcedureCommand command, CancellationToken cancellationToken)
        => catalogUseCases.CreateProcedureAsync(command.Code, command.Name, cancellationToken);

    public Task<ErrorOr<OfferDetailView>> ExecuteAsync(CreateCatalogOfferCommand command, CancellationToken cancellationToken)
        => catalogUseCases.CreateOfferAsync(command.Code, command.OfferType, command.DisplayName, cancellationToken);

    public Task<OfferVersionView?> ExecuteAsync(CreateCatalogOfferVersionCommand command, CancellationToken cancellationToken)
        => catalogUseCases.CreateOfferVersionAsync(command.OfferId, command.ValidFromUtc, command.ValidToUtc, command.PolicyText, command.ChangeNote, cancellationToken);

    public Task<ErrorOr<OfferVersionComponentView>> ExecuteAsync(AddCatalogOfferVersionComponentCommand command, CancellationToken cancellationToken)
        => catalogUseCases.AddComponentAsync(command.VersionId, command.ProcedureId, command.ComponentRole, command.SequenceNo, command.DefaultExpected, cancellationToken);

    public Task<ErrorOr<OfferVersionView>> ExecuteAsync(PublishCatalogOfferVersionCommand command, CancellationToken cancellationToken)
        => catalogUseCases.PublishOfferVersionAsync(command.VersionId, cancellationToken);

    public Task<PriceRuleSetView> ExecuteAsync(CreateCatalogPriceRuleSetCommand command, CancellationToken cancellationToken)
        => catalogPricingUseCases.CreatePriceRuleSetAsync(command.ValidFromUtc, command.ValidToUtc, cancellationToken);

    public Task<ErrorOr<PriceRuleView>> ExecuteAsync(CreateCatalogPriceRuleCommand command, CancellationToken cancellationToken)
        => catalogPricingUseCases.CreatePriceRuleAsync(command.Rule, cancellationToken);

    public Task<ErrorOr<PriceRuleSetView>> ExecuteAsync(PublishCatalogPriceRuleSetCommand command, CancellationToken cancellationToken)
        => catalogPricingUseCases.PublishPriceRuleSetAsync(command.RuleSetId, cancellationToken);

    public Task<DurationRuleSetView> ExecuteAsync(CreateCatalogDurationRuleSetCommand command, CancellationToken cancellationToken)
        => catalogPricingUseCases.CreateDurationRuleSetAsync(command.ValidFromUtc, command.ValidToUtc, cancellationToken);

    public Task<ErrorOr<DurationRuleView>> ExecuteAsync(CreateCatalogDurationRuleCommand command, CancellationToken cancellationToken)
        => catalogPricingUseCases.CreateDurationRuleAsync(command.Rule, cancellationToken);

    public Task<ErrorOr<DurationRuleSetView>> ExecuteAsync(PublishCatalogDurationRuleSetCommand command, CancellationToken cancellationToken)
        => catalogPricingUseCases.PublishDurationRuleSetAsync(command.RuleSetId, cancellationToken);
}

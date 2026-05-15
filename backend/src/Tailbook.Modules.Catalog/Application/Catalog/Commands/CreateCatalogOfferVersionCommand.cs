using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogOfferVersionCommand(
    Guid OfferId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    string? PolicyText,
    string? ChangeNote) : ICommand<OfferVersionView?>;
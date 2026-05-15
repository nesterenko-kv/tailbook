using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record PublishCatalogOfferVersionCommand(Guid VersionId) : ICommand<ErrorOr<OfferVersionView>>;
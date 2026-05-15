using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogOfferCommand(string Code, string OfferType, string DisplayName) : ICommand<ErrorOr<OfferDetailView>>;
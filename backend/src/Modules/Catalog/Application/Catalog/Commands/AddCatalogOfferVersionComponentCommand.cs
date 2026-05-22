using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record AddCatalogOfferVersionComponentCommand(
    Guid VersionId,
    Guid ProcedureId,
    string ComponentRole,
    int SequenceNo,
    bool DefaultExpected) : ICommand<ErrorOr<OfferVersionComponentView>>;
using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Catalog.Application.Catalog.Commands;

public sealed record CreateCatalogProcedureCommand(string Code, string Name) : ICommand<ErrorOr<ProcedureView>>;
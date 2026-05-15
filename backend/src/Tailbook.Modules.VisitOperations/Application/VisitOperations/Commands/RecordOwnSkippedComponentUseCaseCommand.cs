using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record RecordOwnSkippedComponentUseCaseCommand(
    Guid CurrentUserId,
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid OfferVersionComponentId,
    string OmissionReasonCode,
    string? Note) : ICommand<ErrorOr<GroomerVisitDetailView>>;
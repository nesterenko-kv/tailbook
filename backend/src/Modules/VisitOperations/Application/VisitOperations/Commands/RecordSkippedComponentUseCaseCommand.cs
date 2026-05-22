using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record RecordSkippedComponentUseCaseCommand(
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid OfferVersionComponentId,
    string OmissionReasonCode,
    string? Note,
    Guid ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

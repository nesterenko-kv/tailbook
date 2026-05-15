using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record ApplyVisitPriceAdjustmentUseCaseCommand(
    Guid VisitId,
    int Sign,
    decimal Amount,
    string ReasonCode,
    string? Note,
    Guid? TargetItemId,
    Guid ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

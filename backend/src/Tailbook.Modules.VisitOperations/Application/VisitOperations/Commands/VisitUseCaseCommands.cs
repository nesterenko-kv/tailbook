using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record CheckInAppointmentUseCaseCommand(
    Guid AppointmentId,
    Guid? ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

public sealed record RecordPerformedProcedureUseCaseCommand(
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid ProcedureId,
    string? Note,
    Guid? ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

public sealed record RecordSkippedComponentUseCaseCommand(
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid OfferVersionComponentId,
    string OmissionReasonCode,
    string? Note,
    Guid? ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

public sealed record ApplyVisitPriceAdjustmentUseCaseCommand(
    Guid VisitId,
    int Sign,
    decimal Amount,
    string ReasonCode,
    string? Note,
    Guid? ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

public sealed record CompleteVisitUseCaseCommand(
    Guid VisitId,
    Guid? ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

public sealed record CloseVisitUseCaseCommand(
    Guid VisitId,
    Guid? ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;

public sealed record CheckInOwnAppointmentUseCaseCommand(
    Guid CurrentUserId,
    Guid AppointmentId) : ICommand<ErrorOr<GroomerVisitDetailView>>;

public sealed record RecordOwnPerformedProcedureUseCaseCommand(
    Guid CurrentUserId,
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid ProcedureId,
    string? Note) : ICommand<ErrorOr<GroomerVisitDetailView>>;

public sealed record RecordOwnSkippedComponentUseCaseCommand(
    Guid CurrentUserId,
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid OfferVersionComponentId,
    string OmissionReasonCode,
    string? Note) : ICommand<ErrorOr<GroomerVisitDetailView>>;

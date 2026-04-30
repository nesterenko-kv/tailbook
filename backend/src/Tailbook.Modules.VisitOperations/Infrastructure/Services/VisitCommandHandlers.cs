using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services;

public sealed class VisitCommandHandlers(
    VisitUseCases visitUseCases,
    GroomerVisitUseCases groomerVisitUseCases)
    : ICommandHandler<CheckInAppointmentUseCaseCommand, ErrorOr<VisitDetailView>>,
        ICommandHandler<RecordPerformedProcedureUseCaseCommand, ErrorOr<VisitDetailView>>,
        ICommandHandler<RecordSkippedComponentUseCaseCommand, ErrorOr<VisitDetailView>>,
        ICommandHandler<ApplyVisitPriceAdjustmentUseCaseCommand, ErrorOr<VisitDetailView>>,
        ICommandHandler<CompleteVisitUseCaseCommand, ErrorOr<VisitDetailView>>,
        ICommandHandler<CloseVisitUseCaseCommand, ErrorOr<VisitDetailView>>,
        ICommandHandler<CheckInOwnAppointmentUseCaseCommand, ErrorOr<GroomerVisitDetailView>>,
        ICommandHandler<RecordOwnPerformedProcedureUseCaseCommand, ErrorOr<GroomerVisitDetailView>>,
        ICommandHandler<RecordOwnSkippedComponentUseCaseCommand, ErrorOr<GroomerVisitDetailView>>
{
    public Task<ErrorOr<VisitDetailView>> ExecuteAsync(CheckInAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return visitUseCases.CheckInAppointmentAsync(command.AppointmentId, command.ActorUserId, ct);
    }

    public Task<ErrorOr<VisitDetailView>> ExecuteAsync(RecordPerformedProcedureUseCaseCommand command, CancellationToken ct = default)
    {
        return visitUseCases.RecordPerformedProcedureAsync(command.VisitId, command.VisitExecutionItemId, command.ProcedureId, command.Note, command.ActorUserId, ct);
    }

    public Task<ErrorOr<VisitDetailView>> ExecuteAsync(RecordSkippedComponentUseCaseCommand command, CancellationToken ct = default)
    {
        return visitUseCases.RecordSkippedComponentAsync(command.VisitId, command.VisitExecutionItemId, command.OfferVersionComponentId, command.OmissionReasonCode, command.Note, command.ActorUserId, ct);
    }

    public Task<ErrorOr<VisitDetailView>> ExecuteAsync(ApplyVisitPriceAdjustmentUseCaseCommand command, CancellationToken ct = default)
    {
        return visitUseCases.ApplyPriceAdjustmentAsync(command.VisitId, command.Sign, command.Amount, command.ReasonCode, command.Note, command.ActorUserId, ct);
    }

    public Task<ErrorOr<VisitDetailView>> ExecuteAsync(CompleteVisitUseCaseCommand command, CancellationToken ct = default)
    {
        return visitUseCases.CompleteVisitAsync(command.VisitId, command.ActorUserId, ct);
    }

    public Task<ErrorOr<VisitDetailView>> ExecuteAsync(CloseVisitUseCaseCommand command, CancellationToken ct = default)
    {
        return visitUseCases.CloseVisitAsync(command.VisitId, command.ActorUserId, ct);
    }

    public Task<ErrorOr<GroomerVisitDetailView>> ExecuteAsync(CheckInOwnAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return groomerVisitUseCases.CheckInAppointmentAsync(command.CurrentUserId, command.AppointmentId, ct);
    }

    public Task<ErrorOr<GroomerVisitDetailView>> ExecuteAsync(RecordOwnPerformedProcedureUseCaseCommand command, CancellationToken ct = default)
    {
        return groomerVisitUseCases.RecordPerformedProcedureAsync(command.CurrentUserId, command.VisitId, command.VisitExecutionItemId, command.ProcedureId, command.Note, ct);
    }

    public Task<ErrorOr<GroomerVisitDetailView>> ExecuteAsync(RecordOwnSkippedComponentUseCaseCommand command, CancellationToken ct = default)
    {
        return groomerVisitUseCases.RecordSkippedComponentAsync(command.CurrentUserId, command.VisitId, command.VisitExecutionItemId, command.OfferVersionComponentId, command.OmissionReasonCode, command.Note, ct);
    }
}

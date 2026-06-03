using ErrorOr;
using FastEndpoints;
using Tailbook.Modules.Booking.Api.Contracts.Admin.BulkCancelAppointments;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record BulkCancelAppointmentsUseCaseCommand(
    Guid[] AppointmentIds,
    string ReasonCode,
    string? Notes,
    Guid ActorUserId) : ICommand<ErrorOr<BulkCancelAppointmentsResponse>>;

using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public sealed class BookingCommandHandlers(
    BookingManagementUseCases managementUseCases,
    ClientPortalBookingUseCases clientPortalUseCases)
    : ICommandHandler<CreateBookingRequestUseCaseCommand, ErrorOr<BookingRequestDetailView>>,
        ICommandHandler<AttachBookingRequestContextUseCaseCommand, ErrorOr<BookingRequestDetailView>>,
        ICommandHandler<ConvertBookingRequestToAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>,
        ICommandHandler<CreateAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>,
        ICommandHandler<RescheduleAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>,
        ICommandHandler<CancelAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>,
        ICommandHandler<CreateClientBookingRequestUseCaseCommand, ErrorOr<BookingRequestDetailView>>
{
    public Task<ErrorOr<BookingRequestDetailView>> ExecuteAsync(CreateBookingRequestUseCaseCommand command, CancellationToken ct = default)
    {
        return managementUseCases.CreateBookingRequestAsync(command.BookingRequest, command.ActorUserId, ct);
    }

    public Task<ErrorOr<BookingRequestDetailView>> ExecuteAsync(AttachBookingRequestContextUseCaseCommand command, CancellationToken ct = default)
    {
        return managementUseCases.AttachBookingRequestContextAsync(command.Context, command.ActorUserId, ct);
    }

    public Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(ConvertBookingRequestToAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return managementUseCases.ConvertBookingRequestToAppointmentAsync(command.Conversion, command.ActorUserId, ct);
    }

    public Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(CreateAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return managementUseCases.CreateAppointmentAsync(command.Appointment, command.ActorUserId, ct);
    }

    public Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(RescheduleAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return managementUseCases.RescheduleAppointmentAsync(command.Appointment, command.ActorUserId, ct);
    }

    public Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(CancelAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        return managementUseCases.CancelAppointmentAsync(command.Appointment, command.ActorUserId, ct);
    }

    public Task<ErrorOr<BookingRequestDetailView>> ExecuteAsync(CreateClientBookingRequestUseCaseCommand command, CancellationToken ct = default)
    {
        return clientPortalUseCases.CreateMyBookingRequestAsync(command.Actor, command.BookingRequest, ct);
    }
}

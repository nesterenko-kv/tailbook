using ErrorOr;
using FastEndpoints;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateBookingRequestUseCaseCommand(
    CreateBookingRequestCommand BookingRequest,
    string? ActorUserId) : ICommand<ErrorOr<BookingRequestDetailView>>;

public sealed record AttachBookingRequestContextUseCaseCommand(
    AttachBookingRequestContextCommand Context,
    string? ActorUserId) : ICommand<ErrorOr<BookingRequestDetailView>>;

public sealed record ConvertBookingRequestToAppointmentUseCaseCommand(
    ConvertBookingRequestToAppointmentCommand Conversion,
    string? ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;

public sealed record CreateAppointmentUseCaseCommand(
    CreateAppointmentCommand Appointment,
    string? ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;

public sealed record RescheduleAppointmentUseCaseCommand(
    RescheduleAppointmentCommand Appointment,
    string? ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;

public sealed record CancelAppointmentUseCaseCommand(
    CancelAppointmentCommand Appointment,
    string? ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;

public sealed record CreateClientBookingRequestUseCaseCommand(
    ClientPortalActor Actor,
    CreateClientBookingRequestCommand BookingRequest) : ICommand<ErrorOr<BookingRequestDetailView>>;

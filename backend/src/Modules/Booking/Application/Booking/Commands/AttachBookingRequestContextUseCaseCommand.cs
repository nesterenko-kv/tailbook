using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record AttachBookingRequestContextUseCaseCommand(
    AttachBookingRequestContextData Context,
    Guid ActorUserId) : ICommand<ErrorOr<BookingRequestDetailView>>;

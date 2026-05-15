using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateAppointmentUseCaseCommand(
    Guid PetId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    IReadOnlyCollection<CreateAppointmentItemData> Items,
    Guid ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;

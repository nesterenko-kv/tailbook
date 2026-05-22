using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class CreateClientBookingRequestUseCaseCommandHandler(
    CreateBookingRequestUseCaseCommandHandler createBookingRequestHandler)
    : ICommandHandler<CreateClientBookingRequestUseCaseCommand, ErrorOr<BookingRequestDetailView>>
{
    public Task<ErrorOr<BookingRequestDetailView>> ExecuteAsync(CreateClientBookingRequestUseCaseCommand command, CancellationToken ct = default)
    {
        return createBookingRequestHandler.ExecuteAsync(
            new CreateBookingRequestUseCaseCommand(
                command.Actor.ClientId,
                command.PetId,
                command.Actor.ContactPersonId,
                BookingChannelCodes.ClientPortal,
                command.Notes,
                command.PreferredTimes.Select(x => new PreferredTimeWindowInput(x.StartAt, x.EndAt, x.Label))
                    .ToArray(),
                command.Items.Select(x => new CreateBookingRequestItemInput(x.OfferId, x.ItemType, x.RequestedNotes))
                    .ToArray(),
                command.Actor.UserId),
            ct);
    }
}

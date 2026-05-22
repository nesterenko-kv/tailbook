using ErrorOr;
using FastEndpoints;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateClientBookingRequestUseCaseCommand(
    ClientPortalActor Actor,
    Guid PetId,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowInput> PreferredTimes,
    IReadOnlyCollection<CreateClientBookingRequestItemInput> Items) : ICommand<ErrorOr<BookingRequestDetailView>>;

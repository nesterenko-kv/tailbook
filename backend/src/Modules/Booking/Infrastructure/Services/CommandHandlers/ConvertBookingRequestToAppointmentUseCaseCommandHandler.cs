using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class ConvertBookingRequestToAppointmentUseCaseCommandHandler(
    AppDbContext dbContext,
    CreateAppointmentUseCaseCommandHandler createAppointmentHandler,
    IAuditTrailService auditTrailService,
    TimeProvider timeProvider)
    : ICommandHandler<ConvertBookingRequestToAppointmentUseCaseCommand, ErrorOr<AppointmentDetailView>>
{
    public async Task<ErrorOr<AppointmentDetailView>> ExecuteAsync(ConvertBookingRequestToAppointmentUseCaseCommand command, CancellationToken ct = default)
    {
        var bookingRequest = await dbContext.Set<BookingRequest>().SingleOrDefaultAsync(x => x.Id == command.BookingRequestId, ct);
        if (bookingRequest is null)
        {
            return Error.NotFound("Booking.BookingRequestNotFound", "Booking request does not exist.");
        }

        if (bookingRequest.Status == BookingRequestStatusCodes.Converted)
        {
            return Error.Conflict("Booking.BookingRequestAlreadyConverted", "Booking request has already been converted.");
        }

        if (!bookingRequest.PetId.HasValue)
        {
            return Error.Validation("Booking.GuestRequestRequiresSavedPet", "Guest booking requests must be linked to a saved pet before conversion to an appointment.");
        }

        var requestItems = await dbContext.Set<BookingRequestItem>()
            .Where(x => x.BookingRequestId == bookingRequest.Id)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        if (requestItems.Count == 0)
        {
            return Error.Validation("Booking.BookingRequestItemsRequired", "Booking request must contain at least one requested item.");
        }

        var result = await createAppointmentHandler.CreateAppointmentAsync(
            bookingRequest.Id,
            bookingRequest.PetId.Value,
            command.GroomerId,
            command.StartAt,
            requestItems.Select(x => new PreviewQuoteItemQuery(x.OfferId, x.ItemType)).ToArray(),
            command.ActorUserId,
            ct);
        if (result.IsError)
        {
            return result.Errors;
        }

        bookingRequest.MarkConverted(result.Value.Id, timeProvider.GetUtcNow());
        var saveResult = await ConcurrencySafeSaver.SaveAsync(dbContext, ct);
        if (saveResult.IsError)
        {
            return saveResult.Errors;
        }
        await auditTrailService.RecordAsync(
            "booking",
            "booking_request",
            bookingRequest.Id.ToString("D"),
            "CONVERT_TO_APPOINTMENT",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { appointmentId = result.Value.Id }),
            ct);

        return result.Value;
    }
}

using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services.CommandHandlers;

public sealed class AttachBookingRequestContextUseCaseCommandHandler(
    AppDbContext dbContext,
    IBookingManagementReadService bookingReadService,
    IPetQuoteProfileService petQuoteProfileService,
    IClientReferenceValidationService clientReferenceValidationService,
    IContactReferenceValidationService contactReferenceValidationService,
    IAuditTrailService auditTrailService,
    TimeProvider timeProvider)
    : ICommandHandler<AttachBookingRequestContextUseCaseCommand, ErrorOr<BookingRequestDetailView>>
{
    public async Task<ErrorOr<BookingRequestDetailView>> ExecuteAsync(AttachBookingRequestContextUseCaseCommand command, CancellationToken ct = default)
    {
        var data = command.Context;
        var bookingRequest = await dbContext.Set<BookingRequest>()
            .SingleOrDefaultAsync(x => x.Id == data.BookingRequestId, ct);

        if (bookingRequest is null)
        {
            return Error.NotFound("Booking.BookingRequestNotFound", "Booking request does not exist.");
        }

        if (bookingRequest.Status == BookingRequestStatusCodes.Converted)
        {
            return Error.Conflict("Booking.BookingRequestConverted", "Converted booking requests cannot be relinked.");
        }

        var pet = await petQuoteProfileService.GetPetAsync(data.PetId, ct);
        if (pet is null)
        {
            return Error.NotFound("Booking.PetNotFound", "Pet does not exist.");
        }

        if (data.ClientId.HasValue)
        {
            var clientExists = await clientReferenceValidationService.ExistsAsync(data.ClientId.Value, ct);
            if (!clientExists)
            {
                return Error.NotFound("Booking.ClientNotFound", "Client does not exist.");
            }

            if (pet.ClientId.HasValue && pet.ClientId.Value != data.ClientId.Value)
            {
                return Error.Validation("Booking.PetClientMismatch", "Selected pet does not belong to the specified client.");
            }
        }

        if (data.RequestedByContactId.HasValue)
        {
            var contactExists = await contactReferenceValidationService.ExistsAsync(data.RequestedByContactId.Value, ct);
            if (!contactExists)
            {
                return Error.NotFound("Booking.RequestedByContactNotFound", "Requested-by contact does not exist.");
            }
        }

        bookingRequest.ClientId = data.ClientId ?? pet.ClientId;
        bookingRequest.PetId = data.PetId;
        bookingRequest.RequestedByContactId = data.RequestedByContactId;
        bookingRequest.UpdatedAt = timeProvider.GetUtcNow();

        if (bookingRequest.Status == BookingRequestStatusCodes.NeedsReview)
        {
            bookingRequest.Status = BookingRequestStatusCodes.Submitted;
        }

        await dbContext.SaveChangesAsync(ct);
        await auditTrailService.RecordAsync(
            "booking",
            "booking_request",
            bookingRequest.Id.ToString("D"),
            "ATTACH_CONTEXT",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { bookingRequest.ClientId, bookingRequest.PetId, bookingRequest.RequestedByContactId, bookingRequest.Status }),
            ct);

        return (await bookingReadService.GetBookingRequestAsync(bookingRequest.Id, ct))!;
    }
}

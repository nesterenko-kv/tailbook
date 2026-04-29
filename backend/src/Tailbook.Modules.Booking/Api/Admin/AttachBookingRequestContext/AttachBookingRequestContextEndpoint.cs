using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.AttachBookingRequestContext;

public sealed class AttachBookingRequestContextEndpoint(
    ICurrentUser currentUser,
    BookingManagementQueries bookingQueries)
    : Endpoint<AttachBookingRequestContextRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests/{bookingRequestId:guid}/attach-context");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(AttachBookingRequestContextRequest req, CancellationToken ct)
    {
        try
        {
            var bookingRequestId = Route<Guid>("bookingRequestId");
            var result = await bookingQueries.AttachBookingRequestContextAsync(
                new AttachBookingRequestContextCommand(
                    bookingRequestId,
                    req.ClientId,
                    req.PetId,
                    req.RequestedByContactId),
                currentUser.UserId,
                ct);

            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class AttachBookingRequestContextRequest
{
    public Guid? ClientId { get; set; }
    public Guid PetId { get; set; }
    public Guid? RequestedByContactId { get; set; }
}

public sealed class AttachBookingRequestContextRequestValidator : Validator<AttachBookingRequestContextRequest>
{
    public AttachBookingRequestContextRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
    }
}

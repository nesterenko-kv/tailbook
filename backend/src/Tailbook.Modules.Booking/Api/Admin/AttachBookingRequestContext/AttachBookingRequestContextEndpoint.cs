using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.AttachBookingRequestContext;

public sealed class AttachBookingRequestContextEndpoint(BookingManagementQueries bookingQueries)
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
        var result = await bookingQueries.AttachBookingRequestContextAsync(
            new AttachBookingRequestContextCommand(
                req.BookingRequestId,
                req.ClientId,
                req.PetId,
                req.RequestedByContactId),
            req.ActorUserId?.ToString("D"),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class AttachBookingRequestContextRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid BookingRequestId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid PetId { get; set; }
    public Guid? RequestedByContactId { get; set; }
}

public sealed class AttachBookingRequestContextRequestValidator : Validator<AttachBookingRequestContextRequest>
{
    public AttachBookingRequestContextRequestValidator()
    {
        RuleFor(x => x.BookingRequestId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
    }
}

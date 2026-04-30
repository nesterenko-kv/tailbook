using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.ConvertBookingRequestToAppointment;

public sealed class ConvertBookingRequestToAppointmentEndpoint(BookingManagementQueries bookingQueries)
    : Endpoint<ConvertBookingRequestToAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests/{BookingRequestId:guid}/convert");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(ConvertBookingRequestToAppointmentRequest req, CancellationToken ct)
    {
        var result = await bookingQueries.ConvertBookingRequestToAppointmentAsync(
            new ConvertBookingRequestToAppointmentCommand(req.BookingRequestId, req.GroomerId, req.StartAtUtc),
            req.ActorUserId?.ToString("D"),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}

public sealed class ConvertBookingRequestToAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid BookingRequestId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
}

public sealed class ConvertBookingRequestToAppointmentRequestValidator : Validator<ConvertBookingRequestToAppointmentRequest>
{
    public ConvertBookingRequestToAppointmentRequestValidator()
    {
        RuleFor(x => x.BookingRequestId).NotEmpty();
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.StartAtUtc).NotEmpty();
    }
}

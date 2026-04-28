using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.ConvertBookingRequestToAppointment;

public sealed class ConvertBookingRequestToAppointmentEndpoint(ICurrentUser currentUser, IBookingAccessPolicy accessPolicy, BookingManagementQueries bookingQueries)
    : Endpoint<ConvertBookingRequestToAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests/{BookingRequestId:guid}/convert");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(ConvertBookingRequestToAppointmentRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanWriteBooking(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await bookingQueries.ConvertBookingRequestToAppointmentAsync(
                new ConvertBookingRequestToAppointmentCommand(req.BookingRequestId, req.GroomerId, req.StartAtUtc),
                currentUser.UserId,
                ct);

            await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class ConvertBookingRequestToAppointmentRequest
{
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

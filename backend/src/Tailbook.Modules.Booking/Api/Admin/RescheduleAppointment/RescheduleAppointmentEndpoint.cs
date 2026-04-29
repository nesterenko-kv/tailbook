using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Tailbook.Modules.Booking.Api.Admin.RescheduleAppointment;

public sealed class RescheduleAppointmentEndpoint(BookingManagementQueries bookingQueries)
    : Endpoint<RescheduleAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/reschedule");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(RescheduleAppointmentRequest req, CancellationToken ct)
    {
        try
        {
            var result = await bookingQueries.RescheduleAppointmentAsync(
                new RescheduleAppointmentCommand(req.AppointmentId, req.GroomerId, req.StartAtUtc, req.ExpectedVersionNo),
                req.ActorUserId?.ToString("D"),
                ct);

            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(result, cancellation: ct);
        }
        catch (BookingConcurrencyException ex)
        {
            await Send.ResultAsync(Results.Conflict(new ProblemDetails
            {
                Title = "Booking concurrency conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            }));
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class RescheduleAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid AppointmentId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public int ExpectedVersionNo { get; set; }
}

public sealed class RescheduleAppointmentRequestValidator : Validator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.StartAtUtc).NotEmpty();
        RuleFor(x => x.ExpectedVersionNo).GreaterThan(0);
    }
}

using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Tailbook.Modules.Booking.Api.Admin.CancelAppointment;

public sealed class CancelAppointmentEndpoint(ICurrentUser currentUser, IBookingAccessPolicy accessPolicy, BookingManagementQueries bookingQueries)
    : Endpoint<CancelAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/cancel");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(CancelAppointmentRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteBooking(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await bookingQueries.CancelAppointmentAsync(
                new CancelAppointmentCommand(req.AppointmentId, req.ExpectedVersionNo, req.ReasonCode, req.Notes),
                currentUser.UserId,
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

public sealed class CancelAppointmentRequest
{
    public Guid AppointmentId { get; set; }
    public int ExpectedVersionNo { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class CancelAppointmentRequestValidator : Validator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.ExpectedVersionNo).GreaterThan(0);
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

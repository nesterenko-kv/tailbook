using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.CancelAppointment;

public sealed class CancelAppointmentEndpoint(IBookingManagementQueries bookingQueries)
    : Endpoint<CancelAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/cancel");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(CancelAppointmentRequest req, CancellationToken ct)
    {
        var result = await bookingQueries.CancelAppointmentAsync(
            new CancelAppointmentCommand(req.AppointmentId, req.ExpectedVersionNo, req.ReasonCode, req.Notes),
            req.ActorUserId?.ToString("D"),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, cancellation: ct);
    }
}

public sealed class CancelAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

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

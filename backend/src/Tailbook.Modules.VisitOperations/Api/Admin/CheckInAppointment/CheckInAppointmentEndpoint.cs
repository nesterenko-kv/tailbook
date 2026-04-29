using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CheckInAppointment;

public sealed class CheckInAppointmentEndpoint(VisitQueries visitQueries)
    : Endpoint<CheckInAppointmentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/check-in");
        Description(x => x.WithTags("Admin Visits"));
        PermissionsAll("visit.write");
    }

    public override async Task HandleAsync(CheckInAppointmentRequest req, CancellationToken ct)
    {
        try
        {
            var result = await visitQueries.CheckInAppointmentAsync(req.AppointmentId, req.ActorUserId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class CheckInAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid AppointmentId { get; set; }
}

public sealed class CheckInAppointmentRequestValidator : Validator<CheckInAppointmentRequest>
{
    public CheckInAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

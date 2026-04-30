using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CheckInAppointment;

public sealed class CheckInAppointmentEndpoint()
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
        var result = await new CheckInAppointmentUseCaseCommand(req.AppointmentId, req.ActorUserId).ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
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

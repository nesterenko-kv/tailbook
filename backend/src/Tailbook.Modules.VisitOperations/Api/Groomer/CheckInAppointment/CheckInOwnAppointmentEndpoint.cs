using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.CheckInAppointment;

public sealed class CheckInOwnAppointmentEndpoint(IGroomerVisitQueries groomerVisitQueries)
    : Endpoint<CheckInOwnAppointmentRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Post("/api/groomer/appointments/{appointmentId:guid}/check-in");
        Description(x => x.WithTags("Groomer Visits"));
        PermissionsAll("app.groomer.access", "groomer.visits.write");
    }

    public override async Task HandleAsync(CheckInOwnAppointmentRequest req, CancellationToken ct)
    {
        var result = await groomerVisitQueries.CheckInAppointmentAsync(req.UserId, req.AppointmentId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}

public sealed class CheckInOwnAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid AppointmentId { get; set; }
}

public sealed class CheckInOwnAppointmentRequestValidator : Validator<CheckInOwnAppointmentRequest>
{
    public CheckInOwnAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.CheckInAppointment;

public sealed class CheckInOwnAppointmentEndpoint(
    ICurrentUser currentUser,
    IGroomerVisitAccessPolicy accessPolicy,
    GroomerVisitQueries groomerVisitQueries)
    : Endpoint<CheckInOwnAppointmentRequest, GroomerVisitDetailView>
{
    public override void Configure()
    {
        Post("/api/groomer/appointments/{appointmentId:guid}/check-in");
        Description(x => x.WithTags("Groomer Visits"));
    }

    public override async Task HandleAsync(CheckInOwnAppointmentRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteOwnVisits(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!Guid.TryParse(currentUser.UserId, out var currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await groomerVisitQueries.CheckInAppointmentAsync(currentUserId, req.AppointmentId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class CheckInOwnAppointmentRequest
{
    public Guid AppointmentId { get; set; }
}

public sealed class CheckInOwnAppointmentRequestValidator : Validator<CheckInOwnAppointmentRequest>
{
    public CheckInOwnAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.VisitOperations.Application;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CheckInAppointment;

public sealed class CheckInAppointmentEndpoint(ICurrentUser currentUser, IVisitOperationsAccessPolicy accessPolicy, VisitQueries visitQueries)
    : Endpoint<CheckInAppointmentRequest, VisitDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments/{appointmentId:guid}/check-in");
        Description(x => x.WithTags("Admin Visits"));
    }

    public override async Task HandleAsync(CheckInAppointmentRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteVisits(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var actorUserId = Guid.TryParse(currentUser.UserId, out var parsed) ? parsed : (Guid?)null;
            var result = await visitQueries.CheckInAppointmentAsync(req.AppointmentId, actorUserId, ct);
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
    public Guid AppointmentId { get; set; }
}

public sealed class CheckInAppointmentRequestValidator : Validator<CheckInAppointmentRequest>
{
    public CheckInAppointmentRequestValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

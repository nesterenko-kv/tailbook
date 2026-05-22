using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Booking.Api.Admin.CreateAppointment;

public sealed class CreateAppointmentEndpoint(
    IEntityScopeService entityScopeService)
    : Endpoint<CreateAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(CreateAppointmentRequest req, CancellationToken ct)
    {
        var scopeResult = await entityScopeService.VerifyAccessAsync(
            EntityScopeResourceTypes.Pet,
            req.PetId.ToString("D"),
            req.ActorUserId,
            ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        var result = await new CreateAppointmentUseCaseCommand(
            req.PetId,
            req.GroomerId,
            req.StartAt,
            req.Items.Select(x => new CreateAppointmentItemData(x.OfferId, x.ItemType)).ToArray(),
            req.ActorUserId)
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(result.Value, StatusCodes.Status201Created, ct);
    }
}
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Pets.Api.Admin.GetPetDetail;

public sealed class GetPetDetailEndpoint(
    ICurrentUser currentUser,
    IPetsReadService petsReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetPetDetailRequest, GetPetDetailResponse>
{
    public override void Configure()
    {
        Get("/api/admin/pets/{id:guid}");
        Description(x => x.WithTags("Admin Pets"));
        PermissionsAll("pets.read");
    }

    public override async Task HandleAsync(GetPetDetailRequest req, CancellationToken ct)
    {
        var includeContacts = currentUser.HasPermission("crm.contacts.read");
        var pet = await petsReadService.GetPetAsync(req.Id, req.ActorUserId, includeContacts, ct);
        if (pet is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.Pet, req.Id.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(PetResponseMapper.ToGetPetDetailResponse(pet), ct);
    }
}

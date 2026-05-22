using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Pets.Api.Admin.UpdatePet;

public sealed class UpdatePetEndpoint : Endpoint<UpdatePetRequest, UpdatePetResponse>
{
    public override void Configure()
    {
        Patch("/api/admin/pets/{id:guid}");
        Description(x => x.WithTags("Admin Pets"));
        PermissionsAll("pets.write");
    }

    public override async Task HandleAsync(UpdatePetRequest req, CancellationToken ct)
    {
        var result = await new UpdatePetUseCaseCommand(
            req.Id,
            req.Name,
            req.AnimalTypeCode,
            req.BreedId,
            req.CoatTypeCode,
            req.SizeCategoryCode,
            req.BirthDate,
            req.WeightKg,
            req.Notes)
            .ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(PetResponseMapper.ToUpdatePetResponse(result.Value), ct);
    }
}

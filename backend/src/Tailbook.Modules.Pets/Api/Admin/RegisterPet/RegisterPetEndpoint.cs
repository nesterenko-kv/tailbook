using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Pets.Api.Admin;

namespace Tailbook.Modules.Pets.Api.Admin.RegisterPet;

public sealed class RegisterPetEndpoint : Endpoint<RegisterPetRequest, RegisterPetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pets");
        Description(x => x.WithTags("Admin Pets"));
        PermissionsAll("pets.write");
    }

    public override async Task HandleAsync(RegisterPetRequest req, CancellationToken ct)
    {
        var command = new RegisterPetUseCaseCommand(req.ClientId, req.Name, req.AnimalTypeCode, req.BreedId, req.CoatTypeCode, req.SizeCategoryCode, req.BirthDate, req.WeightKg, req.Notes);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(PetResponseMapper.ToRegisterPetResponse(result.Value), StatusCodes.Status201Created, ct);
    }
}

using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Pets.Infrastructure.Services;

public sealed class PetsCommandHandlers(PetsUseCases useCases)
    : ICommandHandler<RegisterPetUseCaseCommand, ErrorOr<PetDetailView>>,
        ICommandHandler<UpdatePetUseCaseCommand, ErrorOr<PetDetailView>>
{
    public Task<ErrorOr<PetDetailView>> ExecuteAsync(RegisterPetUseCaseCommand command, CancellationToken ct = default)
    {
        var petCommand = new RegisterPetCommand(
            command.ClientId,
            command.Name,
            command.AnimalTypeCode,
            command.BreedId,
            command.CoatTypeCode,
            command.SizeCategoryCode,
            command.BirthDate,
            command.WeightKg,
            command.Notes);

        return useCases.RegisterPetAsync(petCommand, ct);
    }

    public Task<ErrorOr<PetDetailView>> ExecuteAsync(UpdatePetUseCaseCommand command, CancellationToken ct = default)
    {
        return useCases.UpdatePetAsync(command.PetId, command.Pet, ct);
    }
}

using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Pets.Application.Pets.Commands;

public sealed record UpdatePetUseCaseCommand(
    Guid PetId,
    string Name,
    string AnimalTypeCode,
    Guid BreedId,
    string? CoatTypeCode,
    string? SizeCategoryCode,
    DateOnly? BirthDate,
    decimal? WeightKg,
    string? Notes) : ICommand<ErrorOr<PetDetailView>>;

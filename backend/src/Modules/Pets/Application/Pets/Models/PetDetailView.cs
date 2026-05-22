using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record PetDetailView(Guid Id, Guid? ClientId, string Name, AnimalTypeView AnimalType, BreedView Breed, CoatTypeView? CoatType, SizeCategoryView? SizeCategory, DateOnly? BirthDate, decimal? WeightKg, string? Notes, IReadOnlyCollection<PetPhotoView> Photos, IReadOnlyCollection<PetContactAdminSummary> Contacts, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
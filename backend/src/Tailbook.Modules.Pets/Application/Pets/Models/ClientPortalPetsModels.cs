namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record ClientPetSummaryView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode, string? Notes, string? PrimaryPhotoFileName);
public sealed record ClientPetPhotoView(Guid Id, string FileName, string ContentType, bool IsPrimary, int SortOrder);
public sealed record ClientPetDetailView(Guid Id, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes, IReadOnlyCollection<ClientPetPhotoView> Photos);

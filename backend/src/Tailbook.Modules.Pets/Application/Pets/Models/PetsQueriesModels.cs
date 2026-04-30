using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record RegisterPetCommand(Guid? ClientId, string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record UpdatePetCommand(string Name, string AnimalTypeCode, Guid BreedId, string? CoatTypeCode, string? SizeCategoryCode, DateOnly? BirthDate, decimal? WeightKg, string? Notes);
public sealed record PetCatalogView(IReadOnlyCollection<AnimalTypeView> AnimalTypes, IReadOnlyCollection<BreedGroupView> BreedGroups, IReadOnlyCollection<BreedView> Breeds, IReadOnlyCollection<CoatTypeView> CoatTypes, IReadOnlyCollection<SizeCategoryView> SizeCategories);
public sealed record AnimalTypeView(Guid Id, string Code, string Name);
public sealed record BreedGroupView(Guid Id, Guid AnimalTypeId, string Code, string Name);
public sealed record BreedView(Guid Id, Guid AnimalTypeId, Guid? BreedGroupId, string Code, string Name, IReadOnlyCollection<Guid> AllowedCoatTypeIds, IReadOnlyCollection<Guid> AllowedSizeCategoryIds);
public sealed record CoatTypeView(Guid Id, Guid? AnimalTypeId, string Code, string Name);
public sealed record SizeCategoryView(Guid Id, Guid? AnimalTypeId, string Code, string Name, decimal? MinWeightKg, decimal? MaxWeightKg);
public sealed record PetPhotoView(Guid Id, string StorageKey, string FileName, string ContentType, bool IsPrimary, int SortOrder, DateTime CreatedAtUtc);
public sealed record PetDetailView(Guid Id, Guid? ClientId, string Name, AnimalTypeView AnimalType, BreedView Breed, CoatTypeView? CoatType, SizeCategoryView? SizeCategory, DateOnly? BirthDate, decimal? WeightKg, string? Notes, IReadOnlyCollection<PetPhotoView> Photos, IReadOnlyCollection<PetContactAdminSummary> Contacts, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record PetListItemView(Guid Id, Guid? ClientId, string Name, string AnimalTypeCode, string AnimalTypeName, string BreedName, string? CoatTypeCode, string? SizeCategoryCode, decimal? WeightKg, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

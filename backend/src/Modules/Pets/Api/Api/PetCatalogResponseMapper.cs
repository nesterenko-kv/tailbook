using AdminAnimalTypeResponse = Tailbook.Modules.Pets.Api.Admin.GetPetCatalog.AnimalTypeResponse;
using AdminBreedGroupResponse = Tailbook.Modules.Pets.Api.Admin.GetPetCatalog.BreedGroupResponse;
using AdminBreedResponse = Tailbook.Modules.Pets.Api.Admin.GetPetCatalog.BreedResponse;
using AdminCoatTypeResponse = Tailbook.Modules.Pets.Api.Admin.GetPetCatalog.CoatTypeResponse;
using AdminGetPetCatalogResponse = Tailbook.Modules.Pets.Api.Admin.GetPetCatalog.GetPetCatalogResponse;
using AdminSizeCategoryResponse = Tailbook.Modules.Pets.Api.Admin.GetPetCatalog.SizeCategoryResponse;
using PublicAnimalTypeResponse = Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog.PublicAnimalTypeResponse;
using PublicBreedGroupResponse = Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog.PublicBreedGroupResponse;
using PublicBreedResponse = Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog.PublicBreedResponse;
using PublicCoatTypeResponse = Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog.PublicCoatTypeResponse;
using PublicGetPetCatalogResponse = Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog.GetPublicPetCatalogResponse;
using PublicSizeCategoryResponse = Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog.PublicSizeCategoryResponse;

namespace Tailbook.Modules.Pets.Api;

internal static class PetCatalogResponseMapper
{
    public static AdminGetPetCatalogResponse ToAdminPetCatalogResponse(PetCatalogView catalog) => new AdminGetPetCatalogResponse
    {
        AnimalTypes = catalog.AnimalTypes.Select(ToAdminAnimalTypeResponse).ToArray(),
        BreedGroups = catalog.BreedGroups.Select(ToAdminBreedGroupResponse).ToArray(),
        Breeds = catalog.Breeds.Select(ToAdminBreedResponse).ToArray(),
        CoatTypes = catalog.CoatTypes.Select(ToAdminCoatTypeResponse).ToArray(),
        SizeCategories = catalog.SizeCategories.Select(ToAdminSizeCategoryResponse).ToArray()
    };

    public static PublicGetPetCatalogResponse ToPublicPetCatalogResponse(PetCatalogView catalog) => new PublicGetPetCatalogResponse
    {
        AnimalTypes = catalog.AnimalTypes.Select(ToPublicAnimalTypeResponse).ToArray(),
        BreedGroups = catalog.BreedGroups.Select(ToPublicBreedGroupResponse).ToArray(),
        Breeds = catalog.Breeds.Select(ToPublicBreedResponse).ToArray(),
        CoatTypes = catalog.CoatTypes.Select(ToPublicCoatTypeResponse).ToArray(),
        SizeCategories = catalog.SizeCategories.Select(ToPublicSizeCategoryResponse).ToArray()
    };

    private static AdminAnimalTypeResponse ToAdminAnimalTypeResponse(AnimalTypeView item) => new AdminAnimalTypeResponse { Id = item.Id, Code = item.Code, Name = item.Name };

    private static AdminBreedGroupResponse ToAdminBreedGroupResponse(BreedGroupView breedGroup) => new AdminBreedGroupResponse { Id = breedGroup.Id, AnimalTypeId = breedGroup.AnimalTypeId, Code = breedGroup.Code, Name = breedGroup.Name };

    private static AdminBreedResponse ToAdminBreedResponse(BreedView breed) => new AdminBreedResponse
    {
        Id = breed.Id,
        AnimalTypeId = breed.AnimalTypeId,
        BreedGroupId = breed.BreedGroupId,
        Code = breed.Code,
        Name = breed.Name,
        AllowedCoatTypeIds = breed.AllowedCoatTypeIds.ToArray(),
        AllowedSizeCategoryIds = breed.AllowedSizeCategoryIds.ToArray()
    };

    private static AdminCoatTypeResponse ToAdminCoatTypeResponse(CoatTypeView coatType) => new AdminCoatTypeResponse { Id = coatType.Id, AnimalTypeId = coatType.AnimalTypeId, Code = coatType.Code, Name = coatType.Name };

    private static AdminSizeCategoryResponse ToAdminSizeCategoryResponse(SizeCategoryView sizeCategory) => new AdminSizeCategoryResponse { Id = sizeCategory.Id, AnimalTypeId = sizeCategory.AnimalTypeId, Code = sizeCategory.Code, Name = sizeCategory.Name, MinWeightKg = sizeCategory.MinWeightKg, MaxWeightKg = sizeCategory.MaxWeightKg };

    private static PublicAnimalTypeResponse ToPublicAnimalTypeResponse(AnimalTypeView item) => new PublicAnimalTypeResponse { Id = item.Id, Code = item.Code, Name = item.Name };

    private static PublicBreedGroupResponse ToPublicBreedGroupResponse(BreedGroupView breedGroup) => new PublicBreedGroupResponse { Id = breedGroup.Id, AnimalTypeId = breedGroup.AnimalTypeId, Code = breedGroup.Code, Name = breedGroup.Name };

    private static PublicBreedResponse ToPublicBreedResponse(BreedView breed) => new PublicBreedResponse
    {
        Id = breed.Id,
        AnimalTypeId = breed.AnimalTypeId,
        BreedGroupId = breed.BreedGroupId,
        Code = breed.Code,
        Name = breed.Name,
        AllowedCoatTypeIds = breed.AllowedCoatTypeIds.ToArray(),
        AllowedSizeCategoryIds = breed.AllowedSizeCategoryIds.ToArray()
    };

    private static PublicCoatTypeResponse ToPublicCoatTypeResponse(CoatTypeView coatType) => new PublicCoatTypeResponse { Id = coatType.Id, AnimalTypeId = coatType.AnimalTypeId, Code = coatType.Code, Name = coatType.Name };

    private static PublicSizeCategoryResponse ToPublicSizeCategoryResponse(SizeCategoryView sizeCategory) => new PublicSizeCategoryResponse { Id = sizeCategory.Id, AnimalTypeId = sizeCategory.AnimalTypeId, Code = sizeCategory.Code, Name = sizeCategory.Name, MinWeightKg = sizeCategory.MinWeightKg, MaxWeightKg = sizeCategory.MaxWeightKg };
}

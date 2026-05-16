using Tailbook.BuildingBlocks.Abstractions;
using DetailBreedResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.BreedResponse;
using DetailContactMethodResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.ContactMethodResponse;
using DetailNamedCatalogItemResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.NamedCatalogItemResponse;
using DetailPetContactResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.PetContactResponse;
using DetailPetPhotoResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.PetPhotoResponse;
using DetailSizeCategoryItemResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.SizeCategoryItemResponse;
using DetailGetPetDetailResponse = Tailbook.Modules.Pets.Api.Admin.GetPetDetail.GetPetDetailResponse;
using RegisterBreedResponse = Tailbook.Modules.Pets.Api.Admin.RegisterPet.BreedResponse;
using RegisterNamedCatalogItemResponse = Tailbook.Modules.Pets.Api.Admin.RegisterPet.NamedCatalogItemResponse;
using RegisterPetResponse = Tailbook.Modules.Pets.Api.Admin.RegisterPet.RegisterPetResponse;
using RegisterSizeCategoryItemResponse = Tailbook.Modules.Pets.Api.Admin.RegisterPet.SizeCategoryItemResponse;
using UpdateBreedResponse = Tailbook.Modules.Pets.Api.Admin.UpdatePet.BreedResponse;
using UpdateNamedCatalogItemResponse = Tailbook.Modules.Pets.Api.Admin.UpdatePet.NamedCatalogItemResponse;
using UpdatePetResponse = Tailbook.Modules.Pets.Api.Admin.UpdatePet.UpdatePetResponse;
using UpdateSizeCategoryItemResponse = Tailbook.Modules.Pets.Api.Admin.UpdatePet.SizeCategoryItemResponse;

namespace Tailbook.Modules.Pets.Api.Admin;

internal static class PetResponseMapper
{
    public static RegisterPetResponse ToRegisterPetResponse(PetDetailView pet)
    {
        return new RegisterPetResponse
        {
            Id = pet.Id,
            ClientId = pet.ClientId,
            Name = pet.Name,
            AnimalType = ToRegisterNamedCatalogItemResponse(pet.AnimalType),
            Breed = ToRegisterBreedResponse(pet.Breed),
            CoatType = pet.CoatType is null ? null : ToRegisterNamedCatalogItemResponse(pet.CoatType),
            SizeCategory = pet.SizeCategory is null ? null : ToRegisterSizeCategoryItemResponse(pet.SizeCategory),
            BirthDate = pet.BirthDate,
            WeightKg = pet.WeightKg,
            Notes = pet.Notes,
            CreatedAt = pet.CreatedAt,
            UpdatedAt = pet.UpdatedAt
        };
    }

    public static UpdatePetResponse ToUpdatePetResponse(PetDetailView pet)
    {
        return new UpdatePetResponse
        {
            Id = pet.Id,
            ClientId = pet.ClientId,
            Name = pet.Name,
            AnimalType = ToUpdateNamedCatalogItemResponse(pet.AnimalType),
            Breed = ToUpdateBreedResponse(pet.Breed),
            CoatType = pet.CoatType is null ? null : ToUpdateNamedCatalogItemResponse(pet.CoatType),
            SizeCategory = pet.SizeCategory is null ? null : ToUpdateSizeCategoryItemResponse(pet.SizeCategory),
            BirthDate = pet.BirthDate,
            WeightKg = pet.WeightKg,
            Notes = pet.Notes,
            UpdatedAt = pet.UpdatedAt
        };
    }

    public static DetailGetPetDetailResponse ToGetPetDetailResponse(PetDetailView pet)
    {
        return new DetailGetPetDetailResponse
        {
            Id = pet.Id,
            ClientId = pet.ClientId,
            Name = pet.Name,
            AnimalType = ToDetailNamedCatalogItemResponse(pet.AnimalType),
            Breed = ToDetailBreedResponse(pet.Breed),
            CoatType = pet.CoatType is null ? null : ToDetailNamedCatalogItemResponse(pet.CoatType),
            SizeCategory = pet.SizeCategory is null ? null : ToDetailSizeCategoryItemResponse(pet.SizeCategory),
            BirthDate = pet.BirthDate,
            WeightKg = pet.WeightKg,
            Notes = pet.Notes,
            Photos = pet.Photos.Select(ToPetPhotoResponse).ToArray(),
            Contacts = pet.Contacts.Select(ToPetContactResponse).ToArray(),
            CreatedAt = pet.CreatedAt,
            UpdatedAt = pet.UpdatedAt
        };
    }

    private static RegisterNamedCatalogItemResponse ToRegisterNamedCatalogItemResponse(AnimalTypeView item)
    {
        return new RegisterNamedCatalogItemResponse { Id = item.Id, Code = item.Code, Name = item.Name };
    }

    private static RegisterNamedCatalogItemResponse ToRegisterNamedCatalogItemResponse(CoatTypeView item)
    {
        return new RegisterNamedCatalogItemResponse { Id = item.Id, Code = item.Code, Name = item.Name };
    }

    private static RegisterBreedResponse ToRegisterBreedResponse(BreedView breed)
    {
        return new RegisterBreedResponse { Id = breed.Id, AnimalTypeId = breed.AnimalTypeId, BreedGroupId = breed.BreedGroupId, Code = breed.Code, Name = breed.Name };
    }

    private static RegisterSizeCategoryItemResponse ToRegisterSizeCategoryItemResponse(SizeCategoryView sizeCategory)
    {
        return new RegisterSizeCategoryItemResponse { Id = sizeCategory.Id, Code = sizeCategory.Code, Name = sizeCategory.Name, MinWeightKg = sizeCategory.MinWeightKg, MaxWeightKg = sizeCategory.MaxWeightKg };
    }

    private static UpdateNamedCatalogItemResponse ToUpdateNamedCatalogItemResponse(AnimalTypeView item)
    {
        return new UpdateNamedCatalogItemResponse { Id = item.Id, Code = item.Code, Name = item.Name };
    }

    private static UpdateNamedCatalogItemResponse ToUpdateNamedCatalogItemResponse(CoatTypeView item)
    {
        return new UpdateNamedCatalogItemResponse { Id = item.Id, Code = item.Code, Name = item.Name };
    }

    private static UpdateBreedResponse ToUpdateBreedResponse(BreedView breed)
    {
        return new UpdateBreedResponse { Id = breed.Id, AnimalTypeId = breed.AnimalTypeId, BreedGroupId = breed.BreedGroupId, Code = breed.Code, Name = breed.Name };
    }

    private static UpdateSizeCategoryItemResponse ToUpdateSizeCategoryItemResponse(SizeCategoryView sizeCategory)
    {
        return new UpdateSizeCategoryItemResponse { Id = sizeCategory.Id, Code = sizeCategory.Code, Name = sizeCategory.Name, MinWeightKg = sizeCategory.MinWeightKg, MaxWeightKg = sizeCategory.MaxWeightKg };
    }

    private static DetailNamedCatalogItemResponse ToDetailNamedCatalogItemResponse(AnimalTypeView item)
    {
        return new DetailNamedCatalogItemResponse { Id = item.Id, Code = item.Code, Name = item.Name };
    }

    private static DetailNamedCatalogItemResponse ToDetailNamedCatalogItemResponse(CoatTypeView item)
    {
        return new DetailNamedCatalogItemResponse { Id = item.Id, Code = item.Code, Name = item.Name };
    }

    private static DetailBreedResponse ToDetailBreedResponse(BreedView breed)
    {
        return new DetailBreedResponse { Id = breed.Id, AnimalTypeId = breed.AnimalTypeId, BreedGroupId = breed.BreedGroupId, Code = breed.Code, Name = breed.Name };
    }

    private static DetailSizeCategoryItemResponse ToDetailSizeCategoryItemResponse(SizeCategoryView sizeCategory)
    {
        return new DetailSizeCategoryItemResponse { Id = sizeCategory.Id, Code = sizeCategory.Code, Name = sizeCategory.Name, MinWeightKg = sizeCategory.MinWeightKg, MaxWeightKg = sizeCategory.MaxWeightKg };
    }

    private static DetailPetPhotoResponse ToPetPhotoResponse(PetPhotoView photo)
    {
        return new DetailPetPhotoResponse { Id = photo.Id, StorageKey = photo.StorageKey, FileName = photo.FileName, ContentType = photo.ContentType, IsPrimary = photo.IsPrimary, SortOrder = photo.SortOrder, CreatedAt = photo.CreatedAt };
    }

    private static DetailPetContactResponse ToPetContactResponse(PetContactAdminSummary contact)
    {
        return new DetailPetContactResponse
        {
            ContactId = contact.ContactId,
            ClientId = contact.ClientId,
            FullName = contact.FullName,
            IsPrimary = contact.IsPrimary,
            CanPickUp = contact.CanPickUp,
            CanPay = contact.CanPay,
            ReceivesNotifications = contact.ReceivesNotifications,
            RoleCodes = contact.RoleCodes,
            Methods = contact.Methods.Select(ToContactMethodResponse).ToArray()
        };
    }

    private static DetailContactMethodResponse ToContactMethodResponse(ContactMethodAdminSummary method)
    {
        return new DetailContactMethodResponse
        {
            Id = method.Id,
            MethodType = method.MethodType,
            DisplayValue = method.DisplayValue,
            IsPreferred = method.IsPreferred,
            VerificationStatus = method.VerificationStatus
        };
    }
}

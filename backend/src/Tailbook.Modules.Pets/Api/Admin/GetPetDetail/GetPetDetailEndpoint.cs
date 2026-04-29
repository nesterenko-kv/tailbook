using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Pets.Application;

namespace Tailbook.Modules.Pets.Api.Admin.GetPetDetail;

public sealed class GetPetDetailEndpoint(ICurrentUser currentUser, PetsQueries petsQueries)
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
        var pet = await petsQueries.GetPetAsync(req.Id, req.ActorUserId, includeContacts, ct);
        if (pet is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetPetDetailResponse
        {
            Id = pet.Id,
            ClientId = pet.ClientId,
            Name = pet.Name,
            AnimalType = new NamedCatalogItemResponse { Id = pet.AnimalType.Id, Code = pet.AnimalType.Code, Name = pet.AnimalType.Name },
            Breed = new BreedResponse { Id = pet.Breed.Id, AnimalTypeId = pet.Breed.AnimalTypeId, BreedGroupId = pet.Breed.BreedGroupId, Code = pet.Breed.Code, Name = pet.Breed.Name },
            CoatType = pet.CoatType is null ? null : new NamedCatalogItemResponse { Id = pet.CoatType.Id, Code = pet.CoatType.Code, Name = pet.CoatType.Name },
            SizeCategory = pet.SizeCategory is null ? null : new SizeCategoryItemResponse { Id = pet.SizeCategory.Id, Code = pet.SizeCategory.Code, Name = pet.SizeCategory.Name, MinWeightKg = pet.SizeCategory.MinWeightKg, MaxWeightKg = pet.SizeCategory.MaxWeightKg },
            BirthDate = pet.BirthDate,
            WeightKg = pet.WeightKg,
            Notes = pet.Notes,
            Photos = pet.Photos.Select(x => new PetPhotoResponse { Id = x.Id, StorageKey = x.StorageKey, FileName = x.FileName, ContentType = x.ContentType, IsPrimary = x.IsPrimary, SortOrder = x.SortOrder, CreatedAtUtc = x.CreatedAtUtc }).ToArray(),
            Contacts = pet.Contacts.Select(x => new PetContactResponse
            {
                ContactId = x.ContactId,
                ClientId = x.ClientId,
                FullName = x.FullName,
                IsPrimary = x.IsPrimary,
                CanPickUp = x.CanPickUp,
                CanPay = x.CanPay,
                ReceivesNotifications = x.ReceivesNotifications,
                RoleCodes = x.RoleCodes,
                Methods = x.Methods.Select(m => new ContactMethodResponse
                {
                    Id = m.Id,
                    MethodType = m.MethodType,
                    DisplayValue = m.DisplayValue,
                    IsPreferred = m.IsPreferred,
                    VerificationStatus = m.VerificationStatus
                }).ToArray()
            }).ToArray(),
            CreatedAtUtc = pet.CreatedAtUtc,
            UpdatedAtUtc = pet.UpdatedAtUtc
        }, ct);
    }
}

public sealed class GetPetDetailRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid Id { get; set; }
}

public sealed class GetPetDetailResponse
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public NamedCatalogItemResponse AnimalType { get; set; } = new();
    public BreedResponse Breed { get; set; } = new();
    public NamedCatalogItemResponse? CoatType { get; set; }
    public SizeCategoryItemResponse? SizeCategory { get; set; }
    public DateOnly? BirthDate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyCollection<PetPhotoResponse> Photos { get; set; } = [];
    public IReadOnlyCollection<PetContactResponse> Contacts { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class NamedCatalogItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class BreedResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class SizeCategoryItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? MinWeightKg { get; set; }
    public decimal? MaxWeightKg { get; set; }
}

public sealed class PetPhotoResponse
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PetContactResponse
{
    public Guid ContactId { get; set; }
    public Guid ClientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; }
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
    public IReadOnlyCollection<ContactMethodResponse> Methods { get; set; } = [];
}

public sealed class ContactMethodResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}

using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Pets.Application;

namespace Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog;

public sealed class GetPublicPetCatalogEndpoint(PetsQueries petsQueries)
    : EndpointWithoutRequest<GetPublicPetCatalogResponse>
{
    public override void Configure()
    {
        Get("/api/public/pets/catalog");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var catalog = await petsQueries.GetCatalogAsync(ct);
        await Send.OkAsync(new GetPublicPetCatalogResponse
        {
            AnimalTypes = catalog.AnimalTypes.Select(x => new PublicAnimalTypeResponse { Id = x.Id, Code = x.Code, Name = x.Name }).ToArray(),
            BreedGroups = catalog.BreedGroups.Select(x => new PublicBreedGroupResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, Code = x.Code, Name = x.Name }).ToArray(),
            Breeds = catalog.Breeds.Select(x => new PublicBreedResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, BreedGroupId = x.BreedGroupId, Code = x.Code, Name = x.Name, AllowedCoatTypeIds = x.AllowedCoatTypeIds.ToArray(), AllowedSizeCategoryIds = x.AllowedSizeCategoryIds.ToArray() }).ToArray(),
            CoatTypes = catalog.CoatTypes.Select(x => new PublicCoatTypeResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, Code = x.Code, Name = x.Name }).ToArray(),
            SizeCategories = catalog.SizeCategories.Select(x => new PublicSizeCategoryResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, Code = x.Code, Name = x.Name, MinWeightKg = x.MinWeightKg, MaxWeightKg = x.MaxWeightKg }).ToArray()
        }, ct);
    }
}

public sealed class GetPublicPetCatalogResponse
{
    public IReadOnlyCollection<PublicAnimalTypeResponse> AnimalTypes { get; set; } = [];
    public IReadOnlyCollection<PublicBreedGroupResponse> BreedGroups { get; set; } = [];
    public IReadOnlyCollection<PublicBreedResponse> Breeds { get; set; } = [];
    public IReadOnlyCollection<PublicCoatTypeResponse> CoatTypes { get; set; } = [];
    public IReadOnlyCollection<PublicSizeCategoryResponse> SizeCategories { get; set; } = [];
}

public sealed class PublicAnimalTypeResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PublicBreedGroupResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PublicBreedResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<Guid> AllowedCoatTypeIds { get; set; } = [];
    public IReadOnlyCollection<Guid> AllowedSizeCategoryIds { get; set; } = [];
}

public sealed class PublicCoatTypeResponse
{
    public Guid Id { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PublicSizeCategoryResponse
{
    public Guid Id { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? MinWeightKg { get; set; }
    public decimal? MaxWeightKg { get; set; }
}

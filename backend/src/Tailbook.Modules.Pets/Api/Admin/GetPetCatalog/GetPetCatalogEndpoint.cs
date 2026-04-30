using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Pets.Api.Admin.GetPetCatalog;

public sealed class GetPetCatalogEndpoint(IPetsReadService petsReadService)
    : EndpointWithoutRequest<GetPetCatalogResponse>
{
    public override void Configure()
    {
        Get("/api/admin/pets/catalog");
        Description(x => x.WithTags("Admin Pets"));
        Permissions("pets.catalog.read", "pets.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var catalog = await petsReadService.GetCatalogAsync(ct);
        await Send.OkAsync(new GetPetCatalogResponse
        {
            AnimalTypes = catalog.AnimalTypes.Select(x => new AnimalTypeResponse { Id = x.Id, Code = x.Code, Name = x.Name }).ToArray(),
            BreedGroups = catalog.BreedGroups.Select(x => new BreedGroupResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, Code = x.Code, Name = x.Name }).ToArray(),
            Breeds = catalog.Breeds.Select(x => new BreedResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, BreedGroupId = x.BreedGroupId, Code = x.Code, Name = x.Name, AllowedCoatTypeIds = x.AllowedCoatTypeIds.ToArray(), AllowedSizeCategoryIds = x.AllowedSizeCategoryIds.ToArray() }).ToArray(),
            CoatTypes = catalog.CoatTypes.Select(x => new CoatTypeResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, Code = x.Code, Name = x.Name }).ToArray(),
            SizeCategories = catalog.SizeCategories.Select(x => new SizeCategoryResponse { Id = x.Id, AnimalTypeId = x.AnimalTypeId, Code = x.Code, Name = x.Name, MinWeightKg = x.MinWeightKg, MaxWeightKg = x.MaxWeightKg }).ToArray()
        }, ct);
    }
}

public sealed class GetPetCatalogResponse
{
    public IReadOnlyCollection<AnimalTypeResponse> AnimalTypes { get; set; } = [];
    public IReadOnlyCollection<BreedGroupResponse> BreedGroups { get; set; } = [];
    public IReadOnlyCollection<BreedResponse> Breeds { get; set; } = [];
    public IReadOnlyCollection<CoatTypeResponse> CoatTypes { get; set; } = [];
    public IReadOnlyCollection<SizeCategoryResponse> SizeCategories { get; set; } = [];
}

public sealed class AnimalTypeResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class BreedGroupResponse
{
    public Guid Id { get; set; }
    public Guid AnimalTypeId { get; set; }
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
    public IReadOnlyCollection<Guid> AllowedCoatTypeIds { get; set; } = [];
    public IReadOnlyCollection<Guid> AllowedSizeCategoryIds { get; set; } = [];
}

public sealed class CoatTypeResponse
{
    public Guid Id { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class SizeCategoryResponse
{
    public Guid Id { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? MinWeightKg { get; set; }
    public decimal? MaxWeightKg { get; set; }
}

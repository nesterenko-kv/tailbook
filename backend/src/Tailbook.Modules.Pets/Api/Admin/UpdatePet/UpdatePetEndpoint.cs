using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Pets.Api.Admin.UpdatePet;

public sealed class UpdatePetEndpoint(IPetsQueries petsQueries)
    : Endpoint<UpdatePetRequest, UpdatePetResponse>
{
    public override void Configure()
    {
        Patch("/api/admin/pets/{id:guid}");
        Description(x => x.WithTags("Admin Pets"));
        PermissionsAll("pets.write");
    }

    public override async Task HandleAsync(UpdatePetRequest req, CancellationToken ct)
    {
        var result = await petsQueries.UpdatePetAsync(req.Id, new UpdatePetCommand(req.Name, req.AnimalTypeCode, req.BreedId, req.CoatTypeCode, req.SizeCategoryCode, req.BirthDate, req.WeightKg, req.Notes), ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var pet = result.Value;
        await Send.OkAsync(new UpdatePetResponse
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
            UpdatedAtUtc = pet.UpdatedAtUtc
        }, ct);
    }
}

public sealed class UpdatePetRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AnimalTypeCode { get; set; } = string.Empty;
    public Guid BreedId { get; set; }
    public string? CoatTypeCode { get; set; }
    public string? SizeCategoryCode { get; set; }
    public DateOnly? BirthDate { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdatePetRequestValidator : Validator<UpdatePetRequest>
{
    public UpdatePetRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AnimalTypeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.BreedId).NotEmpty();
        RuleFor(x => x.CoatTypeCode).MaximumLength(64);
        RuleFor(x => x.SizeCategoryCode).MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0).When(x => x.WeightKg.HasValue);
    }
}

public sealed class UpdatePetResponse
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

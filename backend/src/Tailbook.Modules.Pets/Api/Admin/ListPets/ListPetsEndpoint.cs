using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Pets.Api.Admin.ListPets;

public sealed class ListPetsEndpoint(IPetsQueries petsQueries)
    : Endpoint<ListPetsRequest, PagedResult<PetListItemView>>
{
    public override void Configure()
    {
        Get("/api/admin/pets");
        Description(x => x.WithTags("Admin Pets"));
        PermissionsAll("pets.read");
    }

    public override async Task HandleAsync(ListPetsRequest req, CancellationToken ct)
    {
        var result = await petsQueries.ListPetsAsync(
            req.Search,
            req.ClientId,
            req.AnimalTypeCode,
            req.BreedId,
            req.Page,
            req.PageSize,
            ct);

        await Send.OkAsync(result, ct);
    }
}

public sealed class ListPetsRequest
{
    public string? Search { get; set; }
    public Guid? ClientId { get; set; }
    public string? AnimalTypeCode { get; set; }
    public Guid? BreedId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ListPetsRequestValidator : Validator<ListPetsRequest>
{
    public ListPetsRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(128);
        RuleFor(x => x.AnimalTypeCode).MaximumLength(64);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

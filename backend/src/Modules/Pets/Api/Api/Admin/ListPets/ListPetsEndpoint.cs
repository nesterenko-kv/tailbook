using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Pets.Api.Admin.ListPets;

public sealed class ListPetsEndpoint(
    IPetsReadService petsReadService,
    IScopeAuthorizationService scopeAuthorizationService)
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
        var result = await petsReadService.ListPetsAsync(
            req.Search,
            req.ClientId,
            req.AnimalTypeCode,
            req.BreedId,
            req.Page,
            req.PageSize,
            ct);

        var actorUserIdClaim = User.FindFirst(TailbookClaimTypes.UserId)?.Value;
        IReadOnlyCollection<PetListItemView> filteredItems = result.Items;
        var totalCount = result.TotalCount;

        if (Guid.TryParse(actorUserIdClaim, out var userId))
        {
            var hasGlobal = await scopeAuthorizationService.HasGlobalScopeAsync(userId, ct);
            if (!hasGlobal)
            {
                filteredItems = await ScopeFilter.ApplyAsync(
                    result.Items,
                    userId,
                    EntityScopeResourceTypes.Pet,
                    item => item.Id.ToString("D"),
                    scopeAuthorizationService,
                    ct);
                totalCount = filteredItems.Count;
            }
        }

        var pagedResult = new PagedResult<PetListItemView>(filteredItems, result.Page, result.PageSize, totalCount);
        await Send.OkAsync(pagedResult, ct);
    }
}
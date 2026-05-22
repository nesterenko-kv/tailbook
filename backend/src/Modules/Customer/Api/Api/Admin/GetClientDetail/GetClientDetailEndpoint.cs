using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Customer.Api.Admin.GetClientDetail;

public sealed class GetClientDetailEndpoint(
    ICustomerReadService customerReadService,
    IEntityScopeService entityScopeService)
    : Endpoint<GetClientDetailRequest, GetClientDetailResponse>
{
    public override void Configure()
    {
        Get("/api/admin/clients/{id:guid}");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.clients.read", "crm.contacts.read");
    }

    public override async Task HandleAsync(GetClientDetailRequest req, CancellationToken ct)
    {
        var client = await customerReadService.GetClientDetailAsync(req.Id, req.ActorUserId, ct);
        if (client is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.Client, req.Id.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(new GetClientDetailResponse
        {
            Id = client.Id,
            DisplayName = client.DisplayName,
            Status = client.Status,
            Notes = client.Notes,
            Contacts = client.Contacts.Select(x => new ContactPersonResponse
            {
                Id = x.Id,
                ClientId = x.ClientId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Notes = x.Notes,
                TrustLevel = x.TrustLevel,
                Methods = x.Methods.Select(m => new ContactMethodResponse
                {
                    Id = m.Id,
                    MethodType = m.MethodType,
                    DisplayValue = m.DisplayValue,
                    IsPreferred = m.IsPreferred,
                    VerificationStatus = m.VerificationStatus,
                    Notes = m.Notes
                }).ToArray()
            }).ToArray(),
            Pets = client.Pets.Select(x => new ClientPetSummaryResponse
            {
                Id = x.Id,
                Name = x.Name,
                AnimalTypeCode = x.AnimalTypeCode,
                AnimalTypeName = x.AnimalTypeName,
                BreedName = x.BreedName,
                CoatTypeCode = x.CoatTypeCode,
                SizeCategoryCode = x.SizeCategoryCode
            }).ToArray(),
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        }, ct);
    }
}

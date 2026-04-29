using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Customer.Application;

namespace Tailbook.Modules.Customer.Api.Admin.GetClientDetail;

public sealed class GetClientDetailEndpoint(ICurrentUser currentUser, CustomerQueries customerQueries)
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
        var actorUserId = Guid.TryParse(currentUser.UserId, out var parsed) ? parsed : (Guid?)null;
        var client = await customerQueries.GetClientDetailAsync(req.Id, actorUserId, ct);
        if (client is null)
        {
            await Send.NotFoundAsync(ct);
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
            CreatedAtUtc = client.CreatedAtUtc,
            UpdatedAtUtc = client.UpdatedAtUtc
        }, ct);
    }
}

public sealed class GetClientDetailRequest
{
    public Guid Id { get; set; }
}

public sealed class GetClientDetailResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public IReadOnlyCollection<ContactPersonResponse> Contacts { get; set; } = [];
    public IReadOnlyCollection<ClientPetSummaryResponse> Pets { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class ContactPersonResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Notes { get; set; }
    public string TrustLevel { get; set; } = string.Empty;
    public IReadOnlyCollection<ContactMethodResponse> Methods { get; set; } = [];
}

public sealed class ContactMethodResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class ClientPetSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AnimalTypeCode { get; set; } = string.Empty;
    public string AnimalTypeName { get; set; } = string.Empty;
    public string BreedName { get; set; } = string.Empty;
    public string? CoatTypeCode { get; set; }
    public string? SizeCategoryCode { get; set; }
}

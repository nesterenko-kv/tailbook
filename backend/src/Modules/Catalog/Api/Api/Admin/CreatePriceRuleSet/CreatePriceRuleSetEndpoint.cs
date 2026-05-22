using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRuleSet;

public sealed class CreatePriceRuleSetEndpoint : Endpoint<CreatePriceRuleSetRequest, CreatePriceRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pricing/rule-sets");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreatePriceRuleSetRequest req, CancellationToken ct)
    {
        var command = new CreateCatalogPriceRuleSetCommand(req.ValidFrom, req.ValidTo);
        var result = await command.ExecuteAsync(ct);
        await Send.ResponseAsync(new CreatePriceRuleSetResponse
        {
            Id = result.Id,
            VersionNo = result.VersionNo,
            Status = result.Status,
            ValidFrom = result.ValidFrom,
            ValidTo = result.ValidTo,
            CreatedAt = result.CreatedAt,
            PublishedAt = result.PublishedAt,
            Rules = []
        }, StatusCodes.Status201Created, ct);
    }
}

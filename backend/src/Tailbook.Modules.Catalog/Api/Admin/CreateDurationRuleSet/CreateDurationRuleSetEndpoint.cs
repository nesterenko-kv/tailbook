using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRuleSet;

public sealed class CreateDurationRuleSetEndpoint : Endpoint<CreateDurationRuleSetRequest, CreateDurationRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateDurationRuleSetRequest req, CancellationToken ct)
    {
        var command = new CreateCatalogDurationRuleSetCommand(req.ValidFrom, req.ValidTo);
        var result = await command.ExecuteAsync(ct);
        await Send.ResponseAsync(new CreateDurationRuleSetResponse
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

using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

public sealed class CreateOfferEndpoint : Endpoint<CreateOfferRequest, OfferResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offers");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateOfferRequest req, CancellationToken ct)
    {
        var command = new CreateCatalogOfferCommand(req.Code, req.OfferType, req.DisplayName);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(OfferResponse.Map(result.Value), StatusCodes.Status201Created, ct);
    }
}

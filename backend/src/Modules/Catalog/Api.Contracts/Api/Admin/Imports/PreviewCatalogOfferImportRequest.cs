using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Catalog.Api.Admin.Imports;

public sealed class PreviewCatalogOfferImportRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid ActorUserId { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public string CsvContent { get; set; } = string.Empty;
}

using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Customer.Api.Client.GetMyContactPreferences;

public sealed class UpdateMyContactPreferencesRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public UpdateMyContactMethodPayload[] Methods { get; set; } = [];
}

using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.GetMe;

public sealed class GetMeEndpoint(ICurrentUser currentUser) : EndpointWithoutRequest<GetMeResponse>
{
    public override void Configure()
    {
        Get("api/identity/me");
        Roles("Admin", "Manager", "Groomer", "Client");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new GetMeResponse
        {
            IsAuthenticated = currentUser.IsAuthenticated,
            SubjectId = currentUser.SubjectId,
            Roles = currentUser.Roles
        }, ct);
    }
}

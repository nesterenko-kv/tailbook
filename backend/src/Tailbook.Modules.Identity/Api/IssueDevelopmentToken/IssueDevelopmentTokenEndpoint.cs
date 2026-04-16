using FastEndpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.IssueDevelopmentToken;

public sealed class IssueDevelopmentTokenEndpoint(JwtTokenFactory jwtTokenFactory, IWebHostEnvironment environment)
    : Endpoint<IssueDevelopmentTokenRequest, IssueDevelopmentTokenResponse>
{
    public override void Configure()
    {
        Post("/api/identity/dev-token");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(IssueDevelopmentTokenRequest req, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            await Send.ForbiddenAsync(ct); //sending response here
            return;
        }

        var token = jwtTokenFactory.CreateToken(req.SubjectId, req.Email, req.Roles);
        await Send.OkAsync(new IssueDevelopmentTokenResponse { AccessToken = token }, cancellation: ct);
    }
}

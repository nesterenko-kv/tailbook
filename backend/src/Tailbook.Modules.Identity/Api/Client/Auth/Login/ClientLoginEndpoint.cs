using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Login;

public sealed class ClientLoginEndpoint(ClientPortalIdentityQueries identityQueries) : Endpoint<ClientLoginRequest, ClientLoginResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/login");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(ClientLoginRequest req, CancellationToken ct)
    {
        var result = await identityQueries.AuthenticateClientAsync(req.Email, req.Password, ct);
        if (result is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(new ClientLoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            User = result.User
        }, cancellation: ct);
    }
}

public sealed class ClientLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ClientLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public AuthenticatedUserView User { get; set; } = default!;
}

public sealed class ClientLoginRequestValidator : Validator<ClientLoginRequest>
{
    public ClientLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

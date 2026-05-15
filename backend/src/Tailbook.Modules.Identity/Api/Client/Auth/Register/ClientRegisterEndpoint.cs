using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Register;

public sealed class ClientRegisterEndpoint(
    IRegisterClientPortalUserHandler registerHandler,
    IAuthenticateUserService authenticateUserHandler,
    BrowserSessionService browserSessionService) : Endpoint<ClientRegisterRequest, ClientRegisterResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/register");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(ClientRegisterRequest req, CancellationToken ct)
    {
        var command = new RegisterClientPortalUserInput(
            req.DisplayName,
            req.FirstName,
            req.LastName,
            req.Email,
            req.Password,
            req.Phone,
            req.Instagram
        );

        var registerResult = await registerHandler.ExecuteResultAsync(command, ct);
        if (registerResult.IsError)
        {
            await Send.ResultAsync(registerResult.Errors.ToHttpResult());
            return;
        }

        var result = await authenticateUserHandler.AuthenticateAsync(
            req.Email,
            req.Password,
            requireClientPortalAccess: true,
            enforceMfa: false,
            requestIpAddress: null,
            userAgent: null,
            deviceTrustToken: null,
            cancellationToken: ct);
        if (result.IsError)
        {
            Logger.Log(LogLevel.Warning, "Client portal registration finished without an active login session.");
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        if (result.Value is not AuthenticationSucceededResult { Login: var login })
        {
            throw new InvalidOperationException("Client portal registration does not support MFA challenges.");
        }

        var browserSession = browserSessionService.ApplyClientSession(HttpContext, login);
        if (browserSession.IsError)
        {
            await Send.ResultAsync(browserSession.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(new ClientRegisterResponse
        {
            AccessToken = login.AccessToken,
            ExpiresAt = login.ExpiresAt,
            RefreshToken = browserSession.Value.IncludeRefreshTokenInResponse ? login.RefreshToken : null,
            RefreshTokenExpiresAt = login.RefreshTokenExpiresAt,
            User = login.User
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class ClientRegisterRequestValidator : Validator<ClientRegisterRequest>
{
    public ClientRegisterRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Instagram).MaximumLength(128);
    }
}

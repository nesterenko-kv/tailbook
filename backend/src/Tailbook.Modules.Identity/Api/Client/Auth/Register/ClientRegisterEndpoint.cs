using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Register;

public sealed class ClientRegisterEndpoint(
    IRegisterClientPortalUserHandler registerHandler,
    IAuthenticateUserService authenticateUserHandler) : Endpoint<ClientRegisterRequest, ClientRegisterResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/register");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(ClientRegisterRequest req, CancellationToken ct)
    {
        var command = new RegisterClientPortalUserCommand(
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

        var result = await authenticateUserHandler.AuthenticateAsync(req.Email, req.Password, ct);
        if (result is null)
        {
            Logger.Log(LogLevel.Warning, "Client portal registration finished without an active login session.");
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.ResponseAsync(new ClientRegisterResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
            User = result.User
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class ClientRegisterRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Instagram { get; set; }
}

public sealed class ClientRegisterResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public AuthenticatedUserView User { get; set; } = default!;
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

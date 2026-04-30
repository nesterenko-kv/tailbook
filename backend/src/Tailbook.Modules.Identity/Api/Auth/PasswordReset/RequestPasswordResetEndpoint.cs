using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Identity.Api.Auth.PasswordReset;

public sealed class RequestPasswordResetEndpoint(IPasswordResetService passwordResetService)
    : Endpoint<RequestPasswordResetRequest>
{
    public override void Configure()
    {
        Post("/api/identity/auth/request-password-reset");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(RequestPasswordResetRequest req, CancellationToken ct)
    {
        await passwordResetService.RequestResetAsync(req.Email, ct);
        await Send.StatusCodeAsync(StatusCodes.Status202Accepted, ct);
    }
}

public sealed class RequestPasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class RequestPasswordResetRequestValidator : Validator<RequestPasswordResetRequest>
{
    public RequestPasswordResetRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

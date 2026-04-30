using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Identity.Api.Auth.PasswordReset;

public sealed class ResetPasswordEndpoint(IPasswordResetService passwordResetService)
    : Endpoint<ResetPasswordRequest>
{
    public override void Configure()
    {
        Post("/api/identity/auth/reset-password");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var result = await passwordResetService.ResetPasswordAsync(req.Token, req.NewPassword, ct);
        if (result == PasswordResetResult.Success)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        AddError(result switch
        {
            PasswordResetResult.TokenExpired => "Password reset token has expired.",
            PasswordResetResult.TokenAlreadyUsed => "Password reset token has already been used.",
            _ => "Password reset token is invalid."
        });
        await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
    }
}

public sealed class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequestValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}

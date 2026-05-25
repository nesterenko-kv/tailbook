using ErrorOr;

namespace Tailbook.Modules.Identity.Domain.Aggregates;

public static class IdentityErrors
{
    public static Error InvalidCredentials()
    {
        return Error.Unauthorized(
        code: "Identity.InvalidCredentials",
        description: "Invalid credentials."
    );
    }

    public static Error InvalidRefreshToken()
    {
        return Error.Unauthorized(
        code: "Identity.InvalidRefreshToken",
        description: "Refresh token is invalid or expired."
    );
    }

    public static Error BrowserSessionSurfaceRequired()
    {
        return Error.Validation(
        code: "Identity.BrowserSessionSurfaceRequired",
        description: "Browser session surface is required."
    );
    }

    public static Error InvalidBrowserSessionSurface()
    {
        return Error.Validation(
        code: "Identity.BrowserSessionSurfaceInvalid",
        description: "Browser session surface is invalid."
    );
    }

    public static Error InvalidBrowserSessionCsrf()
    {
        return Error.Forbidden(
        code: "Identity.BrowserSessionCsrfInvalid",
        description: "Browser session CSRF token is invalid."
    );
    }

    public static Error ClientPortalAccessRequired()
    {
        return Error.Unauthorized(
        code: "Identity.ClientPortalAccessRequired",
        description: "Client portal access is required."
    );
    }

    public static Error CurrentUserRequired()
    {
        return Error.Unauthorized(
        code: "Identity.CurrentUserRequired",
        description: "An authenticated user is required."
    );
    }

    public static Error UserNotFound()
    {
        return Error.NotFound(
        code: "Identity.UserNotFound",
        description: "User does not exist."
    );
    }

    public static Error UserEmailExists(string email)
    {
        return Error.Conflict(
        code: "Identity.UserEmailExists",
        description: $"User with email '{email}' already exists."
    );
    }

    public static Error MfaFactorNotFound()
    {
        return Error.NotFound(
        code: "Identity.MfaFactorNotFound",
        description: "MFA factor does not exist."
    );
    }

    public static Error InvalidMfaChallengeCode()
    {
        return Error.Validation(
        code: "Identity.MfaChallengeInvalidCode",
        description: "MFA challenge code is invalid."
    );
    }

    public static Error ExpiredMfaChallenge()
    {
        return Error.Validation(
        code: "Identity.MfaChallengeExpired",
        description: "MFA challenge has expired."
    );
    }

    public static Error MfaChallengeAttemptsExceeded()
    {
        return Error.Validation(
        code: "Identity.MfaChallengeAttemptsExceeded",
        description: "MFA challenge attempt limit has been reached."
    );
    }

    public static Error InvalidMfaRecoveryCode()
    {
        return Error.Validation(
        code: "Identity.MfaRecoveryCodeInvalid",
        description: "MFA recovery code is invalid."
    );
    }

    public static Error MfaRecoverySelfResetNotAllowed()
    {
        return Error.Validation(
        code: "Identity.MfaRecoverySelfResetNotAllowed",
        description: "Use another authorized operator to reset MFA recovery state."
    );
    }

    public static Error InvalidPasswordResetToken()
    {
        return Error.Validation(
        code: "Identity.PasswordResetTokenInvalid",
        description: "Password reset token is invalid."
    );
    }

    public static Error ExpiredPasswordResetToken()
    {
        return Error.Validation(
        code: "Identity.PasswordResetTokenExpired",
        description: "Password reset token has expired."
    );
    }

    public static Error UsedPasswordResetToken()
    {
        return Error.Validation(
        code: "Identity.PasswordResetTokenAlreadyUsed",
        description: "Password reset token has already been used."
    );
    }
}

namespace Tailbook.BuildingBlocks.Abstractions.Security;

public static class SensitivePayloadPurposes
{
    public const string PasswordResetLink = "identity.password-reset-link";
    public const string MfaEmailOtpCode = "identity.mfa-email-otp-code";
}

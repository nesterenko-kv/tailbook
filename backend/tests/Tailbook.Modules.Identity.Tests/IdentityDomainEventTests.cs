using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Identity.Domain.Events;
using Xunit;

namespace Tailbook.Modules.Identity.Tests;

public sealed class IdentityDomainEventTests
{
    [Fact]
    public void Password_reset_token_create_raises_password_reset_requested_domain_event()
    {
        var tokenId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var createdAt = Utc("2026-05-18T10:00:00Z");
        var expiresAt = Utc("2026-05-18T10:15:00Z");

        var token = IdentityPasswordResetToken.Create(
            tokenId,
            userId,
            "token-hash",
            expiresAt,
            createdAt,
            "client@example.test",
            "Client Example",
            "protected-reset-link");

        var domainEvent = Assert.IsType<PasswordResetRequestedDomainEvent>(Assert.Single(token.GetDomainEvents()));
        Assert.Equal(tokenId, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(createdAt, token.CreatedAt);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Equal(createdAt, domainEvent.OccurredAt);
        Assert.Equal("client@example.test", domainEvent.Email);
        Assert.Equal("Client Example", domainEvent.DisplayName);
        Assert.Equal("protected-reset-link", domainEvent.ProtectedResetLink);
        Assert.Equal(expiresAt, domainEvent.ExpiresAt);
        Assert.Equal("Tailbook.Modules.Identity.Integration.PasswordResetRequested", domainEvent.EventType);
        Assert.Equal("identity", domainEvent.ModuleCode);
    }

    [Fact]
    public void Email_otp_challenge_create_raises_mfa_challenge_created_domain_event()
    {
        var challengeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var userId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var factorId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var createdAt = Utc("2026-05-18T11:00:00Z");
        var expiresAt = Utc("2026-05-18T11:05:00Z");

        var challenge = IdentityMfaChallenge.CreateEmailOtp(
            challengeId,
            userId,
            factorId,
            MfaFactorTypes.EmailOtp,
            "code-hash",
            expiresAt,
            createdAt,
            "127.0.0.1",
            "test-agent",
            "client@example.test",
            "Client Example",
            "protected-code");

        var domainEvent = Assert.IsType<MfaEmailOtpChallengeCreatedDomainEvent>(Assert.Single(challenge.GetDomainEvents()));
        Assert.Equal(challengeId, challenge.Id);
        Assert.Equal(userId, challenge.UserId);
        Assert.Equal(factorId, challenge.FactorId);
        Assert.Equal(MfaFactorTypes.EmailOtp, challenge.FactorType);
        Assert.Equal(createdAt, challenge.CreatedAt);
        Assert.Equal(expiresAt, challenge.ExpiresAt);
        Assert.Equal(createdAt, domainEvent.OccurredAt);
        Assert.Equal("client@example.test", domainEvent.Email);
        Assert.Equal("Client Example", domainEvent.DisplayName);
        Assert.Equal(challengeId, domainEvent.ChallengeId);
        Assert.Equal("protected-code", domainEvent.ProtectedCode);
        Assert.Equal(expiresAt, domainEvent.ExpiresAt);
        Assert.Equal("Tailbook.Modules.Identity.Integration.MfaEmailOtpChallengeCreated", domainEvent.EventType);
        Assert.Equal("identity", domainEvent.ModuleCode);
    }

    private static DateTimeOffset Utc(string value)
    {
        return DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.AssumeUniversal);
    }
}

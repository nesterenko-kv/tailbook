using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Identity.Application.Identity.Models;
using Tailbook.Modules.Identity.Application.Identity.Services;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class MfaChallengeTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Identity_login_with_enabled_email_mfa_returns_challenge_without_tokens()
    {
        var email = $"mfa-login-{Guid.NewGuid():N}@test.local";
        var userId = await factory.SeedUserAsync(email, "MFA Login User", "OldPass123!");

        using (var setupScope = factory.Services.CreateScope())
        {
            var factorService = setupScope.ServiceProvider.GetRequiredService<IMfaFactorService>();
            var factor = await factorService.EnableEmailOtpAsync(userId, CancellationToken.None);
            Assert.False(factor.IsError, ErrorCodes(factor.Errors));
        }

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email,
            password = "OldPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal("MfaRequired", payload.Status);
        Assert.Null(payload.AccessToken);
        Assert.Null(payload.RefreshToken);
        Assert.NotNull(payload.MfaChallenge);
        Assert.NotEqual(Guid.Empty, payload.MfaChallenge.ChallengeId);
        Assert.Equal("EmailOtp", payload.MfaChallenge.FactorType);
        Assert.True(payload.MfaChallenge.ExpiresAt > TimeProvider.System.GetUtcNow());

        using var verifyScope = factory.Services.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await dbContext.Set<IdentityRefreshToken>().CountAsync(x => x.UserId == userId));
        Assert.Equal(1, await dbContext.Set<IdentityMfaChallenge>().CountAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task Verify_mfa_challenge_endpoint_returns_tokens_once()
    {
        var challenge = await CreateEnabledChallengeAsync($"mfa-endpoint-{Guid.NewGuid():N}@test.local");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/auth/mfa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = challenge.Code
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal("Authenticated", payload.Status);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Null(payload.MfaChallenge);

        var replay = await client.PostAsJsonAsync("/api/identity/auth/mfa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = challenge.Code
        });

        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task Create_email_otp_challenge_persists_only_hashed_code()
    {
        var userId = await factory.SeedUserAsync($"mfa-challenge-{Guid.NewGuid():N}@test.local", "MFA Challenge User", "OldPass123!");

        using var scope = factory.Services.CreateScope();
        var factorService = scope.ServiceProvider.GetRequiredService<IMfaFactorService>();
        var challengeService = scope.ServiceProvider.GetRequiredService<IMfaChallengeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var sensitivePayloadProtector = scope.ServiceProvider.GetRequiredService<ISensitivePayloadProtector>();

        var factor = await factorService.EnableEmailOtpAsync(userId, CancellationToken.None);
        Assert.False(factor.IsError, ErrorCodes(factor.Errors));

        var challenge = await challengeService.CreateEmailOtpChallengeAsync(
            userId,
            " 203.0.113.10 ",
            new string('a', 600),
            CancellationToken.None);

        Assert.False(challenge.IsError, ErrorCodes(challenge.Errors));
        Assert.Matches(new Regex("^\\d{6}$", RegexOptions.None, TimeSpan.FromSeconds(1)), challenge.Value.Code);
        Assert.Equal(factor.Value.Id, challenge.Value.FactorId);
        Assert.Equal(factor.Value.TargetEmail, challenge.Value.TargetEmail);

        var persisted = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == challenge.Value.ChallengeId);
        Assert.NotEqual(challenge.Value.Code, persisted.CodeHash);
        Assert.True(passwordHasher.Verify(challenge.Value.Code, persisted.CodeHash));
        Assert.Equal("203.0.113.10", persisted.RequestIpAddress);
        Assert.Equal(512, persisted.UserAgent!.Length);
        Assert.Null(persisted.ConsumedAt);
        Assert.Null(persisted.InvalidatedAt);
        Assert.Equal(0, persisted.FailedAttemptCount);

        var outbox = await dbContext.Set<OutboxMessage>()
            .SingleAsync(x => x.EventType.Contains("MfaEmailOtpChallengeCreated") && x.PayloadJson.Contains(challenge.Value.ChallengeId.ToString("D")));
        Assert.DoesNotContain(challenge.Value.Code, outbox.PayloadJson, StringComparison.Ordinal);
        using var payload = JsonDocument.Parse(outbox.PayloadJson);
        var protectedCode = payload.RootElement.GetProperty("ProtectedCode").GetString();
        Assert.False(string.IsNullOrWhiteSpace(protectedCode));
        Assert.Equal(challenge.Value.Code, sensitivePayloadProtector.Unprotect(SensitivePayloadPurposes.MfaEmailOtpCode, protectedCode!));
    }

    [Fact]
    public async Task Verify_email_otp_challenge_counts_failed_attempt_and_consumes_valid_code_once()
    {
        var challenge = await CreateEnabledChallengeAsync($"mfa-verify-{Guid.NewGuid():N}@test.local");

        using var scope = factory.Services.CreateScope();
        var challengeService = scope.ServiceProvider.GetRequiredService<IMfaChallengeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wrongCode = challenge.Code == "000000" ? "111111" : "000000";

        var wrongAttempt = await challengeService.VerifyEmailOtpChallengeAsync(challenge.ChallengeId, wrongCode, CancellationToken.None);
        Assert.True(wrongAttempt.IsError);
        Assert.Contains(wrongAttempt.Errors, x => x.Code == "Identity.MfaChallengeInvalidCode");

        var afterWrongAttempt = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == challenge.ChallengeId);
        Assert.Equal(1, afterWrongAttempt.FailedAttemptCount);
        Assert.NotNull(afterWrongAttempt.LastFailedAt);

        var verified = await challengeService.VerifyEmailOtpChallengeAsync(challenge.ChallengeId, challenge.Code, CancellationToken.None);
        Assert.False(verified.IsError, ErrorCodes(verified.Errors));
        Assert.Equal(challenge.UserId, verified.Value.UserId);

        var consumed = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == challenge.ChallengeId);
        Assert.NotNull(consumed.ConsumedAt);

        var replay = await challengeService.VerifyEmailOtpChallengeAsync(challenge.ChallengeId, challenge.Code, CancellationToken.None);
        Assert.True(replay.IsError);
        Assert.Contains(replay.Errors, x => x.Code == "Identity.MfaChallengeInvalidCode");

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            using var waitScope = factory.Services.CreateScope();
            var waitDbContext = waitScope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await waitDbContext.Set<AuditEntry>()
                .CountAsync(x => x.ModuleCode == "identity"
                                 && x.EntityType == "iam_mfa_challenge"
                                 && x.EntityId == challenge.ChallengeId.ToString("D")) >= 4;
        }, "MFA challenge audit entries were not persisted.");

        var auditEntries = await dbContext.Set<AuditEntry>()
            .Where(x => x.ModuleCode == "identity"
                        && x.EntityType == "iam_mfa_challenge"
                        && x.EntityId == challenge.ChallengeId.ToString("D"))
            .ToListAsync();
        Assert.Contains(auditEntries, x => x.ActionCode == "MFA_CHALLENGE_CREATED");
        Assert.Contains(auditEntries, x => x.ActionCode == "MFA_CHALLENGE_VERIFY_FAILED");
        Assert.Contains(auditEntries, x => x.ActionCode == "MFA_CHALLENGE_VERIFIED");
        Assert.Contains(auditEntries, x => x.ActionCode == "MFA_CHALLENGE_VERIFY_REJECTED");
        Assert.DoesNotContain(auditEntries, x => ContainsText(x.BeforeJson, challenge.Code) || ContainsText(x.AfterJson, challenge.Code));
    }

    [Fact]
    public async Task Create_email_otp_challenge_invalidates_existing_active_challenge()
    {
        var userId = await factory.SeedUserAsync($"mfa-supersede-{Guid.NewGuid():N}@test.local", "MFA Supersede User", "OldPass123!");

        using var scope = factory.Services.CreateScope();
        var factorService = scope.ServiceProvider.GetRequiredService<IMfaFactorService>();
        var challengeService = scope.ServiceProvider.GetRequiredService<IMfaChallengeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var factor = await factorService.EnableEmailOtpAsync(userId, CancellationToken.None);
        Assert.False(factor.IsError, ErrorCodes(factor.Errors));

        var first = await challengeService.CreateEmailOtpChallengeAsync(userId, null, null, CancellationToken.None);
        Assert.False(first.IsError, ErrorCodes(first.Errors));

        var second = await challengeService.CreateEmailOtpChallengeAsync(userId, null, null, CancellationToken.None);
        Assert.False(second.IsError, ErrorCodes(second.Errors));

        var firstPersisted = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == first.Value.ChallengeId);
        Assert.NotNull(firstPersisted.InvalidatedAt);

        var firstVerify = await challengeService.VerifyEmailOtpChallengeAsync(first.Value.ChallengeId, first.Value.Code, CancellationToken.None);
        Assert.True(firstVerify.IsError);
        Assert.Contains(firstVerify.Errors, x => x.Code == "Identity.MfaChallengeInvalidCode");

        var secondVerify = await challengeService.VerifyEmailOtpChallengeAsync(second.Value.ChallengeId, second.Value.Code, CancellationToken.None);
        Assert.False(secondVerify.IsError, ErrorCodes(secondVerify.Errors));
    }

    [Fact]
    public async Task Verify_email_otp_challenge_blocks_after_max_failed_attempts()
    {
        var challenge = await CreateEnabledChallengeAsync($"mfa-max-{Guid.NewGuid():N}@test.local");

        using var scope = factory.Services.CreateScope();
        var challengeService = scope.ServiceProvider.GetRequiredService<IMfaChallengeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wrongCode = challenge.Code == "999999" ? "000000" : "999999";

        for (var i = 0; i < 5; i++)
        {
            _ = await challengeService.VerifyEmailOtpChallengeAsync(challenge.ChallengeId, wrongCode, CancellationToken.None);
        }

        var persisted = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == challenge.ChallengeId);
        Assert.Equal(5, persisted.FailedAttemptCount);

        var validAfterLimit = await challengeService.VerifyEmailOtpChallengeAsync(challenge.ChallengeId, challenge.Code, CancellationToken.None);
        Assert.True(validAfterLimit.IsError);
        Assert.Contains(validAfterLimit.Errors, x => x.Code == "Identity.MfaChallengeAttemptsExceeded");
    }

    private async Task<MfaChallengeCreationResult> CreateEnabledChallengeAsync(string email)
    {
        var userId = await factory.SeedUserAsync(email, "MFA Challenge User", "OldPass123!");

        using var scope = factory.Services.CreateScope();
        var factorService = scope.ServiceProvider.GetRequiredService<IMfaFactorService>();
        var challengeService = scope.ServiceProvider.GetRequiredService<IMfaChallengeService>();

        var factor = await factorService.EnableEmailOtpAsync(userId, CancellationToken.None);
        Assert.False(factor.IsError, ErrorCodes(factor.Errors));

        var challenge = await challengeService.CreateEmailOtpChallengeAsync(userId, null, null, CancellationToken.None);
        Assert.False(challenge.IsError, ErrorCodes(challenge.Errors));
        return challenge.Value;
    }

    private static string ErrorCodes(IReadOnlyCollection<ErrorOr.Error> errors)
    {
        return string.Join(", ", errors.Select(x => x.Code));
    }

    private static bool ContainsText(string? value, string text)
    {
        return value?.Contains(text, StringComparison.Ordinal) == true;
    }

    private sealed class LoginResponseEnvelope
    {
        public string Status { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public MfaChallengeEnvelope? MfaChallenge { get; set; }
    }

    private sealed class MfaChallengeEnvelope
    {
        public Guid ChallengeId { get; set; }
        public string FactorType { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}

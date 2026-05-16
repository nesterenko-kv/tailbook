using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.Api.Tests;
using Tailbook.Api.Tests.Factories;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Application.Identity.Services;
using Xunit;

namespace Tailbook.Modules.Identity.Tests;

public sealed class MfaRecoveryCodeTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Current_user_can_generate_recovery_codes_and_view_safe_status()
    {
        var email = $"mfa-recovery-endpoint-{Guid.NewGuid():N}@test.local";
        var userId = await factory.SeedUserAsync(email, "MFA Recovery User", "OldPass123!");
        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, await factory.LoginAsAsync(email, "OldPass123!"));

        var enableResponse = await client.PostAsync("/api/identity/me/mfa/email", content: null);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        var initialStatusResponse = await client.GetAsync("/api/identity/me/mfa/recovery-codes");
        Assert.Equal(HttpStatusCode.OK, initialStatusResponse.StatusCode);
        var initialStatus = await initialStatusResponse.Content.ReadFromJsonAsync<MfaRecoveryCodeStatusEnvelope>();
        Assert.NotNull(initialStatus);
        Assert.Equal(0, initialStatus.ActiveCodeCount);
        Assert.Null(initialStatus.LastGeneratedAt);

        var generationResponse = await client.PostAsync("/api/identity/me/mfa/recovery-codes", content: null);
        Assert.Equal(HttpStatusCode.OK, generationResponse.StatusCode);
        var generation = await generationResponse.Content.ReadFromJsonAsync<MfaRecoveryCodeGenerationEnvelope>();
        Assert.NotNull(generation);
        Assert.Equal(10, generation.ActiveCodeCount);
        Assert.Equal(10, generation.RecoveryCodes.Length);
        Assert.Equal(10, generation.RecoveryCodes.Select(NormalizeRecoveryCode).Distinct(StringComparer.Ordinal).Count());

        var statusResponse = await client.GetAsync("/api/identity/me/mfa/recovery-codes");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var statusJson = await statusResponse.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<MfaRecoveryCodeStatusEnvelope>(statusJson, JsonSerializerOptions.Web);
        Assert.NotNull(status);
        Assert.Equal(10, status.ActiveCodeCount);
        Assert.NotNull(status.LastGeneratedAt);
        var precisionDiff = (generation.CreatedAt - status.LastGeneratedAt.Value).Duration();
        Assert.True(precisionDiff.TotalMilliseconds < 1, $"Expected {generation.CreatedAt:O} but got {status.LastGeneratedAt.Value:O}");
        Assert.DoesNotContain("recoveryCodes", statusJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(NormalizeRecoveryCode(generation.RecoveryCodes[0]), statusJson, StringComparison.Ordinal);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            using var waitScope = factory.Services.CreateScope();
            var waitDbContext = waitScope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await waitDbContext.Set<AuditEntry>()
                .AnyAsync(x => x.ModuleCode == "identity"
                               && x.EntityType == "iam_mfa_recovery_code_batch"
                               && x.ActionCode == "MFA_RECOVERY_CODES_GENERATED"
                               && x.ActorUserId == userId);
        }, "MFA recovery code generation audit entry was not persisted.");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntry = await dbContext.Set<AuditEntry>()
            .SingleAsync(x => x.ModuleCode == "identity"
                              && x.EntityType == "iam_mfa_recovery_code_batch"
                              && x.ActionCode == "MFA_RECOVERY_CODES_GENERATED"
                              && x.ActorUserId == userId);
        Assert.DoesNotContain(generation.RecoveryCodes, code => ContainsText(auditEntry.BeforeJson, code) || ContainsText(auditEntry.AfterJson, code));
        Assert.DoesNotContain(generation.RecoveryCodes.Select(NormalizeRecoveryCode), code => ContainsText(auditEntry.BeforeJson, code) || ContainsText(auditEntry.AfterJson, code));
    }

    [Fact]
    public async Task Anonymous_user_cannot_access_recovery_code_status()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/identity/me/mfa/recovery-codes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Recovery_code_verification_endpoint_consumes_challenge_and_code_once()
    {
        var fixture = await CreateRecoveryCodeLoginChallengeAsync($"mfa-recovery-login-{Guid.NewGuid():N}@test.local");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/auth/mfa/recovery-code/verify", new
        {
            challengeId = fixture.ChallengeId,
            recoveryCode = fixture.RecoveryCode.ToLowerInvariant()
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal("Authenticated", payload.Status);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Null(payload.MfaChallenge);

        var replay = await client.PostAsJsonAsync("/api/identity/auth/mfa/recovery-code/verify", new
        {
            challengeId = fixture.ChallengeId,
            recoveryCode = fixture.RecoveryCode
        });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            using var waitScope = factory.Services.CreateScope();
            var waitDbContext = waitScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasChallengeAudit = await waitDbContext.Set<AuditEntry>()
                .AnyAsync(x => x.ModuleCode == "identity"
                               && x.EntityType == "iam_mfa_challenge"
                               && x.EntityId == fixture.ChallengeId.ToString("D")
                               && x.ActionCode == "MFA_CHALLENGE_VERIFIED_WITH_RECOVERY_CODE");
            var hasRecoveryCodeAudit = await waitDbContext.Set<AuditEntry>()
                .AnyAsync(x => x.ModuleCode == "identity"
                               && x.EntityType == "iam_mfa_recovery_code"
                               && x.ActionCode == "MFA_RECOVERY_CODE_USED");
            return hasChallengeAudit && hasRecoveryCodeAudit;
        }, "MFA recovery-code verification audit entries were not persisted.");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == fixture.ChallengeId);
        Assert.NotNull(challenge.ConsumedAt);

        var consumedRecoveryCode = await dbContext.Set<IdentityMfaRecoveryCode>()
            .SingleAsync(x => x.ConsumedChallengeId == fixture.ChallengeId);
        Assert.NotNull(consumedRecoveryCode.ConsumedAt);
        Assert.Equal(fixture.UserId, consumedRecoveryCode.UserId);
        Assert.Equal(9, await dbContext.Set<IdentityMfaRecoveryCode>()
            .CountAsync(x => x.UserId == fixture.UserId && x.ConsumedAt == null && x.InvalidatedAt == null));

        var auditEntries = await dbContext.Set<AuditEntry>()
            .Where(x => x.ModuleCode == "identity"
                        && (x.EntityId == fixture.ChallengeId.ToString("D") || x.EntityId == consumedRecoveryCode.Id.ToString("D")))
            .ToListAsync();
        Assert.Contains(auditEntries, x => x.ActionCode == "MFA_CHALLENGE_VERIFIED_WITH_RECOVERY_CODE");
        Assert.Contains(auditEntries, x => x.ActionCode == "MFA_RECOVERY_CODE_USED");
        Assert.DoesNotContain(auditEntries, x => ContainsText(x.BeforeJson, fixture.RecoveryCode) || ContainsText(x.AfterJson, fixture.RecoveryCode));
        var normalizedRecoveryCode = NormalizeRecoveryCode(fixture.RecoveryCode);
        Assert.DoesNotContain(auditEntries, x => ContainsText(x.BeforeJson, normalizedRecoveryCode) || ContainsText(x.AfterJson, normalizedRecoveryCode));
    }

    [Fact]
    public async Task Wrong_recovery_code_verification_counts_failed_challenge_attempt()
    {
        var fixture = await CreateRecoveryCodeLoginChallengeAsync($"mfa-recovery-wrong-{Guid.NewGuid():N}@test.local");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/auth/mfa/recovery-code/verify", new
        {
            challengeId = fixture.ChallengeId,
            recoveryCode = "1111-1111-1111-1111"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == fixture.ChallengeId);
        Assert.Equal(1, challenge.FailedAttemptCount);
        Assert.NotNull(challenge.LastFailedAt);
        Assert.Equal(10, await dbContext.Set<IdentityMfaRecoveryCode>()
            .CountAsync(x => x.UserId == fixture.UserId && x.ConsumedAt == null && x.InvalidatedAt == null));
    }

    [Fact]
    public async Task Admin_can_reset_user_mfa_recovery_state()
    {
        var fixture = await CreateRecoveryCodeLoginChallengeAsync($"mfa-recovery-admin-reset-{Guid.NewGuid():N}@test.local");
        using var adminClient = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(adminClient, await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss"));

        var response = await adminClient.PostAsync($"/api/admin/iam/users/{fixture.UserId:D}/mfa/recovery/reset", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MfaRecoveryResetEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal(fixture.UserId, payload.UserId);
        Assert.Equal(1, payload.DisabledFactorCount);
        Assert.Equal(10, payload.InvalidatedRecoveryCodeCount);
        Assert.Equal(1, payload.InvalidatedChallengeCount);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            using var waitScope = factory.Services.CreateScope();
            var waitDbContext = waitScope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await waitDbContext.Set<AuditEntry>()
                .AnyAsync(x => x.ModuleCode == "identity"
                               && x.EntityType == "iam_mfa_recovery"
                               && x.EntityId == fixture.UserId.ToString("D")
                               && x.ActionCode == "MFA_RECOVERY_RESET");
        }, "MFA recovery reset audit entry was not persisted.");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.All(await dbContext.Set<IdentityMfaFactor>().Where(x => x.UserId == fixture.UserId).ToListAsync(), factor =>
        {
            Assert.Equal("Disabled", factor.Status);
            Assert.NotNull(factor.DisabledAt);
        });
        Assert.Equal(0, await dbContext.Set<IdentityMfaRecoveryCode>()
            .CountAsync(x => x.UserId == fixture.UserId && x.ConsumedAt == null && x.InvalidatedAt == null));
        Assert.NotNull((await dbContext.Set<IdentityMfaChallenge>().SingleAsync(x => x.Id == fixture.ChallengeId)).InvalidatedAt);

        var auditEntry = await dbContext.Set<AuditEntry>()
            .SingleAsync(x => x.ModuleCode == "identity"
                              && x.EntityType == "iam_mfa_recovery"
                              && x.EntityId == fixture.UserId.ToString("D")
                              && x.ActionCode == "MFA_RECOVERY_RESET");
        Assert.DoesNotContain(fixture.RecoveryCode, auditEntry.BeforeJson + auditEntry.AfterJson, StringComparison.Ordinal);
        Assert.DoesNotContain(NormalizeRecoveryCode(fixture.RecoveryCode), auditEntry.BeforeJson + auditEntry.AfterJson, StringComparison.Ordinal);

        using var targetClient = factory.CreateClient();
        var loginResponse = await targetClient.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = fixture.Email,
            password = "OldPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
        Assert.NotNull(login);
        Assert.Equal("Authenticated", login.Status);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Null(login.MfaChallenge);
    }

    [Fact]
    public async Task Admin_mfa_recovery_reset_requires_named_permission()
    {
        var targetUserId = await factory.SeedUserAsync($"mfa-recovery-reset-target-{Guid.NewGuid():N}@test.local", "MFA Recovery Target", "OldPass123!");
        var operatorEmail = $"mfa-recovery-reset-manager-{Guid.NewGuid():N}@test.local";
        await factory.SeedUserAsync(operatorEmail, "MFA Recovery Manager", "Manager123!", "manager");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, await factory.LoginAsAsync(operatorEmail, "Manager123!"));

        var response = await client.PostAsync($"/api/admin/iam/users/{targetUserId:D}/mfa/recovery/reset", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_mfa_recovery_reset_rejects_self_reset()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adminUserId = await dbContext.Set<IdentityUser>()
            .Where(x => x.Email == "admin@test.local")
            .Select(x => x.Id)
            .SingleAsync();

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss"));

        var response = await client.PostAsync($"/api/admin/iam/users/{adminUserId:D}/mfa/recovery/reset", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Current_user_without_mfa_factor_cannot_generate_recovery_codes()
    {
        var email = $"mfa-recovery-no-factor-{Guid.NewGuid():N}@test.local";
        await factory.SeedUserAsync(email, "MFA Recovery User", "OldPass123!");
        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, await factory.LoginAsAsync(email, "OldPass123!"));

        var response = await client.PostAsync("/api/identity/me/mfa/recovery-codes", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Generate_recovery_codes_requires_enabled_mfa_factor()
    {
        var userId = await factory.SeedUserAsync($"mfa-recovery-missing-factor-{Guid.NewGuid():N}@test.local", "MFA Recovery User", "OldPass123!");

        using var scope = factory.Services.CreateScope();
        var recoveryCodeService = scope.ServiceProvider.GetRequiredService<IMfaRecoveryCodeService>();

        var result = await recoveryCodeService.GenerateRecoveryCodesAsync(userId, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, x => x.Code == "Identity.MfaFactorNotFound");
    }

    [Fact]
    public async Task Generate_recovery_codes_returns_raw_codes_once_and_stores_only_hashes()
    {
        var userId = await CreateMfaEnabledUserAsync($"mfa-recovery-generate-{Guid.NewGuid():N}@test.local");

        using var scope = factory.Services.CreateScope();
        var recoveryCodeService = scope.ServiceProvider.GetRequiredService<IMfaRecoveryCodeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var codeFormat = new Regex("^[A-Z2-9]{4}(-[A-Z2-9]{4}){3}$", RegexOptions.None, TimeSpan.FromSeconds(1));

        var result = await recoveryCodeService.GenerateRecoveryCodesAsync(userId, CancellationToken.None);

        Assert.False(result.IsError, ErrorCodes(result.Errors));
        Assert.Equal(10, result.Value.ActiveCodeCount);
        Assert.Equal(10, result.Value.RecoveryCodes.Count);
        Assert.Equal(10, result.Value.RecoveryCodes.Select(NormalizeRecoveryCode).Distinct(StringComparer.Ordinal).Count());
        Assert.All(result.Value.RecoveryCodes, code => Assert.Matches(codeFormat, code));

        var persisted = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.UserId == userId)
            .ToListAsync();
        Assert.Equal(10, persisted.Count);
        Assert.All(persisted, code =>
        {
            Assert.Equal(result.Value.BatchId, code.BatchId);
            Assert.Equal(4, code.CodeSuffix.Length);
            Assert.Null(code.ConsumedAt);
            Assert.Null(code.InvalidatedAt);
        });

        foreach (var rawCode in result.Value.RecoveryCodes)
        {
            var normalizedCode = NormalizeRecoveryCode(rawCode);
            Assert.DoesNotContain(persisted, x => string.Equals(x.CodeHash, rawCode, StringComparison.Ordinal));
            Assert.DoesNotContain(persisted, x => string.Equals(x.CodeHash, normalizedCode, StringComparison.Ordinal));
            Assert.Contains(persisted, x => passwordHasher.Verify(normalizedCode, x.CodeHash));
        }

        var status = await recoveryCodeService.GetRecoveryCodeStatusAsync(userId, CancellationToken.None);
        Assert.Equal(10, status.ActiveCodeCount);
        Assert.Equal(result.Value.CreatedAt, status.LastGeneratedAt);
    }

    [Fact]
    public async Task Regenerating_recovery_codes_invalidates_existing_active_codes()
    {
        var userId = await CreateMfaEnabledUserAsync($"mfa-recovery-regenerate-{Guid.NewGuid():N}@test.local");

        using var scope = factory.Services.CreateScope();
        var recoveryCodeService = scope.ServiceProvider.GetRequiredService<IMfaRecoveryCodeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var first = await recoveryCodeService.GenerateRecoveryCodesAsync(userId, CancellationToken.None);
        Assert.False(first.IsError, ErrorCodes(first.Errors));
        var oldCode = first.Value.RecoveryCodes.First();

        var second = await recoveryCodeService.GenerateRecoveryCodesAsync(userId, CancellationToken.None);
        Assert.False(second.IsError, ErrorCodes(second.Errors));

        var previousCodes = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.BatchId == first.Value.BatchId)
            .ToListAsync();
        Assert.Equal(10, previousCodes.Count);
        Assert.All(previousCodes, code => Assert.NotNull(code.InvalidatedAt));

        var status = await recoveryCodeService.GetRecoveryCodeStatusAsync(userId, CancellationToken.None);
        Assert.Equal(10, status.ActiveCodeCount);
        Assert.Equal(second.Value.CreatedAt, status.LastGeneratedAt);

        var oldCodeConsumption = await recoveryCodeService.ConsumeRecoveryCodeAsync(userId, oldCode, Guid.NewGuid(), CancellationToken.None);
        Assert.True(oldCodeConsumption.IsError);
        Assert.Contains(oldCodeConsumption.Errors, x => x.Code == "Identity.MfaRecoveryCodeInvalid");
    }

    [Fact]
    public async Task Consume_recovery_code_marks_one_code_used_and_blocks_reuse()
    {
        var userId = await CreateMfaEnabledUserAsync($"mfa-recovery-consume-{Guid.NewGuid():N}@test.local");

        using var scope = factory.Services.CreateScope();
        var recoveryCodeService = scope.ServiceProvider.GetRequiredService<IMfaRecoveryCodeService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var generated = await recoveryCodeService.GenerateRecoveryCodesAsync(userId, CancellationToken.None);
        Assert.False(generated.IsError, ErrorCodes(generated.Errors));
        var recoveryCode = generated.Value.RecoveryCodes.First();
        var challengeId = Guid.NewGuid();

        var consumed = await recoveryCodeService.ConsumeRecoveryCodeAsync(userId, recoveryCode.ToLowerInvariant(), challengeId, CancellationToken.None);

        Assert.False(consumed.IsError, ErrorCodes(consumed.Errors));
        Assert.Equal(userId, consumed.Value.UserId);
        Assert.Equal(challengeId, consumed.Value.ChallengeId);

        var persisted = await dbContext.Set<IdentityMfaRecoveryCode>()
            .SingleAsync(x => x.Id == consumed.Value.RecoveryCodeId);
        Assert.NotNull(persisted.ConsumedAt);
        Assert.Equal(challengeId, persisted.ConsumedChallengeId);

        var status = await recoveryCodeService.GetRecoveryCodeStatusAsync(userId, CancellationToken.None);
        Assert.Equal(9, status.ActiveCodeCount);

        var replay = await recoveryCodeService.ConsumeRecoveryCodeAsync(userId, recoveryCode, challengeId, CancellationToken.None);
        Assert.True(replay.IsError);
        Assert.Contains(replay.Errors, x => x.Code == "Identity.MfaRecoveryCodeInvalid");
    }

    private async Task<Guid> CreateMfaEnabledUserAsync(string email)
    {
        var userId = await factory.SeedUserAsync(email, "MFA Recovery User", "OldPass123!");

        using var scope = factory.Services.CreateScope();
        var factorService = scope.ServiceProvider.GetRequiredService<IMfaFactorService>();
        var factor = await factorService.EnableEmailOtpAsync(userId, CancellationToken.None);
        Assert.False(factor.IsError, ErrorCodes(factor.Errors));
        return userId;
    }

    private async Task<RecoveryCodeLoginChallenge> CreateRecoveryCodeLoginChallengeAsync(string email)
    {
        var userId = await factory.SeedUserAsync(email, "MFA Recovery User", "OldPass123!");

        using (var setupClient = factory.CreateClient())
        {
            RealDbWebApplicationFactory.SetBearer(setupClient, await factory.LoginAsAsync(email, "OldPass123!"));

            var enableResponse = await setupClient.PostAsync("/api/identity/me/mfa/email", content: null);
            Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

            var generationResponse = await setupClient.PostAsync("/api/identity/me/mfa/recovery-codes", content: null);
            Assert.Equal(HttpStatusCode.OK, generationResponse.StatusCode);
            var generation = await generationResponse.Content.ReadFromJsonAsync<MfaRecoveryCodeGenerationEnvelope>();
            Assert.NotNull(generation);

            using var loginClient = factory.CreateClient();
            var loginResponse = await loginClient.PostAsJsonAsync("/api/identity/auth/login", new
            {
                email,
                password = "OldPass123!"
            });
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseEnvelope>();
            Assert.NotNull(login);
            Assert.Equal("MfaRequired", login.Status);
            Assert.NotNull(login.MfaChallenge);

            return new RecoveryCodeLoginChallenge(userId, login.MfaChallenge.ChallengeId, generation.RecoveryCodes[0], email);
        }
    }

    private static string NormalizeRecoveryCode(string code)
    {
        return new string(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private static string ErrorCodes(IReadOnlyCollection<ErrorOr.Error> errors)
    {
        return string.Join(", ", errors.Select(x => x.Code));
    }

    private static bool ContainsText(string? value, string text)
    {
        return value?.Contains(text, StringComparison.Ordinal) == true;
    }

    private sealed class MfaRecoveryCodeStatusEnvelope
    {
        public int ActiveCodeCount { get; set; }
        public DateTimeOffset? LastGeneratedAt { get; set; }
    }

    private sealed class MfaRecoveryCodeGenerationEnvelope
    {
        public string[] RecoveryCodes { get; set; } = [];
        public int ActiveCodeCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    private sealed class MfaRecoveryResetEnvelope
    {
        public Guid UserId { get; set; }
        public int DisabledFactorCount { get; set; }
        public int InvalidatedRecoveryCodeCount { get; set; }
        public int InvalidatedChallengeCount { get; set; }
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
    }

    private sealed record RecoveryCodeLoginChallenge(Guid UserId, Guid ChallengeId, string RecoveryCode, string Email);
}

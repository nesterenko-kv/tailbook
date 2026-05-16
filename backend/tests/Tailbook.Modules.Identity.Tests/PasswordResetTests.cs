using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.Api.Tests;
using Tailbook.Api.Tests.Factories;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Xunit;

namespace Tailbook.Modules.Identity.Tests;

public sealed class PasswordResetTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Password_reset_changes_password_and_revokes_existing_refresh_tokens()
    {
        var email = $"reset-{Guid.NewGuid():N}@test.local";
        const string oldPassword = "OldPass123!";
        const string newPassword = "NewPass123!";
        var userId = await factory.SeedUserAsync(email, "Reset User", oldPassword);
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new { email, password = oldPassword });
        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = await RequestResetTokenAsync(client, email);

        var resetResponse = await client.PostAsJsonAsync("/api/identity/auth/reset-password", new
        {
            token,
            newPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);
        var oldPasswordResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new { email, password = oldPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordResponse.StatusCode);

        var newPasswordResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newPasswordResponse.StatusCode);

        var oldRefreshResponse = await client.PostAsJsonAsync("/api/identity/auth/refresh", new { refreshToken = loginPayload!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);

        await TestApiHelpers.WaitUntilAsync(async () =>
        {
            using var waitScope = factory.Services.CreateScope();
            var waitDbContext = waitScope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await waitDbContext.Set<AuditEntry>()
                .CountAsync(x => x.ModuleCode == "identity" && x.EntityType == "iam_user" && x.EntityId == userId.ToString("D")) >= 2;
        }, "Password reset audit entries were not persisted.");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntries = await dbContext.Set<AuditEntry>()
            .Where(x => x.ModuleCode == "identity" && x.EntityType == "iam_user" && x.EntityId == userId.ToString("D"))
            .ToListAsync();
        Assert.Contains(auditEntries, x => x.ActionCode == "PASSWORD_RESET_REQUESTED");
        Assert.Contains(auditEntries, x => x.ActionCode == "PASSWORD_RESET_COMPLETED");
        Assert.DoesNotContain(auditEntries, x => (x.BeforeJson?.Contains(token, StringComparison.Ordinal) ?? false) || (x.AfterJson?.Contains(token, StringComparison.Ordinal) ?? false));
    }

    [Fact]
    public async Task Request_reset_does_not_enumerate_unknown_email()
    {
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await dbContext.Set<OutboxMessage>().CountAsync();

        var response = await client.PostAsJsonAsync("/api/identity/auth/request-password-reset", new
        {
            email = $"missing-{Guid.NewGuid():N}@test.local"
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var after = await dbContext.Set<OutboxMessage>().CountAsync();
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Expired_reset_token_is_rejected()
    {
        var email = $"expired-reset-{Guid.NewGuid():N}@test.local";
        await factory.SeedUserAsync(email, "Expired Reset User", "OldPass123!");
        using var client = factory.CreateClient();
        var token = await RequestResetTokenAsync(client, email);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHash = RefreshTokenService.Hash(token);
            var stored = await dbContext.Set<IdentityPasswordResetToken>().SingleAsync(x => x.TokenHash == tokenHash);
            stored.ExpiresAt = TimeProvider.System.GetUtcNow().AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        var resetResponse = await client.PostAsJsonAsync("/api/identity/auth/reset-password", new
        {
            token,
            newPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resetResponse.StatusCode);
        await AssertErrorCodeAsync(resetResponse, "Identity.PasswordResetTokenExpired");
    }

    [Fact]
    public async Task Used_reset_token_is_rejected()
    {
        var email = $"used-reset-{Guid.NewGuid():N}@test.local";
        await factory.SeedUserAsync(email, "Used Reset User", "OldPass123!");
        using var client = factory.CreateClient();
        var token = await RequestResetTokenAsync(client, email);

        var firstReset = await client.PostAsJsonAsync("/api/identity/auth/reset-password", new
        {
            token,
            newPassword = "NewPass123!"
        });
        Assert.Equal(HttpStatusCode.NoContent, firstReset.StatusCode);

        var secondReset = await client.PostAsJsonAsync("/api/identity/auth/reset-password", new
        {
            token,
            newPassword = "Another123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, secondReset.StatusCode);
        await AssertErrorCodeAsync(secondReset, "Identity.PasswordResetTokenAlreadyUsed");
    }

    [Fact]
    public async Task Weak_reset_password_is_rejected()
    {
        var email = $"weak-reset-{Guid.NewGuid():N}@test.local";
        await factory.SeedUserAsync(email, "Weak Reset User", "OldPass123!");
        using var client = factory.CreateClient();
        var token = await RequestResetTokenAsync(client, email);

        var resetResponse = await client.PostAsJsonAsync("/api/identity/auth/reset-password", new
        {
            token,
            newPassword = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resetResponse.StatusCode);
    }

    private async Task<string> RequestResetTokenAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/identity/auth/request-password-reset", new { email });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sensitivePayloadProtector = scope.ServiceProvider.GetRequiredService<ISensitivePayloadProtector>();
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(x => x.EventType.Contains("PasswordResetRequested"))
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync();
        var message = messages.First(x => x.PayloadJson.Contains(email));

        using var document = JsonDocument.Parse(message.PayloadJson);
        Assert.False(document.RootElement.TryGetProperty("ResetToken", out _));
        Assert.False(document.RootElement.TryGetProperty("ResetLink", out _));
        var protectedResetLink = document.RootElement.GetProperty("ProtectedResetLink").GetString();
        Assert.False(string.IsNullOrWhiteSpace(protectedResetLink));

        var resetLink = sensitivePayloadProtector.Unprotect(SensitivePayloadPurposes.PasswordResetLink, protectedResetLink!);
        var token = ReadTokenFromResetLink(resetLink);
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.DoesNotContain(token, message.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain(resetLink, message.PayloadJson, StringComparison.Ordinal);

        var tokenHash = RefreshTokenService.Hash(token);
        var stored = await dbContext.Set<IdentityPasswordResetToken>().SingleAsync(x => x.TokenHash == tokenHash);
        Assert.NotEqual(token, stored.TokenHash);
        return token;
    }

    private static string ReadTokenFromResetLink(string resetLink)
    {
        var uri = new Uri(resetLink);
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), "token", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        throw new InvalidOperationException("Reset link did not contain a token query parameter.");
    }

    private sealed class LoginResponse
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    private static async Task AssertErrorCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        Assert.True(document.RootElement.TryGetProperty("errors", out var errors), content);
        Assert.True(ContainsErrorCode(errors, expectedCode), content);
    }

    private static bool ContainsErrorCode(JsonElement errors, string expectedCode)
    {
        return errors.ValueKind switch
        {
            JsonValueKind.Array => errors.EnumerateArray()
                .Any(error => string.Equals(ReadProperty(error, "code"), expectedCode, StringComparison.Ordinal)),
            JsonValueKind.Object => errors.TryGetProperty(expectedCode, out _),
            _ => false
        };
    }

    private static string? ReadProperty(JsonElement element, string camelCaseName)
    {
        if (element.TryGetProperty(camelCaseName, out var camelCase))
        {
            return camelCase.GetString();
        }

        var pascalCaseName = char.ToUpperInvariant(camelCaseName[0]) + camelCaseName[1..];
        return element.TryGetProperty(pascalCaseName, out var pascalCase) ? pascalCase.GetString() : null;
    }
}

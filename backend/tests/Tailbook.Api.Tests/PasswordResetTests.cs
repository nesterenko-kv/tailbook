using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Audit.Domain;
using Tailbook.Modules.Identity.Application;
using Tailbook.Modules.Identity.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class PasswordResetTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PasswordResetTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Password_reset_changes_password_and_revokes_existing_refresh_tokens()
    {
        var email = $"reset-{Guid.NewGuid():N}@test.local";
        const string oldPassword = "OldPass123!";
        const string newPassword = "NewPass123!";
        var userId = await _factory.SeedUserAsync(email, "Reset User", oldPassword);
        using var client = _factory.CreateClient();

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

        using var scope = _factory.Services.CreateScope();
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
        using var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
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
        await _factory.SeedUserAsync(email, "Expired Reset User", "OldPass123!");
        using var client = _factory.CreateClient();
        var token = await RequestResetTokenAsync(client, email);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokenHash = RefreshTokenService.Hash(token);
            var stored = await dbContext.Set<IdentityPasswordResetToken>().SingleAsync(x => x.TokenHash == tokenHash);
            stored.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        var resetResponse = await client.PostAsJsonAsync("/api/identity/auth/reset-password", new
        {
            token,
            newPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resetResponse.StatusCode);
    }

    [Fact]
    public async Task Used_reset_token_is_rejected()
    {
        var email = $"used-reset-{Guid.NewGuid():N}@test.local";
        await _factory.SeedUserAsync(email, "Used Reset User", "OldPass123!");
        using var client = _factory.CreateClient();
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
    }

    [Fact]
    public async Task Weak_reset_password_is_rejected()
    {
        var email = $"weak-reset-{Guid.NewGuid():N}@test.local";
        await _factory.SeedUserAsync(email, "Weak Reset User", "OldPass123!");
        using var client = _factory.CreateClient();
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

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await dbContext.Set<OutboxMessage>()
            .Where(x => x.EventType.Contains("PasswordResetRequested") && x.PayloadJson.Contains(email))
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstAsync();

        using var document = JsonDocument.Parse(message.PayloadJson);
        var token = document.RootElement.GetProperty("ResetToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var tokenHash = RefreshTokenService.Hash(token!);
        var stored = await dbContext.Set<IdentityPasswordResetToken>().SingleAsync(x => x.TokenHash == tokenHash);
        Assert.NotEqual(token, stored.TokenHash);
        return token!;
    }

    private sealed class LoginResponse
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Identity.Tests;

public sealed class LoginTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Bootstrap_admin_can_login_and_receive_token()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = "admin@test.local",
            password = "MyV3ryC00lAdminP@ss"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.True(payload.RefreshTokenExpiresAt > TimeProvider.System.GetUtcNow());
        Assert.Contains("admin", payload.User.Roles);
        Assert.Contains("iam.users.read", payload.User.Permissions);
    }

    [Fact]
    public async Task Client_portal_login_rejects_non_client_identity_user()
    {
        const string password = "Manager123!";
        var email = $"manager-{Guid.NewGuid():N}@test.local";
        await factory.SeedUserAsync(email, "Portal Rejected Manager", password, "manager");

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/client/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertErrorCodeAsync(response, "Identity.ClientPortalAccessRequired");
    }

    [Fact]
    public async Task Refresh_token_can_rotate_and_revoke_identity_session()
    {
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = "admin@test.local",
            password = "MyV3ryC00lAdminP@ss"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var refreshResponse = await client.PostAsJsonAsync("/api/identity/auth/refresh", new
        {
            refreshToken = loginPayload!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshPayload);
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.RefreshToken));
        Assert.NotEqual(loginPayload.RefreshToken, refreshPayload.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/identity/auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
        await AssertErrorCodeAsync(reuseResponse, "Identity.InvalidRefreshToken");

        var revokeResponse = await client.PostAsJsonAsync("/api/identity/auth/revoke", new
        {
            refreshToken = refreshPayload.RefreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var revokedRefreshResponse = await client.PostAsJsonAsync("/api/identity/auth/refresh", new
        {
            refreshToken = refreshPayload.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, revokedRefreshResponse.StatusCode);
        await AssertErrorCodeAsync(revokedRefreshResponse, "Identity.InvalidRefreshToken");
    }

    [Fact]
    public async Task Identity_login_failure_returns_strict_error_code()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@test.local",
            password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertErrorCodeAsync(response, "Identity.InvalidCredentials");
    }

    [Fact]
    public async Task Identity_login_is_throttled_after_repeated_failures()
    {
        using var client = factory.CreateClient();
        var request = new
        {
            email = $"missing-{Guid.NewGuid():N}@test.local",
            password = "WrongPassword123!"
        };

        for (var i = 0; i < RealDbWebApplicationFactory.TestMaxFailedLoginAttempts; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/identity/auth/login", request);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/identity/auth/login", request);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
    }

    [Fact]
    public async Task Client_login_is_throttled_after_repeated_failures()
    {
        using var client = factory.CreateClient();
        var request = new
        {
            email = $"client-missing-{Guid.NewGuid():N}@test.local",
            password = "WrongPassword123!"
        };

        for (var i = 0; i < RealDbWebApplicationFactory.TestMaxFailedLoginAttempts; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/client/auth/login", request);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/client/auth/login", request);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
        public LoginUserResponse User { get; set; } = new();
    }

    private sealed class LoginUserResponse
    {
        public string[] Roles { get; set; } = [];
        public string[] Permissions { get; set; } = [];
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

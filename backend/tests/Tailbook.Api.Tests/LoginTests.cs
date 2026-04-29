using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class LoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoginTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Bootstrap_admin_can_login_and_receive_token()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new
        {
            email = "admin@test.local",
            password = "MyV3ryC00lAdminP@ss"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.True(payload.RefreshTokenExpiresAtUtc > DateTime.UtcNow);
        Assert.Contains("admin", payload.User.Roles);
        Assert.Contains("iam.users.read", payload.User.Permissions);
    }

    [Fact]
    public async Task Refresh_token_can_rotate_and_revoke_identity_session()
    {
        using var client = _factory.CreateClient();

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
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.RefreshToken));
        Assert.NotEqual(loginPayload.RefreshToken, refreshPayload.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/identity/auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

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
    }

    [Fact]
    public async Task Identity_login_is_throttled_after_repeated_failures()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            email = $"missing-{Guid.NewGuid():N}@test.local",
            password = "WrongPassword123!"
        };

        for (var i = 0; i < CustomWebApplicationFactory.TestMaxFailedLoginAttempts; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/identity/auth/login", request);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/identity/auth/login", request);

        Assert.Equal((HttpStatusCode)429, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
    }

    [Fact]
    public async Task Client_login_is_throttled_after_repeated_failures()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            email = $"client-missing-{Guid.NewGuid():N}@test.local",
            password = "WrongPassword123!"
        };

        for (var i = 0; i < CustomWebApplicationFactory.TestMaxFailedLoginAttempts; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/client/auth/login", request);
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var throttled = await client.PostAsJsonAsync("/api/client/auth/login", request);

        Assert.Equal((HttpStatusCode)429, throttled.StatusCode);
        Assert.NotNull(throttled.Headers.RetryAfter);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public LoginUserResponse User { get; set; } = new();
    }

    private sealed class LoginUserResponse
    {
        public string[] Roles { get; set; } = [];
        public string[] Permissions { get; set; } = [];
    }
}

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
            password = "Admin12345!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.Contains("admin", payload.User.Roles);
        Assert.Contains("iam.users.read", payload.User.Permissions);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public LoginUserResponse User { get; set; } = new();
    }

    private sealed class LoginUserResponse
    {
        public string[] Roles { get; set; } = [];
        public string[] Permissions { get; set; } = [];
    }
}

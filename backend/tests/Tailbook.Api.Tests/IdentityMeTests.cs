using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class IdentityMeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IdentityMeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Identity_me_returns_display_name_email_roles_and_permissions_for_admin()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IdentityMeEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal("admin@test.local", payload!.Email);
        Assert.Equal("Test Admin", payload.DisplayName);
        Assert.Contains("admin", payload.Roles, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("app.admin.access", payload.Permissions, StringComparer.OrdinalIgnoreCase);
        Assert.NotNull(payload.UserId);
    }

    [Fact]
    public async Task Identity_me_returns_groomer_identity_without_client_binding()
    {
        await _factory.SeedUserAsync("groomer.me@test.local", "Me Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer.me@test.local", "Groomer123!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IdentityMeEnvelope>();
        Assert.NotNull(payload);
        Assert.Equal("groomer.me@test.local", payload!.Email);
        Assert.Equal("Me Groomer", payload.DisplayName);
        Assert.Contains("groomer", payload.Roles, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("app.groomer.access", payload.Permissions, StringComparer.OrdinalIgnoreCase);
        Assert.Null(payload.ClientId);
        Assert.Null(payload.ContactPersonId);
    }

    private sealed class IdentityMeEnvelope
    {
        public Guid? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Guid? ClientId { get; set; }
        public Guid? ContactPersonId { get; set; }
        public string[] Roles { get; set; } = [];
        public string[] Permissions { get; set; } = [];
    }
}

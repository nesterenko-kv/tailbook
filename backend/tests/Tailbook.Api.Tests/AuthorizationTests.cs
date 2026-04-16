using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Groomer_cannot_access_admin_iam_users_endpoint()
    {
        await _factory.SeedUserAsync("groomer@test.local", "Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer@test.local", "Groomer123!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/iam/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_access_admin_iam_users_endpoint()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/iam/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

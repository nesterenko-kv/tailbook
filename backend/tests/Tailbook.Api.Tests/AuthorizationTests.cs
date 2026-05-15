using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Groomer_cannot_access_admin_iam_users_endpoint()
    {
        await factory.SeedUserAsync("groomer@test.local", "Groomer", "Groomer123!", "groomer");
        var token = await factory.LoginAsAsync("groomer@test.local", "Groomer123!");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/iam/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_access_admin_iam_users_endpoint()
    {
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/iam/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

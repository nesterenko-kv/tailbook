using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class CustomerAuthorizationTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Groomer_cannot_access_admin_clients_endpoint()
    {
        await factory.SeedUserAsync("groomer-stage2@test.local", "Groomer", "Groomer123!", "groomer");
        var token = await factory.LoginAsAsync("groomer-stage2@test.local", "Groomer123!");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/clients");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

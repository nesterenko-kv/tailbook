using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class CustomerAuthorizationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Groomer_cannot_access_admin_clients_endpoint()
    {
        await factory.SeedUserAsync("groomer-stage2@test.local", "Groomer", "Groomer123!", "groomer");
        var token = await factory.LoginAsAsync("groomer-stage2@test.local", "Groomer123!");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/clients");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

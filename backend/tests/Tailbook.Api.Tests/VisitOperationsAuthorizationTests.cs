using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class VisitOperationsAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VisitOperationsAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Groomer_cannot_access_admin_visit_endpoints()
    {
        await _factory.SeedUserAsync("visit-groomer@test.local", "Visit Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("visit-groomer@test.local", "Groomer123!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync($"/api/admin/visits/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

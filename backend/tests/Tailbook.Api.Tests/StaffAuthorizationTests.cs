using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class StaffAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StaffAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Groomer_cannot_access_admin_staff_endpoints()
    {
        await _factory.SeedUserAsync("groomer-staff@test.local", "Groomer Staff", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer-staff@test.local", "Groomer123!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/groomers");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

using System.Net;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Staff.Tests;

public sealed class StaffAuthorizationTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Groomer_cannot_access_admin_staff_endpoints()
    {
        await factory.SeedUserAsync("groomer-staff@test.local", "Groomer Staff", "Groomer123!", "groomer");
        var token = await factory.LoginAsAsync("groomer-staff@test.local", "Groomer123!");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/groomers");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

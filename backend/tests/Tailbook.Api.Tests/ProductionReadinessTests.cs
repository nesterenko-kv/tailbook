using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class ProductionReadinessTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Health_live_should_return_ok()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ready_should_return_ok()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

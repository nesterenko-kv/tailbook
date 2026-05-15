using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tailbook.Api.Tests.Smoke;

public sealed class ApiSurfaceTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_endpoint_should_be_available()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_endpoint_should_be_available_in_development()
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_endpoint_should_be_not_available_in_non_development()
    {
        var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

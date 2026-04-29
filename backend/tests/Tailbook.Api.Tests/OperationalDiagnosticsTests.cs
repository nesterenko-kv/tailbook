using System.Net;
using System.Text.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class OperationalDiagnosticsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Readiness_health_check_returns_structured_status_without_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.True(document.RootElement.TryGetProperty("totalDurationMs", out _));

        var checks = document.RootElement.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Contains(checks, check => check.GetProperty("name").GetString() == "postgresql");
        Assert.All(checks, check => Assert.True(check.TryGetProperty("errorType", out _)));
    }

    [Fact]
    public async Task Responses_include_trace_id_header_for_log_correlation()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Trace-Id", out var values));
        Assert.Contains(values, value => !string.IsNullOrWhiteSpace(value));
    }
}

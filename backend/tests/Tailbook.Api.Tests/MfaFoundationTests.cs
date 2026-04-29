using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class MfaFoundationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MfaFoundationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Current_user_can_enable_list_and_disable_email_mfa_factor()
    {
        var email = $"mfa-{Guid.NewGuid():N}@test.local";
        await _factory.SeedUserAsync(email, "MFA User", "OldPass123!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, await _factory.LoginAsAsync(email, "OldPass123!"));

        var enableResponse = await client.PostAsync("/api/identity/me/mfa/email", content: null);

        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        var enabled = await enableResponse.Content.ReadFromJsonAsync<MfaFactorResponse>();
        Assert.NotNull(enabled);
        Assert.Equal("EmailOtp", enabled!.FactorType);
        Assert.Equal("Enabled", enabled.Status);
        Assert.Equal(email, enabled.TargetEmail);

        var listResponse = await client.GetAsync("/api/identity/me/mfa/factors");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = await listResponse.Content.ReadFromJsonAsync<MfaFactorsResponse>();
        Assert.NotNull(listed);
        Assert.Contains(listed!.Items, x => x.Id == enabled.Id && x.Status == "Enabled");

        var disableResponse = await client.DeleteAsync($"/api/identity/me/mfa/factors/{enabled.Id:D}");
        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);

        var relistResponse = await client.GetAsync("/api/identity/me/mfa/factors");
        var relisted = await relistResponse.Content.ReadFromJsonAsync<MfaFactorsResponse>();
        Assert.Contains(relisted!.Items, x => x.Id == enabled.Id && x.Status == "Disabled");
    }

    [Fact]
    public async Task Enabling_email_mfa_is_idempotent_for_current_user()
    {
        var email = $"mfa-idempotent-{Guid.NewGuid():N}@test.local";
        await _factory.SeedUserAsync(email, "MFA Idempotent User", "OldPass123!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, await _factory.LoginAsAsync(email, "OldPass123!"));

        var first = await client.PostAsync("/api/identity/me/mfa/email", content: null);
        var second = await client.PostAsync("/api/identity/me/mfa/email", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstPayload = await first.Content.ReadFromJsonAsync<MfaFactorResponse>();
        var secondPayload = await second.Content.ReadFromJsonAsync<MfaFactorResponse>();
        Assert.Equal(firstPayload!.Id, secondPayload!.Id);

        var listResponse = await client.GetAsync("/api/identity/me/mfa/factors");
        var listed = await listResponse.Content.ReadFromJsonAsync<MfaFactorsResponse>();
        Assert.Single(listed!.Items, x => x.FactorType == "EmailOtp" && x.Status == "Enabled");
    }

    [Fact]
    public async Task Anonymous_user_cannot_access_mfa_factors()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/identity/me/mfa/factors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class MfaFactorsResponse
    {
        public MfaFactorResponse[] Items { get; set; } = [];
    }

    private sealed class MfaFactorResponse
    {
        public Guid Id { get; set; }
        public string FactorType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TargetEmail { get; set; } = string.Empty;
    }
}

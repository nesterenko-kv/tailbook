using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class ClientPortalFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ClientPortalFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Client_can_register_login_and_read_own_profile()
    {
        using var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName = "Portal Client",
            firstName = "Olena",
            lastName = "Portal",
            email = "portal.client@test.local",
            password = "Client123!",
            phone = "+380991112233"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<ClientLoginEnvelope>();
        Assert.NotNull(registerPayload);
        Assert.NotNull(registerPayload!.User.ClientId);
        Assert.NotNull(registerPayload.User.ContactPersonId);

        CustomWebApplicationFactory.SetBearer(client, registerPayload.AccessToken);
        var meResponse = await client.GetAsync("/api/client/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var petsResponse = await client.GetAsync("/api/client/me/pets");
        Assert.Equal(HttpStatusCode.OK, petsResponse.StatusCode);
    }

    [Fact]
    public async Task Client_contact_preferences_are_isolated_to_the_authenticated_client()
    {
        using var firstClient = _factory.CreateClient();
        var firstRegister = await firstClient.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName = "Client One",
            firstName = "Client",
            email = "client.one@test.local",
            password = "Client123!",
            instagram = "@client_one"
        });
        var firstPayload = await firstRegister.Content.ReadFromJsonAsync<ClientLoginEnvelope>();
        CustomWebApplicationFactory.SetBearer(firstClient, firstPayload!.AccessToken);

        using var secondClient = _factory.CreateClient();
        var secondRegister = await secondClient.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName = "Client Two",
            firstName = "Second",
            email = "client.two@test.local",
            password = "Client123!"
        });
        var secondPayload = await secondRegister.Content.ReadFromJsonAsync<ClientLoginEnvelope>();
        CustomWebApplicationFactory.SetBearer(secondClient, secondPayload!.AccessToken);

        var updateResponse = await firstClient.PatchAsJsonAsync("/api/client/me/contact-preferences", new
        {
            methods = new[]
            {
                new { methodType = "Email", value = "client.one@test.local", isPreferred = true },
                new { methodType = "Instagram", value = "@client_one_new", isPreferred = false }
            }
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var ownProfile = await firstClient.GetFromJsonAsync<ClientPreferencesEnvelope>("/api/client/me/contact-preferences");
        Assert.NotNull(ownProfile);
        Assert.Contains(ownProfile!.Methods, x => x.DisplayValue.Contains("client.one", StringComparison.OrdinalIgnoreCase));

        var otherProfile = await secondClient.GetFromJsonAsync<ClientPreferencesEnvelope>("/api/client/me/contact-preferences");
        Assert.NotNull(otherProfile);
        Assert.DoesNotContain(otherProfile!.Methods, x => x.DisplayValue.Contains("client.one", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ClientLoginEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
        public ClientUserEnvelope User { get; set; } = new();
    }

    private sealed class ClientUserEnvelope
    {
        public Guid? ClientId { get; set; }
        public Guid? ContactPersonId { get; set; }
    }

    private sealed class ClientPreferencesEnvelope
    {
        public PreferenceMethodEnvelope[] Methods { get; set; } = [];
    }

    private sealed class PreferenceMethodEnvelope
    {
        public string DisplayValue { get; set; } = string.Empty;
    }
}

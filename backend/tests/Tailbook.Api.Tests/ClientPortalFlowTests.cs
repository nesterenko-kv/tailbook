using System.Net;
using System.Net.Http.Json;
using Xunit;
using System.Text.Json;

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
        var ownProfile = await firstClient.GetFromJsonAsync<JsonElement>("/api/client/me/contact-preferences");
        var ownDisplayValues = ownProfile.GetProperty("methods")
            .EnumerateArray()
            .Select(x =>
            {
                if (x.TryGetProperty("displayValue", out var displayValue))
                {
                    return displayValue.GetString() ?? string.Empty;
                }

                if (x.TryGetProperty("value", out var value))
                {
                    return value.GetString() ?? string.Empty;
                }

                return string.Empty;
            })
            .ToArray();

        Assert.Contains(ownDisplayValues, x => x.Contains("client.one", StringComparison.OrdinalIgnoreCase));

        var otherProfile = await secondClient.GetFromJsonAsync<JsonElement>("/api/client/me/contact-preferences");
        var otherDisplayValues = otherProfile.GetProperty("methods")
            .EnumerateArray()
            .Select(x =>
            {
                if (x.TryGetProperty("displayValue", out var displayValue))
                {
                    return displayValue.GetString() ?? string.Empty;
                }

                if (x.TryGetProperty("value", out var value))
                {
                    return value.GetString() ?? string.Empty;
                }

                return string.Empty;
            })
            .ToArray();

        Assert.DoesNotContain(otherDisplayValues, x => x.Contains("client.one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Client_can_list_bookable_offers_for_own_pet_and_preview_a_quote()
    {
        using var portalClient = _factory.CreateClient();
        var registerResponse = await portalClient.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName = "Booking Client",
            firstName = "Booking",
            email = "booking.client@test.local",
            password = "Client123!",
            instagram = "@booking_client"
        });
        registerResponse.EnsureSuccessStatusCode();

        var portalPayload = await registerResponse.Content.ReadFromJsonAsync<ClientLoginEnvelope>();
        Assert.NotNull(portalPayload);
        Assert.NotNull(portalPayload!.User.ClientId);

        var adminToken = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var adminClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(adminClient, adminToken);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(adminClient);
        var petId = await TestApiHelpers.RegisterPetAsync(adminClient, portalPayload.User.ClientId!.Value,
            catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(adminClient, catalog.SamoyedBreedId);

        CustomWebApplicationFactory.SetBearer(portalClient, portalPayload.AccessToken);

        var offersResponse = await portalClient.GetAsync($"/api/client/booking-offers?petId={petId:D}");
        Assert.Equal(HttpStatusCode.OK, offersResponse.StatusCode);
        var offers = await offersResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(offers);

        var offer = Assert.Single(offers!);
        Assert.Equal(offerId, offer.GetProperty("id").GetGuid());

        var offerType =
            offer.TryGetProperty("offerType", out var offerTypeProp) ? offerTypeProp.GetString() :
            offer.TryGetProperty("itemType", out var itemTypeProp) ? itemTypeProp.GetString() :
            string.Empty;

        Assert.Equal("Package", offerType);
        Assert.Equal(1200m, offer.GetProperty("priceAmount").GetDecimal());
        Assert.Equal(90, offer.GetProperty("serviceMinutes").GetInt32());
        Assert.Equal(90, offer.GetProperty("reservedMinutes").GetInt32());

        var previewResponse = await portalClient.PostAsJsonAsync("/api/client/quotes/preview", new
        {
            petId,
            items = new[]
            {
                new { offerId }
            }
        });
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<ClientQuotePreviewEnvelope>();
        Assert.NotNull(preview);
        Assert.Equal("UAH", preview!.Currency);
        Assert.Equal(1200m, preview.TotalAmount);
        Assert.Equal(90, preview.ServiceMinutes);
        Assert.Equal(90, preview.ReservedMinutes);
        Assert.Single(preview.Items);
        Assert.Contains(preview.PriceLines, x => x.Amount == 1200m);
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

    private sealed class PreferenceMethodEnvelope
    {
        public string DisplayValue { get; } = string.Empty;
    }

    private sealed class ClientQuotePreviewEnvelope
    {
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
        public ClientQuotePreviewItem[] Items { get; set; } = [];
        public ClientPriceLine[] PriceLines { get; set; } = [];
    }

    private sealed class ClientQuotePreviewItem
    {
        public Guid OfferId { get; set; }
    }

    private sealed class ClientPriceLine
    {
        public decimal Amount { get; set; }
    }
}

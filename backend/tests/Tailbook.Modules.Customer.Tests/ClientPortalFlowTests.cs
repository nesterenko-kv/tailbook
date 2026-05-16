using System.Net.Http.Json;
using System.Text.Json;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Scenarios;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class ClientPortalFlowTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Client_can_register_login_and_read_own_profile()
    {
        using var client = factory.CreateAnonymousClient();

        var registerResponse = await client.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName = "Portal Client",
            firstName = "Olena",
            lastName = "Portal",
            email = "portal.client@test.local",
            password = TestUsers.ClientPassword,
            phone = "+380991112233"
        });

        registerResponse.ShouldBeCreated();
        var registerPayload = await registerResponse.ReadRequiredJsonAsync<ClientLoginEnvelope>();
        Assert.NotNull(registerPayload.User.ClientId);
        Assert.NotNull(registerPayload.User.ContactPersonId);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.RefreshToken));

        var loginResponse = await client.PostAsJsonAsync("/api/client/auth/login", new
        {
            email = "portal.client@test.local",
            password = TestUsers.ClientPassword
        });
        loginResponse.ShouldBeOk();
        var loginPayload = await loginResponse.ReadRequiredJsonAsync<ClientLoginEnvelope>();
        Assert.NotNull(loginPayload.User.ClientId);
        Assert.NotNull(loginPayload.User.ContactPersonId);

        var refreshResponse = await client.PostAsJsonAsync("/api/client/auth/refresh", new
        {
            refreshToken = loginPayload.RefreshToken
        });
        refreshResponse.ShouldBeOk();
        var refreshPayload = await refreshResponse.ReadRequiredJsonAsync<ClientLoginEnvelope>();
        Assert.NotEqual(loginPayload.RefreshToken, refreshPayload.RefreshToken);

        RealDbWebApplicationFactory.SetBearer(client, refreshPayload.AccessToken);
        var meResponse = await client.GetAsync("/api/client/me");
        meResponse.ShouldBeOk();

        var petsResponse = await client.GetAsync("/api/client/me/pets");
        petsResponse.ShouldBeOk();
    }

    [Fact]
    public async Task Client_contact_preferences_are_isolated_to_the_authenticated_client()
    {
        using var firstClient = factory.CreateAnonymousClient();
        var firstPayload = await RegisterPortalClientAsync(
            firstClient,
            displayName: "Client One",
            firstName: "Client",
            email: "client.one@test.local",
            instagram: "@client_one");
        RealDbWebApplicationFactory.SetBearer(firstClient, firstPayload.AccessToken);

        using var secondClient = factory.CreateAnonymousClient();
        var secondPayload = await RegisterPortalClientAsync(
            secondClient,
            displayName: "Client Two",
            firstName: "Second",
            email: "client.two@test.local");
        RealDbWebApplicationFactory.SetBearer(secondClient, secondPayload.AccessToken);

        var updateResponse = await firstClient.PatchAsJsonAsync("/api/client/me/contact-preferences", new
        {
            methods = new[]
            {
                new { methodType = "Email", value = "client.one@test.local", isPreferred = true },
                new { methodType = "Instagram", value = "@client_one_new", isPreferred = false }
            }
        });
        updateResponse.ShouldBeOk();

        var ownDisplayValues = await ReadContactPreferenceDisplayValuesAsync(firstClient);
        Assert.Contains(ownDisplayValues, x => x.Contains("client.one", StringComparison.OrdinalIgnoreCase));

        var otherDisplayValues = await ReadContactPreferenceDisplayValuesAsync(secondClient);
        Assert.DoesNotContain(otherDisplayValues, x => x.Contains("client.one", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Client_can_list_bookable_offers_for_own_pet_and_preview_a_quote()
    {
        using var portalClient = factory.CreateAnonymousClient();
        var portalPayload = await RegisterPortalClientAsync(
            portalClient,
            displayName: "Booking Client",
            firstName: "Booking",
            email: "booking.client@test.local",
            instagram: "@booking_client");

        using var admin = await factory.CreateAdminClientAsync();
        var pets = PetScenario.For(admin);
        var catalog = await pets.GetCatalogAsync();
        var petId = await pets.RegisterPetAsync(portalPayload.User.ClientId!.Value, catalog);
        var offerId = await CatalogScenario.For(admin).CreateSchedulableOfferAsync(catalog.SamoyedBreedId);

        RealDbWebApplicationFactory.SetBearer(portalClient, portalPayload.AccessToken);

        var offersResponse = await portalClient.GetAsync($"/api/client/booking-offers?petId={petId:D}");
        offersResponse.ShouldBeOk();
        var offers = await offersResponse.ReadRequiredJsonAsync<JsonElement[]>();

        var offer = Assert.Single(offers);
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
        previewResponse.ShouldBeOk();
        var preview = await previewResponse.ReadRequiredJsonAsync<ClientQuotePreviewEnvelope>();
        Assert.Equal("UAH", preview.Currency);
        Assert.Equal(1200m, preview.TotalAmount);
        Assert.Equal(90, preview.ServiceMinutes);
        Assert.Equal(90, preview.ReservedMinutes);
        Assert.Single(preview.Items);
        Assert.Contains(preview.PriceLines, x => x.Amount == 1200m);
    }

    [Fact]
    public async Task Client_appointments_are_filtered_by_owner_and_from_date()
    {
        var suffix = Guid.NewGuid().ToString("N");
        using var firstPortalClient = factory.CreateAnonymousClient();
        var firstPayload = await RegisterPortalClientAsync(
            firstPortalClient,
            displayName: "Appointment Client One",
            firstName: "Appointment",
            email: $"appointments.one.{suffix}@test.local");

        using var secondPortalClient = factory.CreateAnonymousClient();
        var secondPayload = await RegisterPortalClientAsync(
            secondPortalClient,
            displayName: "Appointment Client Two",
            firstName: "Appointment",
            email: $"appointments.two.{suffix}@test.local");

        using var admin = await factory.CreateAdminClientAsync();
        var pets = PetScenario.For(admin);
        var catalog = await pets.GetCatalogAsync();
        var firstPetId = await pets.RegisterPetAsync(firstPayload.User.ClientId!.Value, catalog);
        var secondPetId = await pets.RegisterPetAsync(secondPayload.User.ClientId!.Value, catalog);
        var offerId = await CatalogScenario.For(admin).CreateSchedulableOfferAsync(catalog.SamoyedBreedId);
        var groomer = await StaffScenario.For(admin).CreateSchedulableGroomerAsync();
        var booking = AdminBookingApi.For(admin);

        var oldAppointment = await booking.CreateAppointmentAsync(
            firstPetId,
            groomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2030-04-27T08:00:00Z"));
        var expectedAppointment = await booking.CreateAppointmentAsync(
            firstPetId,
            groomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2030-04-28T08:00:00Z"));
        var otherClientAppointment = await booking.CreateAppointmentAsync(
            secondPetId,
            groomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2030-04-28T10:00:00Z"));

        RealDbWebApplicationFactory.SetBearer(firstPortalClient, firstPayload.AccessToken);
        var firstResponse = await firstPortalClient.GetAsync("/api/client/appointments?from=2030-04-28T00%3A00%3A00Z");
        firstResponse.ShouldBeOk();
        var firstAppointments = await firstResponse.ReadRequiredJsonAsync<ClientAppointmentSummaryEnvelope[]>();
        var firstAppointment = Assert.Single(firstAppointments);
        Assert.Equal(expectedAppointment.Id, firstAppointment.Id);
        Assert.Equal(firstPetId, firstAppointment.PetId);
        Assert.Contains("Schedulable Package", firstAppointment.ItemLabels);
        Assert.DoesNotContain(firstAppointments, x => x.Id == oldAppointment.Id);
        Assert.DoesNotContain(firstAppointments, x => x.Id == otherClientAppointment.Id);

        RealDbWebApplicationFactory.SetBearer(secondPortalClient, secondPayload.AccessToken);
        var secondResponse = await secondPortalClient.GetAsync("/api/client/appointments?from=2030-04-28T00%3A00%3A00Z");
        secondResponse.ShouldBeOk();
        var secondAppointments = await secondResponse.ReadRequiredJsonAsync<ClientAppointmentSummaryEnvelope[]>();
        var secondAppointment = Assert.Single(secondAppointments);
        Assert.Equal(otherClientAppointment.Id, secondAppointment.Id);
        Assert.Equal(secondPetId, secondAppointment.PetId);
        Assert.DoesNotContain(secondAppointments, x => x.Id == expectedAppointment.Id);
    }

    private static async Task<ClientLoginEnvelope> RegisterPortalClientAsync(
        HttpClient client,
        string displayName,
        string firstName,
        string email,
        string? lastName = null,
        string? instagram = null,
        string? phone = null)
    {
        var response = await client.PostAsJsonAsync("/api/client/auth/register", new
        {
            displayName,
            firstName,
            lastName,
            email,
            password = TestUsers.ClientPassword,
            instagram,
            phone
        });
        response.EnsureSuccessStatusCode();

        var payload = await response.ReadRequiredJsonAsync<ClientLoginEnvelope>();
        Assert.NotNull(payload.User.ClientId);
        return payload;
    }

    private static async Task<string[]> ReadContactPreferenceDisplayValuesAsync(HttpClient client)
    {
        var profile = await client.GetFromJsonAsync<JsonElement>("/api/client/me/contact-preferences");
        return profile.GetProperty("methods")
            .EnumerateArray()
            .Select(ReadDisplayValue)
            .ToArray();
    }

    private static string ReadDisplayValue(JsonElement method)
    {
        if (method.TryGetProperty("displayValue", out var displayValue))
        {
            return displayValue.GetString() ?? string.Empty;
        }

        if (method.TryGetProperty("value", out var value))
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private sealed class ClientLoginEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public ClientUserEnvelope User { get; set; } = new();
    }

    private sealed class ClientUserEnvelope
    {
        public Guid? ClientId { get; set; }
        public Guid? ContactPersonId { get; set; }
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

    private sealed class ClientAppointmentSummaryEnvelope
    {
        public Guid Id { get; set; }
        public Guid PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string[] ItemLabels { get; set; } = [];
    }
}

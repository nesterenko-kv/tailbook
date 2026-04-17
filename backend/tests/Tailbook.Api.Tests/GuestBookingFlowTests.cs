using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class GuestBookingFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GuestBookingFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Guest_can_preview_public_booking_and_submit_request_without_registration()
    {
        var adminToken = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var adminClient = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(adminClient, adminToken);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(adminClient);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(adminClient, catalog.SamoyedBreedId);
        _ = await TestApiHelpers.CreateSchedulableGroomerAsync(adminClient);

        using var guestClient = _factory.CreateClient();
        var publicCatalog = (await guestClient.GetFromJsonAsync<JsonElement>("/api/public/pets/catalog"));
        var dogAnimalTypeId = publicCatalog.GetProperty("animalTypes").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "DOG").GetProperty("id").GetGuid();
        var doubleCoatId = publicCatalog.GetProperty("coatTypes").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "DOUBLE_COAT").GetProperty("id").GetGuid();
        var largeSizeId = publicCatalog.GetProperty("sizeCategories").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "LARGE").GetProperty("id").GetGuid();

        var offersResponse = await guestClient.PostAsJsonAsync("/api/public/booking-offers", new
        {
            pet = new
            {
                animalTypeId = dogAnimalTypeId,
                breedId = catalog.SamoyedBreedId,
                coatTypeId = doubleCoatId,
                sizeCategoryId = largeSizeId,
                petName = "Snow",
                notes = "Friendly"
            }
        });
        Assert.Equal(HttpStatusCode.OK, offersResponse.StatusCode);
        var offers = await offersResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(offers);
        Assert.Contains(offers!, x => x.GetProperty("id").GetGuid() == offerId);

        var plannerResponse = await guestClient.PostAsJsonAsync("/api/public/booking-planner", new
        {
            pet = new
            {
                animalTypeId = dogAnimalTypeId,
                breedId = catalog.SamoyedBreedId,
                coatTypeId = doubleCoatId,
                sizeCategoryId = largeSizeId,
                petName = "Snow"
            },
            localDate = "2026-04-27",
            items = new[] { new { offerId } }
        });
        Assert.Equal(HttpStatusCode.OK, plannerResponse.StatusCode);
        var planner = await plannerResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(planner.TryGetProperty("quote", out var quote));
        Assert.Equal(1200m, quote.GetProperty("totalAmount").GetDecimal());

        var requestResponse = await guestClient.PostAsJsonAsync("/api/public/booking-requests", new
        {
            pet = new
            {
                animalTypeId = dogAnimalTypeId,
                breedId = catalog.SamoyedBreedId,
                coatTypeId = doubleCoatId,
                sizeCategoryId = largeSizeId,
                petName = "Snow",
                notes = "Guest-first request"
            },
            requester = new
            {
                displayName = "Guest Customer",
                phone = "+380991112233",
                preferredContactMethodCode = "Phone"
            },
            selectionMode = "PreferredWindow",
            preferredTimes = new[]
            {
                new
                {
                    startAtUtc = DateTime.Parse("2026-04-27T08:00:00Z").ToUniversalTime(),
                    endAtUtc = DateTime.Parse("2026-04-27T10:00:00Z").ToUniversalTime(),
                    label = "Morning window"
                }
            },
            items = new[] { new { offerId } },
            notes = "Please call before confirming."
        });
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var requestPayload = await requestResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NeedsReview", requestPayload.GetProperty("status").GetString());
        Assert.Equal("PublicWidget", requestPayload.GetProperty("channel").GetString());
        Assert.True(requestPayload.GetProperty("subject").GetProperty("guestIntake").GetProperty("pet").TryGetProperty("breedName", out _));
    }
}

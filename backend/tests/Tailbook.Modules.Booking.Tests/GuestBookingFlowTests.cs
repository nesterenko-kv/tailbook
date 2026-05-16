using System.Net.Http.Json;
using System.Text.Json;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Booking.Tests;

public sealed class GuestBookingFlowTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Guest_can_preview_public_booking_and_submit_request_without_registration()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var catalog = await PetScenario.For(admin).GetCatalogAsync();
        var offerId = await CatalogScenario.For(admin).CreateSchedulableOfferAsync(catalog.SamoyedBreedId);
        _ = await StaffScenario.For(admin).CreateSchedulableGroomerAsync();

        using var guestClient = factory.CreateAnonymousClient();
        var publicCatalog = await ReadPublicPetCatalogAsync(guestClient);

        var offersResponse = await guestClient.PostAsJsonAsync("/api/public/booking-offers", new
        {
            pet = new
            {
                animalTypeId = publicCatalog.DogAnimalTypeId,
                breedId = catalog.SamoyedBreedId,
                coatTypeId = publicCatalog.DoubleCoatId,
                sizeCategoryId = publicCatalog.LargeSizeId,
                petName = "Snow",
                notes = "Friendly"
            }
        });
        offersResponse.ShouldBeOk();
        var offers = await offersResponse.ReadRequiredJsonAsync<JsonElement[]>();
        Assert.Contains(offers, x => x.GetProperty("id").GetGuid() == offerId);

        var plannerResponse = await guestClient.PostAsJsonAsync("/api/public/booking-planner", new
        {
            pet = new
            {
                animalTypeId = publicCatalog.DogAnimalTypeId,
                breedId = catalog.SamoyedBreedId,
                coatTypeId = publicCatalog.DoubleCoatId,
                sizeCategoryId = publicCatalog.LargeSizeId,
                petName = "Snow"
            },
            localDate = "2026-04-27",
            items = new[] { new { offerId } }
        });
        plannerResponse.ShouldBeOk();
        var planner = await plannerResponse.ReadRequiredJsonAsync<JsonElement>();
        Assert.True(planner.TryGetProperty("quote", out var quote));
        Assert.Equal(1200m, quote.GetProperty("totalAmount").GetDecimal());

        var requestResponse = await guestClient.PostAsJsonAsync("/api/public/booking-requests", new
        {
            pet = new
            {
                animalTypeId = publicCatalog.DogAnimalTypeId,
                breedId = catalog.SamoyedBreedId,
                coatTypeId = publicCatalog.DoubleCoatId,
                sizeCategoryId = publicCatalog.LargeSizeId,
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
                    startAt = ApiClientExtensions.UtcDateTime("2026-04-27T08:00:00Z"),
                    endAt = ApiClientExtensions.UtcDateTime("2026-04-27T10:00:00Z"),
                    label = "Morning window"
                }
            },
            items = new[] { new { offerId } },
            notes = "Please call before confirming."
        });
        requestResponse.ShouldBeCreated();
        var requestPayload = await requestResponse.ReadRequiredJsonAsync<JsonElement>();
        Assert.Equal("NeedsReview", requestPayload.GetProperty("status").GetString());
        Assert.Equal("PublicWidget", requestPayload.GetProperty("channel").GetString());
        Assert.True(requestPayload.GetProperty("subject").GetProperty("guestIntake").GetProperty("pet").TryGetProperty("breedName", out _));
    }

    [Fact]
    public async Task Public_booking_planner_excludes_slots_overlapping_existing_appointments()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync($"Planner Client {Guid.NewGuid():N}");
        var offerId = await CatalogScenario.For(admin).CreateSchedulableOfferAsync(pet.Catalog.SamoyedBreedId);
        var groomer = await StaffScenario.For(admin).CreateSchedulableGroomerAsync();
        var blockedStartAt = ApiClientExtensions.UtcDateTime("2030-05-01T08:00:00Z");
        _ = await AdminBookingApi.For(admin).CreateAppointmentAsync(pet.PetId, groomer.Id, offerId, blockedStartAt);

        using var guestClient = factory.CreateAnonymousClient();
        var publicCatalog = await ReadPublicPetCatalogAsync(guestClient);

        var plannerResponse = await guestClient.PostAsJsonAsync("/api/public/booking-planner", new
        {
            pet = new
            {
                animalTypeId = publicCatalog.DogAnimalTypeId,
                breedId = pet.Catalog.SamoyedBreedId,
                coatTypeId = publicCatalog.DoubleCoatId,
                sizeCategoryId = publicCatalog.LargeSizeId,
                petName = "Snow"
            },
            localDate = "2030-05-01",
            items = new[] { new { offerId } }
        });

        plannerResponse.ShouldBeOk();
        var planner = await plannerResponse.ReadRequiredJsonAsync<JsonElement>();
        Assert.Equal(1200m, planner.GetProperty("quote").GetProperty("totalAmount").GetDecimal());

        var anySuitableSlots = planner.GetProperty("anySuitableSlots").EnumerateArray().ToArray();
        Assert.NotEmpty(anySuitableSlots);
        foreach (var slot in anySuitableSlots.Where(slot => slot.GetProperty("startAt").GetDateTimeOffset() == blockedStartAt))
        {
            Assert.DoesNotContain(slot.GetProperty("groomerIds").EnumerateArray(), id => id.GetGuid() == groomer.Id);
        }

        var groomerView = planner.GetProperty("groomers")
            .EnumerateArray()
            .Single(x => x.GetProperty("groomerId").GetGuid() == groomer.Id);
        Assert.True(groomerView.GetProperty("canTakeRequest").GetBoolean());
        var groomerSlots = groomerView.GetProperty("slots").EnumerateArray().ToArray();
        Assert.NotEmpty(groomerSlots);
        Assert.DoesNotContain(groomerSlots, slot => slot.GetProperty("startAt").GetDateTimeOffset() == blockedStartAt);
    }

    private static async Task<PublicPetCatalogIds> ReadPublicPetCatalogAsync(HttpClient client)
    {
        var publicCatalog = await client.GetFromJsonAsync<JsonElement>("/api/public/pets/catalog");

        return new PublicPetCatalogIds(
            DogAnimalTypeId: publicCatalog.GetProperty("animalTypes").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "DOG").GetProperty("id").GetGuid(),
            DoubleCoatId: publicCatalog.GetProperty("coatTypes").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "DOUBLE_COAT").GetProperty("id").GetGuid(),
            LargeSizeId: publicCatalog.GetProperty("sizeCategories").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "LARGE").GetProperty("id").GetGuid());
    }

    private sealed record PublicPetCatalogIds(Guid DogAnimalTypeId, Guid DoubleCoatId, Guid LargeSizeId);
}

using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Assertions;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;
using Tailbook.Api.Tests.TestSupport.Scenarios;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class StaffSchedulingFlowTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_create_groomer_configure_schedule_and_detect_blocked_availability()
    {
        using var admin = await factory.CreateAdminClientAsync();
        var staff = StaffScenario.For(admin);

        var offer = await CatalogScenario.For(admin).CreateOfferAsync(
            CatalogScenario.UniqueCode("SCHED"),
            "StandaloneService",
            "Schedule Check");
        var groomer = await staff.CreateGroomerAsync("Iryna");

        var scheduleResponse = await staff.AddWorkingScheduleResponseAsync(groomer.Id, weekday: 1);
        scheduleResponse.ShouldBeCreated();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync("Schedule Client", "Staff scheduling pet");

        var available = await staff.CheckAvailabilityAsync(
            groomer.Id,
            pet.PetId,
            startAt: ApiClientExtensions.UtcDateTime("2026-04-20T07:00:00Z"),
            reservedMinutes: 90,
            offerIds: [offer.Id]);

        available.ShouldBeAvailableUntil(ApiClientExtensions.UtcDateTime("2026-04-20T08:30:00Z"));

        var blockResponse = await staff.AddTimeBlockResponseAsync(
            groomer.Id,
            startAt: ApiClientExtensions.UtcDateTime("2026-04-20T07:30:00Z"),
            endAt: ApiClientExtensions.UtcDateTime("2026-04-20T08:15:00Z"),
            reasonCode: "LUNCH",
            notes: "Lunch overlap");
        blockResponse.ShouldBeCreated();

        var blocked = await staff.CheckAvailabilityAsync(
            groomer.Id,
            pet.PetId,
            startAt: ApiClientExtensions.UtcDateTime("2026-04-20T07:00:00Z"),
            reservedMinutes: 90,
            offerIds: [offer.Id]);

        blocked.ShouldBeUnavailableBecause("blocked time");

        var schedule = await staff.GetScheduleAsync(
            groomer.Id,
            from: "2026-04-20T00:00:00Z",
            to: "2026-04-21T00:00:00Z");
        Assert.Single(schedule.TimeBlocks);
        Assert.NotEmpty(schedule.AvailabilityWindows);
    }

    [Fact]
    public async Task Groomer_capability_modifier_is_applied_to_quote_preview_reserved_duration()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync("Modifier Client", "Staff scheduling pet");
        var catalog = pet.Catalog;
        var catalogApi = CatalogScenario.For(admin);

        var offer = await catalogApi.CreateOfferAsync(CatalogScenario.UniqueCode("MOD"), "Package", "Modifier Package");
        var procedure = await catalogApi.CreateProcedureAsync(CatalogScenario.UniqueCode("MODP"), "Modifier Procedure");
        var version = await catalogApi.CreateVersionAsync(offer.Id);
        await catalogApi.AddComponentAsync(version.Id, procedure.Id);
        await catalogApi.PublishOfferVersionAsync(version.Id);

        var priceRuleSet = await catalogApi.CreatePriceRuleSetAsync();
        await catalogApi.AddPriceRuleAsync(priceRuleSet.Id, offer.Id, 100, 1200m, catalog.SamoyedBreedId, null);
        await catalogApi.PublishPriceRuleSetAsync(priceRuleSet.Id);

        var durationRuleSet = await catalogApi.CreateDurationRuleSetAsync();
        await catalogApi.AddDurationRuleAsync(durationRuleSet.Id, offer.Id, 100, 90, 5, 10, catalog.SamoyedBreedId, null);
        await catalogApi.PublishDurationRuleSetAsync(durationRuleSet.Id);

        var staff = StaffScenario.For(admin);
        var groomer = await staff.CreateGroomerAsync("Oksana");
        await staff.AddCapabilityAsync(
            groomer.Id,
            catalog.SamoyedBreedId,
            offer.Id,
            capabilityMode: "Allow",
            reservedDurationModifierMinutes: 15);

        var previewResponse = await admin.PostAsJsonAsync("/api/admin/quotes/preview", new
        {
            petId = pet.PetId,
            groomerId = groomer.Id,
            items = new[]
            {
                new { itemType = "Package", offerId = offer.Id }
            }
        });

        previewResponse.ShouldBeOk();
        var preview = await previewResponse.ReadRequiredJsonAsync<PreviewQuoteEnvelope>();
        Assert.Equal(120, preview.DurationSnapshot.ReservedMinutes);
        Assert.Contains(preview.DurationSnapshot.Lines, x => x.LineType == "GroomerCapabilityModifier");
    }

    [Fact]
    public async Task Deny_capability_blocks_availability_check_with_bad_request()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync("Blocked Client", "Staff scheduling pet");
        var offer = await CatalogScenario.For(admin).CreateOfferAsync(
            CatalogScenario.UniqueCode("DENY"),
            "StandaloneService",
            "Deny Check");

        var staff = StaffScenario.For(admin);
        var groomer = await staff.CreateGroomerAsync("Denied Groomer");
        await staff.AddWorkingScheduleAsync(groomer.Id, weekday: 1);

        var denyResponse = await staff.AddCapabilityResponseAsync(
            groomer.Id,
            pet.Catalog.SamoyedBreedId,
            offer.Id,
            capabilityMode: "Deny",
            reservedDurationModifierMinutes: 0);
        denyResponse.ShouldBeCreated();

        var availabilityResponse = await staff.CheckAvailabilityResponseAsync(
            groomer.Id,
            pet.PetId,
            startAt: ApiClientExtensions.UtcDateTime("2026-04-20T07:00:00Z"),
            reservedMinutes: 90,
            offerIds: [offer.Id]);

        availabilityResponse.ShouldBeBadRequest();
    }

    [Fact]
    public async Task Admin_can_list_groomers_in_items_envelope()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var created = await StaffScenario.For(admin).CreateGroomerAsync("Envelope Groomer");

        var response = await admin.GetAsync("/api/admin/groomers");
        response.ShouldBeOk();

        var payload = await response.ReadRequiredJsonAsync<ListGroomersEnvelope>();
        Assert.NotEmpty(payload.Items);
        Assert.Contains(payload.Items, x => x.Id == created.Id);
    }

    private sealed class ListGroomersEnvelope
    {
        public GroomerListItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class GroomerListItemEnvelope
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}

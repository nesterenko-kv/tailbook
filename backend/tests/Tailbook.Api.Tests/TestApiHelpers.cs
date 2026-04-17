using System.Net.Http.Json;

namespace Tailbook.Api.Tests;

internal static class TestApiHelpers
{
    internal static async Task<PetCatalogSelection> GetPetCatalogAsync(HttpClient client)
    {
        var payload = (await client.GetFromJsonAsync<PetCatalogEnvelope>("/api/admin/pets/catalog"))!;
        return new PetCatalogSelection(
            payload.AnimalTypes.Single(x => x.Code == "DOG").Code,
            payload.CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code,
            payload.SizeCategories.Single(x => x.Code == "LARGE").Code,
            payload.Breeds.Single(x => x.Code == "SAMOYED").Id);
    }

    internal static async Task<Guid> CreateClientAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ClientEnvelope>();
        return payload!.Id;
    }

    internal static async Task<Guid> RegisterPetAsync(HttpClient client, Guid clientId, Guid breedId, string animalTypeCode, string coatTypeCode, string sizeCategoryCode)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId,
            name = "Milo",
            animalTypeCode,
            breedId,
            coatTypeCode,
            sizeCategoryCode,
            notes = "Stage 11 pet"
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PetEnvelope>();
        return payload!.Id;
    }

    internal static async Task<Guid> CreateSchedulableOfferAsync(HttpClient client, Guid breedId)
    {
        var offerResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code = $"PKG_{Guid.NewGuid():N}"[..12], offerType = "Package", displayName = "Schedulable Package" });
        offerResponse.EnsureSuccessStatusCode();
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferEnvelope>();

        var procedureResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"PROC_{Guid.NewGuid():N}"[..13], name = "Procedure" });
        procedureResponse.EnsureSuccessStatusCode();
        var procedure = await procedureResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var versionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offer!.Id:D}/versions", new { offerId = offer.Id });
        versionResponse.EnsureSuccessStatusCode();
        var version = await versionResponse.Content.ReadFromJsonAsync<OfferVersionEnvelope>();

        (await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/components", new { versionId = version.Id, procedureId = procedure!.Id, componentRole = "Included", sequenceNo = 1, defaultExpected = true })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/publish", new { versionId = version.Id })).EnsureSuccessStatusCode();

        var priceRuleSet = await client.PostAsJsonAsync("/api/admin/pricing/rule-sets", new { });
        priceRuleSet.EnsureSuccessStatusCode();
        var priceRuleSetPayload = await priceRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();
        (await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload!.Id:D}/rules", new { ruleSetId = priceRuleSetPayload.Id, offerId = offer.Id, priority = 100, fixedAmount = 1200m, currency = "UAH", breedId })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload.Id:D}/publish", new { ruleSetId = priceRuleSetPayload.Id })).EnsureSuccessStatusCode();

        var durationRuleSet = await client.PostAsJsonAsync("/api/admin/duration/rule-sets", new { });
        durationRuleSet.EnsureSuccessStatusCode();
        var durationRuleSetPayload = await durationRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();
        (await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload!.Id:D}/rules", new { ruleSetId = durationRuleSetPayload.Id, offerId = offer.Id, priority = 100, baseMinutes = 90, bufferBeforeMinutes = 0, bufferAfterMinutes = 0, breedId })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload.Id:D}/publish", new { ruleSetId = durationRuleSetPayload.Id })).EnsureSuccessStatusCode();

        return offer.Id;
    }

    internal static async Task<GroomerEnvelope> CreateSchedulableGroomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/groomers", new { displayName = "Stage 11 Groomer" });
        response.EnsureSuccessStatusCode();
        var groomer = await response.Content.ReadFromJsonAsync<GroomerEnvelope>();

        foreach (var weekday in new[] { 1, 2, 3, 4, 5 })
        {
            (await client.PostAsJsonAsync($"/api/admin/groomers/{groomer!.Id:D}/working-schedules", new { groomerId = groomer.Id, weekday, startLocalTime = "09:00", endLocalTime = "18:00" })).EnsureSuccessStatusCode();
        }

        return groomer!;
    }

    internal static async Task<AppointmentEnvelope> CreateAppointmentAsync(HttpClient client, Guid petId, Guid groomerId, Guid offerId, DateTime startAtUtc)
    {
        var response = await client.PostAsJsonAsync("/api/admin/appointments", new { petId, groomerId, startAtUtc, items = new[] { new { offerId, itemType = "Package" } } });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentEnvelope>())!;
    }

    internal static async Task<VisitEnvelope> CheckInAsync(HttpClient client, Guid appointmentId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/appointments/{appointmentId:D}/check-in", new { appointmentId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VisitEnvelope>())!;
    }

    internal sealed record PetCatalogSelection(string DogAnimalTypeCode, string DoubleCoatCode, string LargeSizeCode, Guid SamoyedBreedId);

    internal sealed class PetCatalogEnvelope
    {
        public AnimalTypeEnvelope[] AnimalTypes { get; set; } = [];
        public BreedEnvelope[] Breeds { get; set; } = [];
        public CoatTypeEnvelope[] CoatTypes { get; set; } = [];
        public SizeCategoryEnvelope[] SizeCategories { get; set; } = [];
    }

    internal sealed class AnimalTypeEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    internal sealed class BreedEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    internal sealed class CoatTypeEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    internal sealed class SizeCategoryEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    internal sealed class ClientEnvelope { public Guid Id { get; set; } }
    internal sealed class PetEnvelope { public Guid Id { get; set; } }
    internal sealed class OfferEnvelope { public Guid Id { get; set; } }
    internal sealed class ProcedureEnvelope { public Guid Id { get; set; } }
    internal sealed class OfferVersionEnvelope { public Guid Id { get; set; } }
    internal sealed class RuleSetEnvelope { public Guid Id { get; set; } }
    internal sealed class GroomerEnvelope { public Guid Id { get; set; } }
    internal sealed class AppointmentEnvelope { public Guid Id { get; set; } }
    internal sealed class VisitEnvelope { public Guid Id { get; set; } }
}

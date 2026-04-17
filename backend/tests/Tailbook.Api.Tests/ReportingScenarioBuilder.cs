using System.Net.Http.Json;

namespace Tailbook.Api.Tests;

internal static class ReportingScenarioBuilder
{
    public static async Task<ReportingScenarioResult> CreateClosedVisitAsync(HttpClient client)
    {
        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Reporting Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerSetup = await CreateVisitReadyOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await CreateSchedulableGroomerAsync(client);
        var appointment = await CreateAppointmentAsync(client, petId, groomer.Id, offerSetup.OfferId);

        var checkInResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{appointment.Id:D}/check-in", new { appointmentId = appointment.Id });
        checkInResponse.EnsureSuccessStatusCode();
        var visit = (await checkInResponse.Content.ReadFromJsonAsync<VisitEnvelope>())!;
        var executionItem = visit.Items.Single();
        var expectedComponent = executionItem.ExpectedComponents.First();

        var performedResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/performed-procedures", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            procedureId = offerSetup.SecondProcedureId,
            note = "Completed as expected."
        });
        performedResponse.EnsureSuccessStatusCode();

        var skippedResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/skipped-components", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            offerVersionComponentId = expectedComponent.Id,
            omissionReasonCode = "PET_STRESSED",
            note = "Skipped one included step."
        });
        skippedResponse.EnsureSuccessStatusCode();

        var adjustmentResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/adjustments", new
        {
            visitId = visit.Id,
            sign = -1,
            amount = 150,
            reasonCode = "CALMER_THAN_EXPECTED",
            note = "Applied goodwill reduction."
        });
        adjustmentResponse.EnsureSuccessStatusCode();

        var completeResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/complete", new { visitId = visit.Id });
        completeResponse.EnsureSuccessStatusCode();

        var closeResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/close", new { visitId = visit.Id });
        closeResponse.EnsureSuccessStatusCode();
        var closedVisit = (await closeResponse.Content.ReadFromJsonAsync<VisitEnvelope>())!;

        return new ReportingScenarioResult(closedVisit.Id, offerSetup.OfferId);
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ClientEnvelope>();
        return payload!.Id;
    }

    private static async Task<PetCatalogEnvelope> GetPetCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/pets/catalog");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetCatalogEnvelope>())!;
    }

    private static async Task<Guid> RegisterPetAsync(HttpClient client, Guid clientId, Guid breedId, string animalTypeCode, string coatTypeCode, string sizeCategoryCode)
    {
        var response = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId,
            name = "Milo",
            animalTypeCode,
            breedId,
            coatTypeCode,
            sizeCategoryCode
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<PetEnvelope>();
        return payload!.Id;
    }

    private static async Task<(Guid OfferId, Guid SecondProcedureId)> CreateVisitReadyOfferAsync(HttpClient client, Guid breedId)
    {
        var offerResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code = $"RPT_{Guid.NewGuid():N}"[..14], offerType = "Package", displayName = "Report Package" });
        offerResponse.EnsureSuccessStatusCode();
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferEnvelope>();

        var procedureOneResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"RPROC_{Guid.NewGuid():N}"[..14], name = "Bathing" });
        procedureOneResponse.EnsureSuccessStatusCode();
        var procedureOne = await procedureOneResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var procedureTwoResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"RPROC_{Guid.NewGuid():N}"[..14], name = "Drying" });
        procedureTwoResponse.EnsureSuccessStatusCode();
        var procedureTwo = await procedureTwoResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var versionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offer!.Id:D}/versions", new { offerId = offer.Id });
        versionResponse.EnsureSuccessStatusCode();
        var version = await versionResponse.Content.ReadFromJsonAsync<OfferVersionEnvelope>();

        foreach (var tuple in new[] { (procedureOne!.Id, 1), (procedureTwo!.Id, 2) })
        {
            var componentResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/components", new
            {
                versionId = version.Id,
                procedureId = tuple.Id,
                componentRole = "Included",
                sequenceNo = tuple.Item2,
                defaultExpected = true
            });
            componentResponse.EnsureSuccessStatusCode();
        }

        var publishVersionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/publish", new { versionId = version.Id });
        publishVersionResponse.EnsureSuccessStatusCode();

        var priceRuleSet = await client.PostAsJsonAsync("/api/admin/pricing/rule-sets", new { });
        priceRuleSet.EnsureSuccessStatusCode();
        var priceRuleSetPayload = await priceRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();

        var priceRule = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload!.Id:D}/rules", new
        {
            ruleSetId = priceRuleSetPayload.Id,
            offerId = offer.Id,
            priority = 100,
            fixedAmount = 1500,
            currency = "UAH",
            breedId
        });
        priceRule.EnsureSuccessStatusCode();
        var pricePublish = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload.Id:D}/publish", new { ruleSetId = priceRuleSetPayload.Id });
        pricePublish.EnsureSuccessStatusCode();

        var durationRuleSet = await client.PostAsJsonAsync("/api/admin/duration/rule-sets", new { });
        durationRuleSet.EnsureSuccessStatusCode();
        var durationRuleSetPayload = await durationRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();

        var durationRule = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload!.Id:D}/rules", new
        {
            ruleSetId = durationRuleSetPayload.Id,
            offerId = offer.Id,
            priority = 100,
            baseMinutes = 120,
            bufferBeforeMinutes = 10,
            bufferAfterMinutes = 15,
            breedId
        });
        durationRule.EnsureSuccessStatusCode();
        var durationPublish = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload.Id:D}/publish", new { ruleSetId = durationRuleSetPayload.Id });
        durationPublish.EnsureSuccessStatusCode();

        return (offer.Id, procedureTwo!.Id);
    }

    private static async Task<GroomerEnvelope> CreateSchedulableGroomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/groomers", new { displayName = "Report Groomer", active = true });
        response.EnsureSuccessStatusCode();
        var groomer = await response.Content.ReadFromJsonAsync<GroomerEnvelope>();

        foreach (var weekday in Enumerable.Range(1, 7))
        {
            var scheduleResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer!.Id:D}/working-schedules", new
            {
                groomerId = groomer.Id,
                weekday,
                startLocalTime = "08:00",
                endLocalTime = "18:00"
            });
            scheduleResponse.EnsureSuccessStatusCode();
        }

        return groomer!;
    }

    private static async Task<AppointmentEnvelope> CreateAppointmentAsync(HttpClient client, Guid petId, Guid groomerId, Guid offerId)
    {
        var appointmentStartAtUtc = DateTime.UtcNow.Date.AddDays(-1).AddHours(10);

        var response = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId,
            groomerId,
            startAtUtc = appointmentStartAtUtc,
            items = new[] { new { itemType = "Package", offerId } }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentEnvelope>())!;
    }

    internal sealed record ReportingScenarioResult(Guid VisitId, Guid OfferId);

    private sealed class ClientEnvelope { public Guid Id { get; set; } }
    private sealed class PetEnvelope { public Guid Id { get; set; } }
    private sealed class OfferEnvelope { public Guid Id { get; set; } }
    private sealed class ProcedureEnvelope { public Guid Id { get; set; } }
    private sealed class OfferVersionEnvelope { public Guid Id { get; set; } }
    private sealed class RuleSetEnvelope { public Guid Id { get; set; } }
    private sealed class GroomerEnvelope { public Guid Id { get; set; } public string DisplayName { get; set; } = string.Empty; }
    private sealed class AppointmentEnvelope { public Guid Id { get; set; } public string Status { get; set; } = string.Empty; }

    private sealed class PetCatalogEnvelope
    {
        public AnimalTypeEnvelope[] AnimalTypes { get; set; } = [];
        public BreedEnvelope[] Breeds { get; set; } = [];
        public CoatTypeEnvelope[] CoatTypes { get; set; } = [];
        public SizeCategoryEnvelope[] SizeCategories { get; set; } = [];

        public Guid SamoyedBreedId => Breeds.Single(x => x.Code == "SAMOYED").Id;
        public Guid DogAnimalTypeId => AnimalTypes.Single(x => x.Code == "DOG").Id;
        public string DogAnimalTypeCode => AnimalTypes.Single(x => x.Code == "DOG").Code;
        public string DoubleCoatCode => CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code;
        public string LargeSizeCode => SizeCategories.Single(x => x.Code == "LARGE").Code;
    }

    private sealed class AnimalTypeEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    private sealed class BreedEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    private sealed class CoatTypeEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }
    private sealed class SizeCategoryEnvelope { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; }

    private sealed class VisitEnvelope
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal FinalTotalAmount { get; set; }
        public VisitExecutionItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class VisitExecutionItemEnvelope
    {
        public Guid Id { get; set; }
        public VisitExpectedComponentEnvelope[] ExpectedComponents { get; set; } = [];
    }

    private sealed class VisitExpectedComponentEnvelope
    {
        public Guid Id { get; set; }
    }
}

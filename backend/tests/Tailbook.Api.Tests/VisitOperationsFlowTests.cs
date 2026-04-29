using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class VisitOperationsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public VisitOperationsFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_check_in_execute_adjust_complete_and_close_visit()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Visit Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerSetup = await CreateVisitReadyOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await CreateSchedulableGroomerAsync(client);
        var appointment = await CreateAppointmentAsync(client, petId, groomer.Id, offerSetup.OfferId);

        var checkInResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{appointment.Id:D}/check-in", new { appointmentId = appointment.Id });
        Assert.Equal(HttpStatusCode.Created, checkInResponse.StatusCode);
        var visit = await checkInResponse.Content.ReadFromJsonAsync<VisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal("Open", visit!.Status);
        Assert.Single(visit.Items);
        Assert.Equal("CheckedIn", await GetAppointmentStatusAsync(client, appointment.Id));

        var executionItem = visit.Items.Single();
        var expectedComponent = executionItem.ExpectedComponents.First();

        var performedResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/performed-procedures", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            procedureId = offerSetup.SecondProcedureId,
            note = "Completed as expected."
        });
        Assert.Equal(HttpStatusCode.OK, performedResponse.StatusCode);
        visit = await performedResponse.Content.ReadFromJsonAsync<VisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal("InProgress", visit!.Status);
        Assert.Equal("InProgress", await GetAppointmentStatusAsync(client, appointment.Id));

        var skippedResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/skipped-components", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            offerVersionComponentId = expectedComponent.Id,
            omissionReasonCode = "PET_STRESSED",
            note = "Skipped one included step."
        });
        Assert.Equal(HttpStatusCode.OK, skippedResponse.StatusCode);
        visit = await skippedResponse.Content.ReadFromJsonAsync<VisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Single(visit!.Items.Single().SkippedComponents);

        var adjustmentResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/adjustments", new
        {
            visitId = visit.Id,
            sign = -1,
            amount = 150,
            reasonCode = "CALMER_THAN_EXPECTED",
            note = "Applied goodwill reduction."
        });
        Assert.Equal(HttpStatusCode.OK, adjustmentResponse.StatusCode);
        visit = await adjustmentResponse.Content.ReadFromJsonAsync<VisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal(1350m, visit!.FinalTotalAmount);

        var adjustmentAuditResponse = await client.GetAsync($"/api/admin/audit?moduleCode=visitops&entityType=visit&entityId={visit.Id:D}");
        Assert.Equal(HttpStatusCode.OK, adjustmentAuditResponse.StatusCode);
        var adjustmentAudit = await adjustmentAuditResponse.Content.ReadFromJsonAsync<AuditTrailEnvelope>();
        Assert.Contains(adjustmentAudit!.Items, x => x.ActionCode == "APPLY_ADJUSTMENT");

        var completeResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/complete", new { visitId = visit.Id });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        visit = await completeResponse.Content.ReadFromJsonAsync<VisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal("AwaitingFinalization", visit!.Status);
        Assert.Equal("Completed", await GetAppointmentStatusAsync(client, appointment.Id));

        var closeResponse = await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/close", new { visitId = visit.Id });
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);
        visit = await closeResponse.Content.ReadFromJsonAsync<VisitEnvelope>();
        Assert.NotNull(visit);
        Assert.Equal("Closed", visit!.Status);
        Assert.Equal("Closed", await GetAppointmentStatusAsync(client, appointment.Id));

        var detailResponse = await client.GetAsync($"/api/admin/visits/{visit.Id:D}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var auditResponse = await client.GetAsync($"/api/admin/audit/access?resourceType=visit&resourceId={visit.Id:D}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<AccessAuditEnvelope>();
        Assert.NotNull(audit);
        Assert.Contains(audit!.Items, x => x.ActionCode == "READ_VISIT_DETAIL");
    }

    [Fact]
    public async Task Same_appointment_cannot_be_checked_in_twice()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Duplicate Visit Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerSetup = await CreateVisitReadyOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await CreateSchedulableGroomerAsync(client);
        var appointment = await CreateAppointmentAsync(client, petId, groomer.Id, offerSetup.OfferId);

        var firstResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{appointment.Id:D}/check-in", new { appointmentId = appointment.Id });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{appointment.Id:D}/check-in", new { appointmentId = appointment.Id });
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
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
        var offerResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code = $"VISIT_{Guid.NewGuid():N}"[..14], offerType = "Package", displayName = "Visit Package" });
        offerResponse.EnsureSuccessStatusCode();
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferEnvelope>();

        var procedureOneResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"VPROC_{Guid.NewGuid():N}"[..14], name = "Bathing" });
        procedureOneResponse.EnsureSuccessStatusCode();
        var procedureOne = await procedureOneResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var procedureTwoResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"VPROC_{Guid.NewGuid():N}"[..14], name = "Drying" });
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
        var priceRuleResponse = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload!.Id:D}/rules", new
        {
            ruleSetId = priceRuleSetPayload.Id,
            offerId = offer.Id,
            priority = 100,
            fixedAmount = 1500,
            currency = "UAH",
            breedId
        });
        priceRuleResponse.EnsureSuccessStatusCode();
        var publishPriceResponse = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{priceRuleSetPayload.Id:D}/publish", new { ruleSetId = priceRuleSetPayload.Id });
        publishPriceResponse.EnsureSuccessStatusCode();

        var durationRuleSet = await client.PostAsJsonAsync("/api/admin/duration/rule-sets", new { });
        durationRuleSet.EnsureSuccessStatusCode();
        var durationRuleSetPayload = await durationRuleSet.Content.ReadFromJsonAsync<RuleSetEnvelope>();
        var durationRuleResponse = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload!.Id:D}/rules", new
        {
            ruleSetId = durationRuleSetPayload.Id,
            offerId = offer.Id,
            priority = 100,
            baseMinutes = 120,
            bufferBeforeMinutes = 5,
            bufferAfterMinutes = 10,
            breedId
        });
        durationRuleResponse.EnsureSuccessStatusCode();
        var publishDurationResponse = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload.Id:D}/publish", new { ruleSetId = durationRuleSetPayload.Id });
        publishDurationResponse.EnsureSuccessStatusCode();

        return (offer.Id, procedureTwo.Id);
    }

    private static async Task<GroomerEnvelope> CreateSchedulableGroomerAsync(HttpClient client)
    {
        var createResponse = await client.PostAsJsonAsync("/api/admin/groomers", new { displayName = "Visit Groomer" });
        createResponse.EnsureSuccessStatusCode();
        var groomer = await createResponse.Content.ReadFromJsonAsync<GroomerEnvelope>();

        var scheduleResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer!.Id:D}/working-schedules", new
        {
            groomerId = groomer.Id,
            weekday = 5,
            startLocalTime = "09:00",
            endLocalTime = "18:00"
        });
        scheduleResponse.EnsureSuccessStatusCode();

        return groomer;
    }

    private static async Task<AppointmentEnvelope> CreateAppointmentAsync(HttpClient client, Guid petId, Guid groomerId, Guid offerId)
    {
        var response = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId,
            groomerId,
            startAtUtc = DateTime.Parse("2026-04-24T07:00:00Z").ToUniversalTime(),
            items = new[] { new { offerId, itemType = "Package" } }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AppointmentEnvelope>())!;
    }

    private static async Task<string> GetAppointmentStatusAsync(HttpClient client, Guid appointmentId)
    {
        var response = await client.GetAsync($"/api/admin/appointments/{appointmentId:D}");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AppointmentEnvelope>();
        return payload!.Status;
    }

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
        public IReadOnlyCollection<AnimalTypeEnvelope> AnimalTypes { get; set; } = [];
        public IReadOnlyCollection<BreedEnvelope> Breeds { get; set; } = [];
        public IReadOnlyCollection<CoatTypeEnvelope> CoatTypes { get; set; } = [];
        public IReadOnlyCollection<SizeCategoryEnvelope> SizeCategories { get; set; } = [];

        public Guid SamoyedBreedId => Breeds.Single(x => x.Code == "SAMOYED").Id;
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
        public IReadOnlyCollection<VisitExecutionItemEnvelope> Items { get; set; } = [];
    }

    private sealed class VisitExecutionItemEnvelope
    {
        public Guid Id { get; set; }
        public IReadOnlyCollection<VisitExpectedComponentEnvelope> ExpectedComponents { get; set; } = [];
        public IReadOnlyCollection<VisitSkippedComponentEnvelope> SkippedComponents { get; set; } = [];
    }

    private sealed class VisitExpectedComponentEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class VisitSkippedComponentEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class AccessAuditEnvelope
    {
        public IReadOnlyCollection<AccessAuditItemEnvelope> Items { get; set; } = [];
    }

    private sealed class AuditTrailEnvelope
    {
        public IReadOnlyCollection<AuditTrailItemEnvelope> Items { get; set; } = [];
    }

    private sealed class AuditTrailItemEnvelope
    {
        public string ActionCode { get; set; } = string.Empty;
    }

    private sealed class AccessAuditItemEnvelope
    {
        public string ActionCode { get; set; } = string.Empty;
    }
}

using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class BookingFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_booking_request_and_convert_it_to_appointment()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Booking Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await CreateSchedulableGroomerAsync(client);

        var requestResponse = await client.PostAsJsonAsync("/api/admin/booking-requests", new
        {
            clientId,
            petId,
            channel = "Admin",
            notes = "Customer prefers afternoon.",
            preferredTimes = new[]
            {
                new { startAtUtc = DateTime.Parse("2026-04-22T10:00:00Z").ToUniversalTime(), endAtUtc = DateTime.Parse("2026-04-22T13:00:00Z").ToUniversalTime(), label = "Afternoon" }
            },
            items = new[]
            {
                new { offerId, itemType = "Package" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var bookingRequest = await requestResponse.Content.ReadFromJsonAsync<BookingRequestEnvelope>();
        Assert.NotNull(bookingRequest);
        Assert.Equal("Submitted", bookingRequest!.Status);

        var convertResponse = await client.PostAsJsonAsync($"/api/admin/booking-requests/{bookingRequest.Id:D}/convert", new
        {
            bookingRequestId = bookingRequest.Id,
            groomerId = groomer.Id,
            startAtUtc = DateTime.Parse("2026-04-22T07:00:00Z").ToUniversalTime()
        });
        Assert.Equal(HttpStatusCode.Created, convertResponse.StatusCode);

        var appointment = await convertResponse.Content.ReadFromJsonAsync<AppointmentEnvelope>();
        Assert.NotNull(appointment);
        Assert.Equal(bookingRequest.Id, appointment!.BookingRequestId);
        Assert.Equal("Confirmed", appointment.Status);
        Assert.Equal(1, appointment.VersionNo);
        Assert.Single(appointment.Items);

        var listResponse = await client.GetAsync("/api/admin/booking-requests");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var requestList = await listResponse.Content.ReadFromJsonAsync<PagedBookingRequestEnvelope>();
        Assert.NotNull(requestList);
        Assert.Contains(requestList!.Items, x => x.Id == bookingRequest.Id && x.Status == "Converted");

        var auditResponse = await client.GetAsync($"/api/admin/audit?moduleCode=booking&entityType=booking_request&entityId={bookingRequest.Id:D}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditTrailEnvelope>();
        Assert.Contains(audit!.Items, x => x.ActionCode == "CONVERT_TO_APPOINTMENT");
    }

    [Fact]
    public async Task Admin_can_create_reschedule_and_cancel_appointment_with_optimistic_concurrency()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Appointment Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await CreateSchedulableGroomerAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId,
            groomerId = groomer.Id,
            startAtUtc = DateTime.Parse("2026-04-23T07:00:00Z").ToUniversalTime(),
            items = new[]
            {
                new { offerId, itemType = "Package" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AppointmentEnvelope>();
        Assert.NotNull(created);

        var rescheduleResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{created!.Id:D}/reschedule", new
        {
            appointmentId = created.Id,
            groomerId = groomer.Id,
            startAtUtc = DateTime.Parse("2026-04-23T10:00:00Z").ToUniversalTime(),
            expectedVersionNo = 1
        });
        Assert.Equal(HttpStatusCode.OK, rescheduleResponse.StatusCode);
        var rescheduled = await rescheduleResponse.Content.ReadFromJsonAsync<AppointmentEnvelope>();
        Assert.NotNull(rescheduled);
        Assert.Equal("Rescheduled", rescheduled!.Status);
        Assert.Equal(2, rescheduled.VersionNo);

        var staleCancelResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{created.Id:D}/cancel", new
        {
            appointmentId = created.Id,
            expectedVersionNo = 1,
            reasonCode = "CLIENT_REQUEST"
        });
        Assert.Equal(HttpStatusCode.Conflict, staleCancelResponse.StatusCode);

        var cancelResponse = await client.PostAsJsonAsync($"/api/admin/appointments/{created.Id:D}/cancel", new
        {
            appointmentId = created.Id,
            expectedVersionNo = 2,
            reasonCode = "CLIENT_REQUEST",
            notes = "Customer rescheduled elsewhere."
        });
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<AppointmentEnvelope>();
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled!.Status);
        Assert.Equal(3, cancelled.VersionNo);

        var auditResponse = await client.GetAsync($"/api/admin/audit?moduleCode=booking&entityType=appointment&entityId={created.Id:D}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditTrailEnvelope>();
        Assert.Contains(audit!.Items, x => x.ActionCode == "CANCEL");
    }

    [Fact]
    public async Task Existing_appointment_blocks_staff_availability_check()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await GetPetCatalogAsync(client);
        var clientId = await CreateClientAsync(client, "Overlap Client");
        var petId = await RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await CreateSchedulableGroomerAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId,
            groomerId = groomer.Id,
            startAtUtc = DateTime.Parse("2026-04-24T07:00:00Z").ToUniversalTime(),
            items = new[] { new { offerId, itemType = "Package" } }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var availabilityResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer.Id:D}/availability/check", new
        {
            groomerId = groomer.Id,
            petId,
            startAtUtc = DateTime.Parse("2026-04-24T07:10:00Z").ToUniversalTime(),
            reservedMinutes = 90,
            offerIds = new[] { offerId }
        });
        Assert.Equal(HttpStatusCode.OK, availabilityResponse.StatusCode);
        var availability = await availabilityResponse.Content.ReadFromJsonAsync<AvailabilityEnvelope>();
        Assert.NotNull(availability);
        Assert.False(availability!.IsAvailable);
        Assert.Contains(availability.Reasons, x => x.Contains("existing appointment", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<Guid> CreateSchedulableOfferAsync(HttpClient client, Guid breedId)
    {
        var offerResponse = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code = $"BOOK_{Guid.NewGuid():N}".Substring(0, 13), offerType = "Package", displayName = "Booking Package" });
        offerResponse.EnsureSuccessStatusCode();
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferEnvelope>();

        var procedureResponse = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code = $"PROC_{Guid.NewGuid():N}".Substring(0, 13), name = "Booking Procedure" });
        procedureResponse.EnsureSuccessStatusCode();
        var procedure = await procedureResponse.Content.ReadFromJsonAsync<ProcedureEnvelope>();

        var versionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offer!.Id:D}/versions", new { offerId = offer.Id });
        versionResponse.EnsureSuccessStatusCode();
        var version = await versionResponse.Content.ReadFromJsonAsync<OfferVersionEnvelope>();

        var componentResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version!.Id:D}/components", new
        {
            versionId = version.Id,
            procedureId = procedure!.Id,
            componentRole = "Included",
            sequenceNo = 1,
            defaultExpected = true
        });
        componentResponse.EnsureSuccessStatusCode();

        var publishVersionResponse = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{version.Id:D}/publish", new { versionId = version.Id });
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
            baseMinutes = 90,
            bufferBeforeMinutes = 0,
            bufferAfterMinutes = 0,
            breedId
        });
        durationRuleResponse.EnsureSuccessStatusCode();
        var publishDurationResponse = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{durationRuleSetPayload.Id:D}/publish", new { ruleSetId = durationRuleSetPayload.Id });
        publishDurationResponse.EnsureSuccessStatusCode();

        return offer.Id;
    }

    private static async Task<GroomerEnvelope> CreateSchedulableGroomerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/groomers", new { displayName = "Stage 6 Groomer" });
        response.EnsureSuccessStatusCode();
        var groomer = await response.Content.ReadFromJsonAsync<GroomerEnvelope>();

        foreach (var weekday in new[] { 1, 2, 3, 4, 5 })
        {
            var scheduleResponse = await client.PostAsJsonAsync($"/api/admin/groomers/{groomer!.Id:D}/working-schedules", new
            {
                groomerId = groomer.Id,
                weekday,
                startLocalTime = "09:00",
                endLocalTime = "18:00"
            });
            scheduleResponse.EnsureSuccessStatusCode();
        }

        return groomer!;
    }

    private static async Task<PetCatalogEnvelope> GetPetCatalogAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/pets/catalog");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetCatalogEnvelope>())!;
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateClientEnvelope>())!.Id;
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
            sizeCategoryCode,
            notes = "Stage 6 pet"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PetEnvelope>())!.Id;
    }

    private sealed class PagedBookingRequestEnvelope
    {
        public BookingRequestListEnvelope[] Items { get; set; } = [];
    }

    private sealed class AuditTrailEnvelope
    {
        public AuditTrailItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class AuditTrailItemEnvelope
    {
        public string ActionCode { get; set; } = string.Empty;
    }

    private sealed class BookingRequestListEnvelope
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class BookingRequestEnvelope
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class AppointmentEnvelope
    {
        public Guid Id { get; set; }
        public Guid? BookingRequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int VersionNo { get; set; }
        public AppointmentItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class AppointmentItemEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class AvailabilityEnvelope
    {
        public bool IsAvailable { get; set; }
        public string[] Reasons { get; set; } = [];
    }

    private sealed class PetCatalogEnvelope
    {
        public CatalogAnimalType[] AnimalTypes { get; set; } = [];
        public CatalogBreed[] Breeds { get; set; } = [];
        public CatalogCoatType[] CoatTypes { get; set; } = [];
        public CatalogSizeCategory[] SizeCategories { get; set; } = [];

        public Guid SamoyedBreedId => Breeds.Single(x => x.Code == "SAMOYED").Id;
        public string DogAnimalTypeCode => AnimalTypes.Single(x => x.Code == "DOG").Code;
        public string DoubleCoatCode => CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code;
        public string LargeSizeCode => SizeCategories.Single(x => x.Code == "LARGE").Code;
    }

    private sealed class CatalogAnimalType
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CatalogBreed
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CatalogCoatType
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CatalogSizeCategory
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class CreateClientEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class PetEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class OfferEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class ProcedureEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class OfferVersionEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class RuleSetEnvelope
    {
        public Guid Id { get; set; }
    }

    private sealed class GroomerEnvelope
    {
        public Guid Id { get; set; }
    }
}

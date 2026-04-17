using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class Stage11FoundationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Stage11FoundationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_query_estimate_accuracy_and_package_performance_reports()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(client);
        var clientId = await TestApiHelpers.CreateClientAsync(client, "Report Client");
        var petId = await TestApiHelpers.RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await TestApiHelpers.CreateSchedulableGroomerAsync(client);
        var appointment = await TestApiHelpers.CreateAppointmentAsync(client, petId, groomer.Id, offerId, DateTime.Parse("2026-04-25T08:00:00Z").ToUniversalTime());
        var visit = await TestApiHelpers.CheckInAsync(client, appointment.Id);
        (await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/complete", new { visitId = visit.Id })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/admin/visits/{visit.Id:D}/close", new { visitId = visit.Id })).EnsureSuccessStatusCode();

        var estimateResponse = await client.GetAsync("/api/admin/reports/estimate-accuracy");
        Assert.Equal(HttpStatusCode.OK, estimateResponse.StatusCode);

        var packageResponse = await client.GetAsync("/api/admin/reports/package-performance");
        Assert.Equal(HttpStatusCode.OK, packageResponse.StatusCode);
    }

    [Fact]
    public async Task Processing_outbox_creates_notification_job()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(client);
        var clientId = await TestApiHelpers.CreateClientAsync(client, "Notification Client");
        var petId = await TestApiHelpers.RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await TestApiHelpers.CreateSchedulableGroomerAsync(client);
        (await client.PostAsJsonAsync("/api/admin/appointments", new { petId, groomerId = groomer.Id, startAtUtc = DateTime.Parse("2026-04-26T08:00:00Z").ToUniversalTime(), items = new[] { new { offerId, itemType = "Package" } } })).EnsureSuccessStatusCode();

        var processResponse = await client.PostAsync("/api/admin/notifications/outbox/process", null);
        Assert.Equal(HttpStatusCode.OK, processResponse.StatusCode);

        var jobsResponse = await client.GetAsync("/api/admin/notifications/jobs");
        Assert.Equal(HttpStatusCode.OK, jobsResponse.StatusCode);
        var jobs = await jobsResponse.Content.ReadFromJsonAsync<NotificationJobsEnvelope>();
        Assert.NotNull(jobs);
        Assert.NotEmpty(jobs!.Items);
    }

    [Fact]
    public async Task Appointment_creation_writes_audit_trail_entry()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var catalog = await TestApiHelpers.GetPetCatalogAsync(client);
        var clientId = await TestApiHelpers.CreateClientAsync(client, "Audit Trail Client");
        var petId = await TestApiHelpers.RegisterPetAsync(client, clientId, catalog.SamoyedBreedId, catalog.DogAnimalTypeCode, catalog.DoubleCoatCode, catalog.LargeSizeCode);
        var offerId = await TestApiHelpers.CreateSchedulableOfferAsync(client, catalog.SamoyedBreedId);
        var groomer = await TestApiHelpers.CreateSchedulableGroomerAsync(client);
        var appointment = await TestApiHelpers.CreateAppointmentAsync(client, petId, groomer.Id, offerId, DateTime.Parse("2026-04-27T08:00:00Z").ToUniversalTime());

        var auditResponse = await client.GetAsync($"/api/admin/audit?moduleCode=booking&entityType=appointment&entityId={appointment.Id:D}");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditEntriesEnvelope>();
        Assert.NotNull(audit);
        Assert.Contains(audit!.Items, x => x.ActionCode == "CREATE");
    }

    private sealed class NotificationJobsEnvelope
    {
        public NotificationJobItem[] Items { get; set; } = [];
    }

    private sealed class NotificationJobItem
    {
        public Guid Id { get; set; }
    }

    private sealed class AuditEntriesEnvelope
    {
        public AuditEntryItem[] Items { get; set; } = [];
    }

    private sealed class AuditEntryItem
    {
        public string ActionCode { get; set; } = string.Empty;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Notifications.Tests;

public sealed class ReportingNotificationsAuditFoundationTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_query_estimate_accuracy_and_package_performance_reports()
    {
        using var client = await factory.CreateAdminClientAsync();

        await ReportingScenario.CreateClosedVisitAsync(client);

        var estimateResponse = await client.GetAsync("/api/admin/reports/estimate-accuracy");
        estimateResponse.ShouldBeOk();

        var packageResponse = await client.GetAsync("/api/admin/reports/package-performance");
        packageResponse.ShouldBeOk();
    }

    [Fact]
    public async Task Processing_outbox_creates_notification_job()
    {
        using var client = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(client).CreateSchedulablePetAsync("Notification Client");
        var offerId = await CatalogScenario.For(client).CreateSchedulableOfferAsync(pet.Catalog.SamoyedBreedId);
        var groomer = await StaffScenario.For(client).CreateSchedulableGroomerAsync();
        await AdminBookingApi.For(client).CreateAppointmentAsync(
            pet.PetId,
            groomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2026-04-26T08:00:00Z"));

        var processResponse = await client.PostAsync("/api/admin/notifications/outbox/process", null);
        processResponse.ShouldBeOk();

        var jobsResponse = await client.GetAsync("/api/admin/notifications/jobs");
        jobsResponse.ShouldBeOk();
        var jobs = await jobsResponse.ReadRequiredJsonAsync<NotificationJobsEnvelope>();
        Assert.NotEmpty(jobs.Items);

        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var localFilePath = configuration["Notifications:LocalFilePath"];
        Assert.False(string.IsNullOrWhiteSpace(localFilePath));
        Assert.True(File.Exists(localFilePath));

        var content = await File.ReadAllTextAsync(localFilePath);
        Assert.Contains("Appointment created", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Appointment_creation_writes_audit_trail_entry()
    {
        using var client = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(client).CreateSchedulablePetAsync("Audit Trail Client");
        var offerId = await CatalogScenario.For(client).CreateSchedulableOfferAsync(pet.Catalog.SamoyedBreedId);
        var groomer = await StaffScenario.For(client).CreateSchedulableGroomerAsync();
        var appointment = await AdminBookingApi.For(client).CreateAppointmentAsync(
            pet.PetId,
            groomer.Id,
            offerId,
            ApiClientExtensions.UtcDateTime("2026-04-27T08:00:00Z"));

        await client.AssertAuditEntryEventuallyExistsAsync(
            moduleCode: "booking",
            entityType: "appointment",
            entityId: appointment.Id,
            actionCode: "CREATE",
            failureMessage: "Appointment create audit entry was not persisted.");
    }

    private sealed class NotificationJobsEnvelope
    {
        public NotificationJobItem[] Items { get; set; } = [];
    }

    private sealed class NotificationJobItem
    {
        public Guid Id { get; set; }
    }
}

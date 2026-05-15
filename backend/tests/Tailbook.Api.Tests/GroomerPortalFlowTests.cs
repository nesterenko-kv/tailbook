using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;
using Tailbook.Api.Tests.TestSupport.Scenarios;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class GroomerPortalFlowTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Assigned_groomer_gets_privacy_safe_appointment_detail_without_contact_data()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var customer = CustomerScenario.For(admin);
        var pets = PetScenario.For(admin);
        var catalog = await pets.GetCatalogAsync();
        var clientId = await customer.CreateClientAsync("Privacy Client");
        var contactId = await customer.AddContactAsync(clientId, "Olena", "Hidden");
        await customer.AddContactMethodAsync(contactId, "Phone", "+380991112233", "+380991112233");
        await customer.AddContactMethodAsync(contactId, "Instagram", "@privacy_hidden_owner", "@privacy_hidden_owner");

        var petId = await pets.RegisterPetAsync(clientId, catalog, notes: "Sensitive paws");
        var offer = await CatalogScenario.For(admin).CreateVisitReadyOfferAsync(
            catalog.SamoyedBreedId,
            codePrefix: "GVP",
            displayName: "Groomer Visit Package",
            fixedAmount: 1400m,
            serviceMinutes: 110,
            bufferBeforeMinutes: 5,
            bufferAfterMinutes: 10);

        var groomerUserId = await factory.SeedUserAsync(
            "groomer.privacy@test.local",
            "Assigned Groomer",
            TestUsers.GroomerPassword,
            "groomer");
        var groomer = await StaffScenario.For(admin).CreateSchedulableGroomerAsync(
            "Assigned Groomer",
            groomerUserId,
            weekdays: [5]);
        var appointment = await AdminBookingApi.For(admin).CreateAppointmentAsync(petId, groomer.Id, offer.OfferId);

        using var groomerClient = await factory.CreateAuthenticatedClientAsync(
            "groomer.privacy@test.local",
            TestUsers.GroomerPassword);

        var response = await groomerClient.GetAsync($"/api/groomer/appointments/{appointment.Id:D}");
        response.ShouldBeOk();

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sensitive paws", raw);
        Assert.DoesNotContain("+380991112233", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@privacy_hidden_owner", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contact", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Groomer_can_execute_own_visit_but_cannot_read_other_groomer_appointment()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var pet = await PetScenario.For(admin).CreateSchedulablePetAsync(
            "Execution Client",
            petNotes: "Prefers calm handling");
        var offer = await CatalogScenario.For(admin).CreateVisitReadyOfferAsync(
            pet.Catalog.SamoyedBreedId,
            codePrefix: "GVP",
            displayName: "Groomer Visit Package",
            fixedAmount: 1400m,
            serviceMinutes: 110,
            bufferBeforeMinutes: 5,
            bufferAfterMinutes: 10);

        var assignedUserId = await factory.SeedUserAsync(
            "groomer.exec@test.local",
            "Execution Groomer",
            TestUsers.GroomerPassword,
            "groomer");
        var otherUserId = await factory.SeedUserAsync(
            "groomer.other@test.local",
            "Other Groomer",
            TestUsers.GroomerPassword,
            "groomer");

        var staff = StaffScenario.For(admin);
        var assignedGroomer = await staff.CreateSchedulableGroomerAsync("Execution Groomer", assignedUserId, weekdays: [5]);
        await staff.CreateSchedulableGroomerAsync("Other Groomer", otherUserId, weekdays: [5]);

        var appointment = await AdminBookingApi.For(admin).CreateAppointmentAsync(pet.PetId, assignedGroomer.Id, offer.OfferId);

        using var otherClient = await factory.CreateAuthenticatedClientAsync(
            "groomer.other@test.local",
            TestUsers.GroomerPassword);
        var forbiddenResponse = await otherClient.GetAsync($"/api/groomer/appointments/{appointment.Id:D}");
        forbiddenResponse.ShouldBeNotFound();

        using var groomerClient = await factory.CreateAuthenticatedClientAsync(
            "groomer.exec@test.local",
            TestUsers.GroomerPassword);

        var checkInResponse = await groomerClient.PostAsJsonAsync($"/api/groomer/appointments/{appointment.Id:D}/check-in", new { appointmentId = appointment.Id });
        checkInResponse.ShouldBeCreated();
        var visit = await checkInResponse.ReadRequiredJsonAsync<VisitEnvelope>();
        Assert.Equal("Open", visit.Status);

        var executionItem = visit.Items.Single();
        var expectedComponent = executionItem.ExpectedComponents.First();

        var performedResponse = await groomerClient.PostAsJsonAsync($"/api/groomer/visits/{visit.Id:D}/performed-procedures", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            procedureId = expectedComponent.ProcedureId,
            note = "Completed by assigned groomer."
        });
        performedResponse.ShouldBeOk();
        visit = await performedResponse.ReadRequiredJsonAsync<VisitEnvelope>();
        Assert.Equal("InProgress", visit.Status);
        Assert.NotEmpty(visit.Items.Single().PerformedProcedures);

        var skippedResponse = await groomerClient.PostAsJsonAsync($"/api/groomer/visits/{visit.Id:D}/skipped-components", new
        {
            visitId = visit.Id,
            visitExecutionItemId = executionItem.Id,
            offerVersionComponentId = expectedComponent.Id,
            omissionReasonCode = "OPERATIONAL_DECISION",
            note = "Skipped after execution test."
        });
        skippedResponse.ShouldBeOk();
        visit = await skippedResponse.ReadRequiredJsonAsync<VisitEnvelope>();
        Assert.NotEmpty(visit.Items.Single().SkippedComponents);

        var currentVisitResponse = await groomerClient.GetAsync($"/api/groomer/appointments/{appointment.Id:D}/visit");
        currentVisitResponse.ShouldBeOk();
    }
}

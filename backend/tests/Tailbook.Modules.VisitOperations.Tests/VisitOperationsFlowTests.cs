using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.VisitOperations.Tests;

public sealed class VisitOperationsFlowTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_check_in_execute_adjust_complete_and_close_visit()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await VisitScenario
            .For(admin)
            .WithSchedulablePet("Visit Client")
            .WithVisitReadyOffer()
            .WithAvailableGroomer("Visit Groomer")
            .CreateAsync();

        var visit = await scenario.CheckInAsync();
        Assert.Equal("Open", visit.Status);
        Assert.Single(visit.Items);
        Assert.Equal("CheckedIn", await scenario.GetAppointmentStatusAsync());

        var executionItem = visit.Items.Single();
        var expectedComponent = executionItem.ExpectedComponents.First();

        visit = await scenario.AddPerformedProcedureAsync(
            visit.Id,
            executionItem.Id,
            scenario.Offer.SecondProcedureId,
            "Completed as expected.");
        Assert.Equal("InProgress", visit.Status);
        Assert.Equal("InProgress", await scenario.GetAppointmentStatusAsync());

        visit = await scenario.SkipExpectedComponentAsync(
            visit.Id,
            executionItem.Id,
            expectedComponent.Id,
            "PET_STRESSED",
            "Skipped one included step.");
        Assert.Single(visit.Items.Single().SkippedComponents);

        visit = await scenario.ApplyAdjustmentAsync(
            visit.Id,
            sign: -1,
            amount: 150,
            reasonCode: "CALMER_THAN_EXPECTED",
            note: "Applied goodwill reduction.");
        Assert.Equal(1350m, visit.FinalTotalAmount);

        await admin.AssertAuditEntryEventuallyExistsAsync(
            moduleCode: "visitops",
            entityType: "visit",
            entityId: visit.Id,
            actionCode: "APPLY_ADJUSTMENT",
            failureMessage: "Visit adjustment audit entry was not persisted.");

        visit = await scenario.CompleteAsync(visit.Id);
        Assert.Equal("AwaitingFinalization", visit.Status);
        Assert.Equal("Completed", await scenario.GetAppointmentStatusAsync());

        visit = await scenario.CloseAsync(visit.Id);
        Assert.Equal("Closed", visit.Status);
        Assert.Equal("Closed", await scenario.GetAppointmentStatusAsync());

        var detailResponse = await scenario.GetVisitDetailResponseAsync(visit.Id);
        detailResponse.ShouldBeOk();

        await admin.AssertAccessAuditEntryEventuallyExistsAsync(
            resourceType: "visit",
            resourceId: visit.Id,
            actionCode: "READ_VISIT_DETAIL",
            failureMessage: "Visit detail access audit entry was not persisted.");
    }

    [Fact]
    public async Task Same_appointment_cannot_be_checked_in_twice()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await VisitScenario
            .For(admin)
            .WithSchedulablePet("Duplicate Visit Client")
            .WithVisitReadyOffer()
            .WithAvailableGroomer("Visit Groomer")
            .CreateAsync();

        var firstResponse = await scenario.CheckInResponseAsync();
        firstResponse.ShouldBeCreated();

        var secondResponse = await scenario.CheckInResponseAsync();
        secondResponse.ShouldBeConflict();
    }
}

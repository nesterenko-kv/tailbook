namespace Tailbook.Api.Tests.TestSupport.Scenarios;

public sealed class ReportingScenario
{
    public static async Task<ReportingScenarioResult> CreateClosedVisitAsync(HttpClient client)
    {
        var scenario = await VisitScenario
            .For(client)
            .WithSchedulablePet("Reporting Client")
            .WithVisitReadyOffer(
                codePrefix: "RPT",
                displayName: "Report Package",
                fixedAmount: 1500m,
                serviceMinutes: 120,
                bufferBeforeMinutes: 10,
                bufferAfterMinutes: 15)
            .WithAvailableGroomer("Report Groomer", Enumerable.Range(1, 7))
            .WithAppointmentAt(TimeProvider.System.GetUtcNow().Date.AddDays(-1).AddHours(10))
            .CreateAsync();

        var visit = await scenario.CheckInAsync();
        var executionItem = visit.Items.Single();
        var expectedComponent = executionItem.ExpectedComponents.First();

        await scenario.AddPerformedProcedureAsync(
            visit.Id,
            executionItem.Id,
            scenario.Offer.SecondProcedureId,
            "Completed as expected.");

        await scenario.SkipExpectedComponentAsync(
            visit.Id,
            executionItem.Id,
            expectedComponent.Id,
            "PET_STRESSED",
            "Skipped one included step.");

        await scenario.ApplyAdjustmentAsync(
            visit.Id,
            sign: -1,
            amount: 150,
            reasonCode: "CALMER_THAN_EXPECTED",
            note: "Applied goodwill reduction.");

        visit = await scenario.CompleteAsync(visit.Id);
        visit = await scenario.CloseAsync(visit.Id);

        return new ReportingScenarioResult(visit.Id, scenario.Offer.OfferId);
    }
}

public sealed record ReportingScenarioResult(Guid VisitId, Guid OfferId);

using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

public sealed class CatalogScenario(HttpClient client)
{
    public static CatalogScenario For(HttpClient client)
        => new(client);

    public async Task<OfferEnvelope> CreateOfferAsync(string code, string offerType, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/catalog/offers", new { code, offerType, displayName });
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<OfferEnvelope>();
    }

    public async Task<ProcedureEnvelope> CreateProcedureAsync(string code, string name)
    {
        var response = await client.PostAsJsonAsync("/api/admin/catalog/procedures", new { code, name });
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<ProcedureEnvelope>();
    }

    public async Task<OfferVersionEnvelope> CreateVersionAsync(Guid offerId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offers/{offerId:D}/versions", new { offerId });
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<OfferVersionEnvelope>();
    }

    public async Task AddComponentAsync(Guid versionId, Guid procedureId, int sequenceNo = 1)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{versionId:D}/components", new
        {
            versionId,
            procedureId,
            componentRole = "Included",
            sequenceNo,
            defaultExpected = true
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task PublishOfferVersionAsync(Guid versionId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/catalog/offer-versions/{versionId:D}/publish", new { versionId });
        response.EnsureSuccessStatusCode();
    }

    public async Task<RuleSetEnvelope> CreatePriceRuleSetAsync()
    {
        var response = await client.PostAsJsonAsync("/api/admin/pricing/rule-sets", new { });
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<RuleSetEnvelope>();
    }

    public async Task AddPriceRuleAsync(
        Guid ruleSetId,
        Guid offerId,
        int priority,
        decimal fixedAmount,
        Guid? breedId,
        Guid? animalTypeId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{ruleSetId:D}/rules", new
        {
            ruleSetId,
            offerId,
            priority,
            fixedAmount,
            currency = "UAH",
            breedId,
            animalTypeId
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task PublishPriceRuleSetAsync(Guid ruleSetId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/pricing/rule-sets/{ruleSetId:D}/publish", new { ruleSetId });
        response.EnsureSuccessStatusCode();
    }

    public async Task<RuleSetEnvelope> CreateDurationRuleSetAsync()
    {
        var response = await client.PostAsJsonAsync("/api/admin/duration/rule-sets", new { });
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredJsonAsync<RuleSetEnvelope>();
    }

    public async Task AddDurationRuleAsync(
        Guid ruleSetId,
        Guid offerId,
        int priority,
        int baseMinutes,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        Guid? breedId,
        Guid? animalTypeId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{ruleSetId:D}/rules", new
        {
            ruleSetId,
            offerId,
            priority,
            baseMinutes,
            bufferBeforeMinutes,
            bufferAfterMinutes,
            breedId,
            animalTypeId
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task PublishDurationRuleSetAsync(Guid ruleSetId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/duration/rule-sets/{ruleSetId:D}/publish", new { ruleSetId });
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid> CreateSchedulableOfferAsync(
        Guid breedId,
        string codePrefix = "PKG",
        string displayName = "Schedulable Package",
        decimal fixedAmount = 1200m,
        int serviceMinutes = 90,
        int bufferBeforeMinutes = 0,
        int bufferAfterMinutes = 0)
    {
        var setup = await CreateVisitReadyOfferAsync(
            breedId,
            codePrefix,
            displayName,
            fixedAmount,
            serviceMinutes,
            bufferBeforeMinutes,
            bufferAfterMinutes,
            procedureNames: ["Procedure"]);

        return setup.OfferId;
    }

    public async Task<VisitReadyOffer> CreateVisitReadyOfferAsync(
        Guid breedId,
        string codePrefix = "VISIT",
        string displayName = "Visit Package",
        decimal fixedAmount = 1500m,
        int serviceMinutes = 120,
        int bufferBeforeMinutes = 5,
        int bufferAfterMinutes = 10,
        string[]? procedureNames = null)
    {
        procedureNames ??= ["Bathing", "Drying"];

        var offer = await CreateOfferAsync(UniqueCode(codePrefix), "Package", displayName);
        var version = await CreateVersionAsync(offer.Id);
        var procedures = new List<ProcedureEnvelope>();

        for (var i = 0; i < procedureNames.Length; i++)
        {
            var procedure = await CreateProcedureAsync(UniqueCode($"{codePrefix}_P"), procedureNames[i]);
            procedures.Add(procedure);
            await AddComponentAsync(version.Id, procedure.Id, i + 1);
        }

        await PublishOfferVersionAsync(version.Id);

        var priceRuleSet = await CreatePriceRuleSetAsync();
        await AddPriceRuleAsync(priceRuleSet.Id, offer.Id, 100, fixedAmount, breedId, null);
        await PublishPriceRuleSetAsync(priceRuleSet.Id);

        var durationRuleSet = await CreateDurationRuleSetAsync();
        await AddDurationRuleAsync(durationRuleSet.Id, offer.Id, 100, serviceMinutes, bufferBeforeMinutes, bufferAfterMinutes, breedId, null);
        await PublishDurationRuleSetAsync(durationRuleSet.Id);

        return new VisitReadyOffer(offer.Id, procedures.First().Id, procedures.Last().Id);
    }

    internal static string UniqueCode(string prefix)
    {
        var sanitized = prefix.Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase);
        var shortPrefix = sanitized[..Math.Min(5, sanitized.Length)];
        return $"{shortPrefix}_{Guid.NewGuid():N}"[..14];
    }
}

public sealed record VisitReadyOffer(Guid OfferId, Guid FirstProcedureId, Guid SecondProcedureId);

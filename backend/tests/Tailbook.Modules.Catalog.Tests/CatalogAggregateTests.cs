using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.Api.Tests;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Contracts;
using Tailbook.Modules.Catalog.Domain.Aggregates;
using Tailbook.Modules.Catalog.Domain.Entities;
using Xunit;

namespace Tailbook.Modules.Catalog.Tests;

public sealed class CatalogAggregateTests
{
    [Fact]
    public void Commercial_offer_creates_versions_components_and_publishes_package_versions()
    {
        var offer = CreatePackageOffer();
        var version = AssertSuccess(offer.CreateVersion(
            Guid.NewGuid(),
            null,
            null,
            "  Policy text  ",
            "  Initial version  ",
            Utc("2026-05-01T09:00:00Z")));

        var component = AssertSuccess(offer.AddComponent(
            version.Id,
            Guid.NewGuid(),
            " included ",
            1,
            true,
            Utc("2026-05-01T09:05:00Z")));

        Assert.Equal(1, version.VersionNo);
        Assert.Equal(OfferVersionStatusCodes.Draft, version.Status);
        Assert.Equal("Policy text", version.PolicyText);
        Assert.Equal("Initial version", version.ChangeNote);
        Assert.Equal(OfferComponentRoleCodes.Included, component.ComponentRole);
        Assert.Single(version.Components);

        AssertErrorCode(
            offer.AddComponent(version.Id, Guid.NewGuid(), OfferComponentRoleCodes.Included, 1, true, Utc("2026-05-01T09:06:00Z")),
            "Catalog.ComponentSequenceExists");
        AssertErrorCode(
            offer.AddComponent(version.Id, component.ProcedureId, OfferComponentRoleCodes.Included, 2, true, Utc("2026-05-01T09:07:00Z")),
            "Catalog.ComponentProcedureExists");

        AssertSuccess(offer.PublishVersion(version.Id, Utc("2026-05-01T10:00:00Z")));

        Assert.Equal(OfferVersionStatusCodes.Published, version.Status);
        Assert.Equal(Utc("2026-05-01T10:00:00Z"), version.PublishedAt);
        AssertErrorCode(
            offer.AddComponent(version.Id, Guid.NewGuid(), OfferComponentRoleCodes.Included, 2, true, Utc("2026-05-01T10:01:00Z")),
            "Catalog.OfferVersionImmutable");
    }

    [Fact]
    public void Commercial_offer_blocks_invalid_component_and_publish_paths()
    {
        var standalone = AssertSuccess(CommercialOffer.Create(
            Guid.NewGuid(),
            " nail trim ",
            OfferTypeCodes.StandaloneService,
            " Nail Trim ",
            Utc("2026-05-01T09:00:00Z")));
        var standaloneVersion = AssertSuccess(standalone.CreateVersion(Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:00:00Z")));

        AssertErrorCode(
            standalone.AddComponent(standaloneVersion.Id, Guid.NewGuid(), OfferComponentRoleCodes.Included, 1, true, Utc("2026-05-01T09:01:00Z")),
            "Catalog.OfferVersionNotPackage");

        var package = CreatePackageOffer();
        var emptyVersion = AssertSuccess(package.CreateVersion(Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:00:00Z")));

        AssertErrorCode(
            package.PublishVersion(emptyVersion.Id, Utc("2026-05-01T09:10:00Z")),
            "Catalog.PackageOfferVersionEmpty");
    }

    [Fact]
    public void Catalog_child_collections_cannot_be_externally_mutated()
    {
        var offer = CreatePackageOffer();
        var version = AssertSuccess(offer.CreateVersion(Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:00:00Z")));
        AssertSuccess(offer.AddComponent(version.Id, Guid.NewGuid(), OfferComponentRoleCodes.Included, 1, true, Utc("2026-05-01T09:01:00Z")));

        var priceRuleSet = PriceRuleSet.Create(Guid.NewGuid(), 1, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));
        AssertSuccess(priceRuleSet.AddRule(Guid.NewGuid(), 10, 100m, "UAH", Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:01:00Z")));

        var durationRuleSet = DurationRuleSet.Create(Guid.NewGuid(), 1, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));
        AssertSuccess(durationRuleSet.AddRule(Guid.NewGuid(), 10, 60, 5, 10, Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:01:00Z")));

        var versions = Assert.IsAssignableFrom<ICollection<OfferVersion>>(offer.Versions);
        var components = Assert.IsAssignableFrom<ICollection<OfferVersionComponent>>(version.Components);
        var priceRules = Assert.IsAssignableFrom<ICollection<PriceRule>>(priceRuleSet.Rules);
        var durationRules = Assert.IsAssignableFrom<ICollection<DurationRule>>(durationRuleSet.Rules);

        Assert.True(versions.IsReadOnly);
        Assert.True(components.IsReadOnly);
        Assert.True(priceRules.IsReadOnly);
        Assert.True(durationRules.IsReadOnly);
        Assert.Throws<NotSupportedException>(versions.Clear);
        Assert.Throws<NotSupportedException>(components.Clear);
        Assert.Throws<NotSupportedException>(priceRules.Clear);
        Assert.Throws<NotSupportedException>(durationRules.Clear);
    }

    [Fact]
    public void Price_rule_set_owns_rule_uniqueness_currency_and_publish_lifecycle()
    {
        var offerId = Guid.NewGuid();
        var animalTypeId = Guid.NewGuid();
        var ruleSet = PriceRuleSet.Create(Guid.NewGuid(), 1, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));

        var rule = AssertSuccess(ruleSet.AddRule(
            offerId,
            20,
            125.5m,
            " uah ",
            animalTypeId,
            null,
            null,
            null,
            null,
            Utc("2026-05-01T09:01:00Z")));

        Assert.Equal("UAH", rule.Currency);
        Assert.Equal(1, rule.SpecificityScore);
        Assert.Equal(1, rule.Condition.SpecificityScore);
        AssertErrorCode(
            ruleSet.AddRule(offerId, 30, 140m, "UAH", animalTypeId, null, null, null, null, Utc("2026-05-01T09:02:00Z")),
            "Catalog.DuplicatePriceRule");

        AssertSuccess(ruleSet.Publish(Utc("2026-05-01T10:00:00Z")));

        Assert.Equal(RuleSetStatusCodes.Published, ruleSet.Status);
        AssertErrorCode(
            ruleSet.AddRule(Guid.NewGuid(), 10, 150m, "UAH", null, null, null, null, null, Utc("2026-05-01T10:01:00Z")),
            "Catalog.PriceRuleSetNotDraft");
    }

    [Fact]
    public void Duration_rule_set_owns_rule_uniqueness_and_publish_lifecycle()
    {
        var offerId = Guid.NewGuid();
        var breedId = Guid.NewGuid();
        var emptyRuleSet = DurationRuleSet.Create(Guid.NewGuid(), 1, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));

        AssertErrorCode(emptyRuleSet.Publish(Utc("2026-05-01T09:01:00Z")), "Catalog.DurationRuleSetEmpty");

        var ruleSet = DurationRuleSet.Create(Guid.NewGuid(), 2, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));
        var rule = AssertSuccess(ruleSet.AddRule(
            offerId,
            10,
            90,
            5,
            10,
            null,
            breedId,
            null,
            null,
            null,
            Utc("2026-05-01T09:01:00Z")));

        Assert.Equal(1, rule.SpecificityScore);
        Assert.Equal(105, rule.BaseMinutes + rule.BufferBeforeMinutes + rule.BufferAfterMinutes);
        AssertErrorCode(
            ruleSet.AddRule(offerId, 20, 100, 5, 10, null, breedId, null, null, null, Utc("2026-05-01T09:02:00Z")),
            "Catalog.DuplicateDurationRule");

        AssertSuccess(ruleSet.Publish(Utc("2026-05-01T10:00:00Z")));

        Assert.Equal(RuleSetStatusCodes.Published, ruleSet.Status);
    }

    [Fact]
    public async Task Catalog_aggregates_round_trip_through_ef_core()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"catalog-aggregate-{Guid.NewGuid():N}")
            .Options;

        var offer = CreatePackageOffer();
        var version = AssertSuccess(offer.CreateVersion(Guid.NewGuid(), null, null, "Policy", "Initial", Utc("2026-05-01T09:00:00Z")));
        AssertSuccess(offer.AddComponent(version.Id, Guid.NewGuid(), OfferComponentRoleCodes.Included, 1, true, Utc("2026-05-01T09:01:00Z")));
        AssertSuccess(offer.PublishVersion(version.Id, Utc("2026-05-01T09:02:00Z")));

        var priceRuleSet = PriceRuleSet.Create(Guid.NewGuid(), 1, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));
        AssertSuccess(priceRuleSet.AddRule(offer.Id, 10, 100m, "UAH", Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:01:00Z")));
        AssertSuccess(priceRuleSet.Publish(Utc("2026-05-01T09:02:00Z")));

        var durationRuleSet = DurationRuleSet.Create(Guid.NewGuid(), 1, Utc("2026-05-01T09:00:00Z"), null, Utc("2026-05-01T09:00:00Z"));
        AssertSuccess(durationRuleSet.AddRule(offer.Id, 10, 60, 5, 10, Guid.NewGuid(), null, null, null, null, Utc("2026-05-01T09:01:00Z")));
        AssertSuccess(durationRuleSet.Publish(Utc("2026-05-01T09:02:00Z")));

        await using (var dbContext = TestModelConfiguration.CreateDbContext(options))
        {
            dbContext.Set<CommercialOffer>().Add(offer);
            dbContext.Set<PriceRuleSet>().Add(priceRuleSet);
            dbContext.Set<DurationRuleSet>().Add(durationRuleSet);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = TestModelConfiguration.CreateDbContext(options))
        {
            var loadedOffer = await dbContext.Set<CommercialOffer>()
                .AsNoTracking()
                .Include(x => x.Versions)
                .ThenInclude(x => x.Components)
                .SingleAsync(x => x.Id == offer.Id);
            var loadedPriceRuleSet = await dbContext.Set<PriceRuleSet>()
                .AsNoTracking()
                .Include(x => x.Rules)
                .ThenInclude(x => x.Condition)
                .SingleAsync(x => x.Id == priceRuleSet.Id);
            var loadedDurationRuleSet = await dbContext.Set<DurationRuleSet>()
                .AsNoTracking()
                .Include(x => x.Rules)
                .ThenInclude(x => x.Condition)
                .SingleAsync(x => x.Id == durationRuleSet.Id);

            var loadedVersion = Assert.Single(loadedOffer.Versions);
            Assert.Equal(OfferVersionStatusCodes.Published, loadedVersion.Status);
            Assert.Single(loadedVersion.Components);
            Assert.Single(loadedPriceRuleSet.Rules);
            Assert.NotNull(loadedPriceRuleSet.Rules.Single().Condition);
            Assert.Single(loadedDurationRuleSet.Rules);
            Assert.NotNull(loadedDurationRuleSet.Rules.Single().Condition);
        }
    }

    private static CommercialOffer CreatePackageOffer()
    {
        return AssertSuccess(CommercialOffer.Create(
            Guid.NewGuid(),
            " full grooming ",
            " package ",
            " Full Grooming ",
            Utc("2026-05-01T09:00:00Z")));
    }

    private static T AssertSuccess<T>(ErrorOr<T> result)
    {
        Assert.False(result.IsError, string.Join("; ", result.Errors.Select(error => error.Description)));
        return result.Value;
    }

    private static void AssertSuccess(ErrorOr<Success> result)
    {
        Assert.False(result.IsError, string.Join("; ", result.Errors.Select(error => error.Description)));
    }

    private static void AssertErrorCode<T>(ErrorOr<T> result, string expectedCode)
    {
        var error = Assert.Single(AssertError(result));
        Assert.Equal(expectedCode, error.Code);
    }

    private static IReadOnlyList<Error> AssertError<T>(ErrorOr<T> result)
    {
        Assert.True(result.IsError);
        return result.Errors;
    }

    private static DateTimeOffset Utc(string value)
    {
        return DateTimeOffset.Parse(value).ToUniversalTime();
    }
}

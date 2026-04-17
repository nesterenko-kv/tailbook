using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Contracts;
using Tailbook.Modules.Catalog.Domain;
using Tailbook.Modules.Pets.Domain;
using Tailbook.Modules.Staff.Domain;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class DevelopmentDemoSalonSeeder(IHostEnvironment environment) : IDataSeeder
{
    private const string Currency = "UAH";
    private const string SeedChangeNote = "Development demo seed refreshed from bookkeeping export and current price cards.";

    public int Order => 40;

    public async Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        var taxonomy = await LoadTaxonomyAsync(dbContext, cancellationToken);

        await EnsureGroomersAsync(dbContext, utcNow, cancellationToken);
        var procedures = await EnsureProceduresAsync(dbContext, utcNow, cancellationToken);
        var offers = await EnsureOffersAsync(dbContext, utcNow, cancellationToken);
        await EnsureOfferVersionsAsync(dbContext, offers, procedures, utcNow, cancellationToken);
        await EnsurePricingAsync(dbContext, taxonomy, offers, utcNow, cancellationToken);
        await EnsureDurationsAsync(dbContext, taxonomy, offers, utcNow, cancellationToken);
    }

    private static async Task<TaxonomyLookup> LoadTaxonomyAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var animalTypes = await dbContext.Set<AnimalType>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var breedGroups = await dbContext.Set<BreedGroup>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var breeds = await dbContext.Set<Breed>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var coatTypes = await dbContext.Set<CoatType>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return new TaxonomyLookup(animalTypes, breedGroups, breeds, coatTypes, sizeCategories);
    }

    private static async Task EnsureGroomersAsync(AppDbContext dbContext, DateTime utcNow, CancellationToken cancellationToken)
    {
        var schedulesByName = new Dictionary<string, (int Weekday, TimeSpan Start, TimeSpan End)[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Юля"] =
            [
                (1, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (2, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (3, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (4, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (5, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (6, TimeSpan.FromHours(9), TimeSpan.FromHours(16))
            ],
            ["Наташа"] =
            [
                (2, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (3, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (4, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (5, TimeSpan.FromHours(9), TimeSpan.FromHours(18)),
                (6, TimeSpan.FromHours(9), TimeSpan.FromHours(16))
            ],
            ["Лена"] =
            [
                (3, TimeSpan.FromHours(10), TimeSpan.FromHours(18)),
                (4, TimeSpan.FromHours(10), TimeSpan.FromHours(18)),
                (5, TimeSpan.FromHours(10), TimeSpan.FromHours(18)),
                (6, TimeSpan.FromHours(10), TimeSpan.FromHours(17))
            ],
            ["Ксения"] =
            [
                (5, TimeSpan.FromHours(10), TimeSpan.FromHours(18)),
                (6, TimeSpan.FromHours(10), TimeSpan.FromHours(18)),
                (7, TimeSpan.FromHours(10), TimeSpan.FromHours(16))
            ]
        };

        var groomers = await dbContext.Set<Groomer>().ToListAsync(cancellationToken);
        var schedules = await dbContext.Set<WorkingSchedule>().ToListAsync(cancellationToken);

        foreach (var pair in schedulesByName)
        {
            var groomer = groomers.SingleOrDefault(x => string.Equals(x.DisplayName, pair.Key, StringComparison.OrdinalIgnoreCase));
            if (groomer is null)
            {
                groomer = new Groomer
                {
                    Id = Guid.NewGuid(),
                    DisplayName = pair.Key,
                    Active = true,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };
                dbContext.Set<Groomer>().Add(groomer);
                groomers.Add(groomer);
            }
            else
            {
                groomer.Active = true;
                groomer.UpdatedAtUtc = utcNow;
            }

            foreach (var scheduleSeed in pair.Value)
            {
                var schedule = schedules.SingleOrDefault(x => x.GroomerId == groomer.Id && x.Weekday == scheduleSeed.Weekday);
                if (schedule is null)
                {
                    schedule = new WorkingSchedule
                    {
                        Id = Guid.NewGuid(),
                        GroomerId = groomer.Id,
                        Weekday = scheduleSeed.Weekday,
                        StartLocalTime = scheduleSeed.Start,
                        EndLocalTime = scheduleSeed.End,
                        CreatedAtUtc = utcNow,
                        UpdatedAtUtc = utcNow
                    };
                    dbContext.Set<WorkingSchedule>().Add(schedule);
                    schedules.Add(schedule);
                }
                else
                {
                    schedule.StartLocalTime = scheduleSeed.Start;
                    schedule.EndLocalTime = scheduleSeed.End;
                    schedule.UpdatedAtUtc = utcNow;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, ProcedureCatalogItem>> EnsureProceduresAsync(AppDbContext dbContext, DateTime utcNow, CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new ProcedureSeed("BATHING", "Bathing"),
            new ProcedureSeed("COAT_DRYING", "Coat Drying"),
            new ProcedureSeed("BRUSHING", "Brushing"),
            new ProcedureSeed("DESHEDDING_BRUSHING", "Deshedding Brushing"),
            new ProcedureSeed("HAIRCUT", "Haircut"),
            new ProcedureSeed("EAR_CLEANING", "Ear Cleaning"),
            new ProcedureSeed("NAIL_TRIM_AND_FILING", "Nail Trim & Filing"),
            new ProcedureSeed("SKIN_CARE", "Skin Care / Nourishing Treatment")
        };

        var procedures = await dbContext.Set<ProcedureCatalogItem>().ToListAsync(cancellationToken);
        foreach (var seed in seeds)
        {
            var entity = procedures.SingleOrDefault(x => string.Equals(x.Code, seed.Code, StringComparison.OrdinalIgnoreCase));
            if (entity is null)
            {
                entity = new ProcedureCatalogItem
                {
                    Id = Guid.NewGuid(),
                    Code = seed.Code,
                    Name = seed.Name,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };
                dbContext.Set<ProcedureCatalogItem>().Add(entity);
                procedures.Add(entity);
            }
            else
            {
                entity.Name = seed.Name;
                entity.IsActive = true;
                entity.UpdatedAtUtc = utcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return procedures.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, CommercialOffer>> EnsureOffersAsync(AppDbContext dbContext, DateTime utcNow, CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new OfferSeed("DOG_FULL_GROOMING", OfferTypeCodes.Package, "Dog Full Grooming"),
            new OfferSeed("DOG_EXPRESS_DESHEDDING", OfferTypeCodes.Package, "Dog Express Deshedding"),
            new OfferSeed("CAT_HAIRCUT", OfferTypeCodes.Package, "Cat Haircut"),
            new OfferSeed("CAT_EXPRESS_DESHEDDING", OfferTypeCodes.Package, "Cat Express Deshedding"),
            new OfferSeed("DELIVERY_ADDON", OfferTypeCodes.AddOn, "Delivery"),
            new OfferSeed("NAIL_TRIM_ONLY", OfferTypeCodes.StandaloneService, "Nail Trim & Filing"),
            new OfferSeed("EAR_CLEANING_ONLY", OfferTypeCodes.StandaloneService, "Ear Cleaning")
        };

        var offers = await dbContext.Set<CommercialOffer>().ToListAsync(cancellationToken);
        foreach (var seed in seeds)
        {
            var entity = offers.SingleOrDefault(x => string.Equals(x.Code, seed.Code, StringComparison.OrdinalIgnoreCase));
            if (entity is null)
            {
                entity = new CommercialOffer
                {
                    Id = Guid.NewGuid(),
                    Code = seed.Code,
                    OfferType = seed.OfferType,
                    DisplayName = seed.DisplayName,
                    IsActive = true,
                    CreatedAtUtc = utcNow,
                    UpdatedAtUtc = utcNow
                };
                dbContext.Set<CommercialOffer>().Add(entity);
                offers.Add(entity);
            }
            else
            {
                entity.OfferType = seed.OfferType;
                entity.DisplayName = seed.DisplayName;
                entity.IsActive = true;
                entity.UpdatedAtUtc = utcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return offers.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, OfferVersion>> EnsureOfferVersionsAsync(
        AppDbContext dbContext,
        IReadOnlyDictionary<string, CommercialOffer> offers,
        IReadOnlyDictionary<string, ProcedureCatalogItem> procedures,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var policyText = "Final amount may vary after the visit based on behavior, matting, real effort, and non-standard size. Customer contact data stays admin-only in operational flows.";
        var componentMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["DOG_FULL_GROOMING"] = ["BATHING", "HAIRCUT", "EAR_CLEANING", "NAIL_TRIM_AND_FILING"],
            ["DOG_EXPRESS_DESHEDDING"] = ["BATHING", "DESHEDDING_BRUSHING", "COAT_DRYING", "EAR_CLEANING", "NAIL_TRIM_AND_FILING"],
            ["CAT_HAIRCUT"] = ["NAIL_TRIM_AND_FILING", "BRUSHING", "BATHING", "SKIN_CARE", "EAR_CLEANING", "HAIRCUT"],
            ["CAT_EXPRESS_DESHEDDING"] = ["NAIL_TRIM_AND_FILING", "DESHEDDING_BRUSHING", "BATHING", "SKIN_CARE", "EAR_CLEANING"],
            ["NAIL_TRIM_ONLY"] = ["NAIL_TRIM_AND_FILING"],
            ["EAR_CLEANING_ONLY"] = ["EAR_CLEANING"]
        };

        var allVersions = await dbContext.Set<OfferVersion>().ToListAsync(cancellationToken);
        var allComponents = await dbContext.Set<OfferVersionComponent>().ToListAsync(cancellationToken);
        var result = new Dictionary<string, OfferVersion>(StringComparer.OrdinalIgnoreCase);

        foreach (var offer in offers.Values)
        {
            var version = allVersions
                .Where(x => x.OfferId == offer.Id)
                .OrderByDescending(x => x.VersionNo)
                .FirstOrDefault();

            if (version is null)
            {
                version = new OfferVersion
                {
                    Id = Guid.NewGuid(),
                    OfferId = offer.Id,
                    VersionNo = 1,
                    Status = OfferVersionStatusCodes.Published,
                    ValidFromUtc = utcNow,
                    PolicyText = policyText,
                    ChangeNote = SeedChangeNote,
                    CreatedAtUtc = utcNow,
                    PublishedAtUtc = utcNow
                };
                dbContext.Set<OfferVersion>().Add(version);
                allVersions.Add(version);
            }
            else
            {
                version.Status = OfferVersionStatusCodes.Published;
                version.ValidToUtc = null;
                version.PolicyText = policyText;
                version.ChangeNote = SeedChangeNote;
                version.PublishedAtUtc ??= utcNow;
            }

            result[offer.Code] = version;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var pair in componentMap)
        {
            var version = result[pair.Key];
            var sequenceNo = 1;
            foreach (var procedureCode in pair.Value)
            {
                var procedure = procedures[procedureCode];
                var component = allComponents.SingleOrDefault(x => x.OfferVersionId == version.Id && x.ProcedureId == procedure.Id);
                if (component is null)
                {
                    component = new OfferVersionComponent
                    {
                        Id = Guid.NewGuid(),
                        OfferVersionId = version.Id,
                        ProcedureId = procedure.Id,
                        ComponentRole = OfferComponentRoleCodes.Included,
                        SequenceNo = sequenceNo,
                        DefaultExpected = true,
                        CreatedAtUtc = utcNow
                    };
                    dbContext.Set<OfferVersionComponent>().Add(component);
                    allComponents.Add(component);
                }
                else
                {
                    component.ComponentRole = OfferComponentRoleCodes.Included;
                    component.SequenceNo = sequenceNo;
                    component.DefaultExpected = true;
                }

                sequenceNo++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task EnsurePricingAsync(
        AppDbContext dbContext,
        TaxonomyLookup taxonomy,
        IReadOnlyDictionary<string, CommercialOffer> offers,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var ruleSet = await dbContext.Set<PriceRuleSet>()
            .Where(x => x.Status == RuleSetStatusCodes.Published)
            .OrderByDescending(x => x.ValidFromUtc)
            .ThenByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (ruleSet is null)
        {
            var nextVersion = (await dbContext.Set<PriceRuleSet>().MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
            ruleSet = new PriceRuleSet
            {
                Id = Guid.NewGuid(),
                VersionNo = nextVersion,
                Status = RuleSetStatusCodes.Published,
                ValidFromUtc = utcNow,
                CreatedAtUtc = utcNow,
                PublishedAtUtc = utcNow
            };
            dbContext.Set<PriceRuleSet>().Add(ruleSet);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var rules = await dbContext.Set<PriceRule>()
            .Where(x => x.RuleSetId == ruleSet.Id)
            .ToListAsync(cancellationToken);
        var conditions = await dbContext.Set<PriceRuleCondition>()
            .Where(x => rules.Select(r => r.Id).Contains(x.PriceRuleId))
            .ToListAsync(cancellationToken);

        foreach (var seed in BuildPriceSeeds())
        {
            var offerId = offers[seed.OfferCode].Id;
            var resolved = seed.Resolve(taxonomy);
            var existingRule = FindMatchingRule(rules, conditions, ruleSet.Id, offerId, resolved);
            if (existingRule is null)
            {
                var rule = new PriceRule
                {
                    Id = Guid.NewGuid(),
                    RuleSetId = ruleSet.Id,
                    OfferId = offerId,
                    Priority = seed.Priority,
                    SpecificityScore = resolved.SpecificityScore,
                    ActionType = PriceRuleActionTypes.FixedAmount,
                    FixedAmount = seed.Amount,
                    Currency = Currency,
                    CreatedAtUtc = utcNow
                };

                var condition = new PriceRuleCondition
                {
                    Id = Guid.NewGuid(),
                    PriceRuleId = rule.Id,
                    AnimalTypeId = resolved.AnimalTypeId,
                    BreedId = resolved.BreedId,
                    BreedGroupId = resolved.BreedGroupId,
                    CoatTypeId = resolved.CoatTypeId,
                    SizeCategoryId = resolved.SizeCategoryId
                };

                dbContext.Set<PriceRule>().Add(rule);
                dbContext.Set<PriceRuleCondition>().Add(condition);
                rules.Add(rule);
                conditions.Add(condition);
            }
            else
            {
                existingRule.Rule.Priority = seed.Priority;
                existingRule.Rule.SpecificityScore = resolved.SpecificityScore;
                existingRule.Rule.ActionType = PriceRuleActionTypes.FixedAmount;
                existingRule.Rule.FixedAmount = seed.Amount;
                existingRule.Rule.Currency = Currency;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDurationsAsync(
        AppDbContext dbContext,
        TaxonomyLookup taxonomy,
        IReadOnlyDictionary<string, CommercialOffer> offers,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var ruleSet = await dbContext.Set<DurationRuleSet>()
            .Where(x => x.Status == RuleSetStatusCodes.Published)
            .OrderByDescending(x => x.ValidFromUtc)
            .ThenByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (ruleSet is null)
        {
            var nextVersion = (await dbContext.Set<DurationRuleSet>().MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
            ruleSet = new DurationRuleSet
            {
                Id = Guid.NewGuid(),
                VersionNo = nextVersion,
                Status = RuleSetStatusCodes.Published,
                ValidFromUtc = utcNow,
                CreatedAtUtc = utcNow,
                PublishedAtUtc = utcNow
            };
            dbContext.Set<DurationRuleSet>().Add(ruleSet);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var rules = await dbContext.Set<DurationRule>()
            .Where(x => x.RuleSetId == ruleSet.Id)
            .ToListAsync(cancellationToken);
        var conditions = await dbContext.Set<DurationRuleCondition>()
            .Where(x => rules.Select(r => r.Id).Contains(x.DurationRuleId))
            .ToListAsync(cancellationToken);

        foreach (var seed in BuildDurationSeeds())
        {
            var offerId = offers[seed.OfferCode].Id;
            var resolved = seed.Resolve(taxonomy);
            var existingRule = FindMatchingRule(rules, conditions, ruleSet.Id, offerId, resolved);
            if (existingRule is null)
            {
                var rule = new DurationRule
                {
                    Id = Guid.NewGuid(),
                    RuleSetId = ruleSet.Id,
                    OfferId = offerId,
                    Priority = seed.Priority,
                    SpecificityScore = resolved.SpecificityScore,
                    BaseMinutes = seed.BaseMinutes,
                    BufferBeforeMinutes = seed.BufferBeforeMinutes,
                    BufferAfterMinutes = seed.BufferAfterMinutes,
                    CreatedAtUtc = utcNow
                };

                var condition = new DurationRuleCondition
                {
                    Id = Guid.NewGuid(),
                    DurationRuleId = rule.Id,
                    AnimalTypeId = resolved.AnimalTypeId,
                    BreedId = resolved.BreedId,
                    BreedGroupId = resolved.BreedGroupId,
                    CoatTypeId = resolved.CoatTypeId,
                    SizeCategoryId = resolved.SizeCategoryId
                };

                dbContext.Set<DurationRule>().Add(rule);
                dbContext.Set<DurationRuleCondition>().Add(condition);
                rules.Add(rule);
                conditions.Add(condition);
            }
            else
            {
                existingRule.Rule.Priority = seed.Priority;
                existingRule.Rule.SpecificityScore = resolved.SpecificityScore;
                existingRule.Rule.BaseMinutes = seed.BaseMinutes;
                existingRule.Rule.BufferBeforeMinutes = seed.BufferBeforeMinutes;
                existingRule.Rule.BufferAfterMinutes = seed.BufferAfterMinutes;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RuleMatch<PriceRule, PriceRuleCondition>? FindMatchingRule(
        IReadOnlyCollection<PriceRule> rules,
        IReadOnlyCollection<PriceRuleCondition> conditions,
        Guid ruleSetId,
        Guid offerId,
        ResolvedCondition resolved)
    {
        return rules
            .Where(x => x.RuleSetId == ruleSetId && x.OfferId == offerId)
            .Select(rule => new RuleMatch<PriceRule, PriceRuleCondition>(rule, conditions.Single(x => x.PriceRuleId == rule.Id)))
            .SingleOrDefault(x => Matches(x.Condition.AnimalTypeId, resolved.AnimalTypeId)
                                  && Matches(x.Condition.BreedId, resolved.BreedId)
                                  && Matches(x.Condition.BreedGroupId, resolved.BreedGroupId)
                                  && Matches(x.Condition.CoatTypeId, resolved.CoatTypeId)
                                  && Matches(x.Condition.SizeCategoryId, resolved.SizeCategoryId));
    }

    private static RuleMatch<DurationRule, DurationRuleCondition>? FindMatchingRule(
        IReadOnlyCollection<DurationRule> rules,
        IReadOnlyCollection<DurationRuleCondition> conditions,
        Guid ruleSetId,
        Guid offerId,
        ResolvedCondition resolved)
    {
        return rules
            .Where(x => x.RuleSetId == ruleSetId && x.OfferId == offerId)
            .Select(rule => new RuleMatch<DurationRule, DurationRuleCondition>(rule, conditions.Single(x => x.DurationRuleId == rule.Id)))
            .SingleOrDefault(x => Matches(x.Condition.AnimalTypeId, resolved.AnimalTypeId)
                                  && Matches(x.Condition.BreedId, resolved.BreedId)
                                  && Matches(x.Condition.BreedGroupId, resolved.BreedGroupId)
                                  && Matches(x.Condition.CoatTypeId, resolved.CoatTypeId)
                                  && Matches(x.Condition.SizeCategoryId, resolved.SizeCategoryId));
    }

    private static bool Matches(Guid? left, Guid? right) => left == right;

    private static IReadOnlyCollection<PriceSeed> BuildPriceSeeds()
    {
        return new[]
        {
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "YORKSHIRE_TERRIER"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "BIEWER_TERRIER"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 800m, breedCode: "MALTIPOO_F1"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 800m, breedCode: "MALTIPOO_F2"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 550m, breedCode: "CHIHUAHUA"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "POMERANIAN"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "GERMAN_SPITZ"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 800m, breedCode: "JAPANESE_SPITZ"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 800m, breedCode: "POODLE_TOY"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 800m, breedCode: "BICHON_FRISE"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "PEKINGESE"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "MALTESE"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 700m, breedCode: "SHIH_TZU"),
            new PriceSeed("DOG_FULL_GROOMING", 10, 900m, breedCode: "COCKER_SPANIEL"),
            new PriceSeed("DOG_FULL_GROOMING", 20, 900m, breedCode: "CAVALIER_KING_CHARLES_SPANIEL"),
            new PriceSeed("DOG_FULL_GROOMING", 100, 800m, animalTypeCode: "DOG"),

            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 500m, breedCode: "TOY_TERRIER"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 500m, breedCode: "CHIHUAHUA"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 600m, breedGroupCode: "DOG_DACHSHUND"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 600m, breedCode: "FOX_TERRIER"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 600m, breedCode: "JACK_RUSSELL_TERRIER"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 600m, breedCode: "PUG"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 650m, breedCode: "FRENCH_BULLDOG"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 700m, breedCode: "BEAGLE"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 700m, breedCode: "WELSH_CORGI_PEMBROKE"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 700m, breedCode: "WELSH_CORGI_CARDIGAN"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 700m, breedCode: "SHIBA_INU"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 850m, breedCode: "ENGLISH_BULLDOG"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 1000m, breedGroupCode: "DOG_RETRIEVER"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 1000m, breedCode: "HUSKY"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 1300m, breedCode: "GERMAN_SHEPHERD"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 1100m, breedCode: "ALASKAN_MALAMUTE"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 1500m, breedCode: "AKITA"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 10, 1400m, breedCode: "SAMOYED"),
            new PriceSeed("DOG_EXPRESS_DESHEDDING", 100, 700m, animalTypeCode: "DOG"),

            new PriceSeed("CAT_EXPRESS_DESHEDDING", 10, 1100m, animalTypeCode: "CAT", breedCode: "MAINE_COON"),
            new PriceSeed("CAT_EXPRESS_DESHEDDING", 20, 800m, animalTypeCode: "CAT", coatTypeCode: "LONG_COAT"),
            new PriceSeed("CAT_EXPRESS_DESHEDDING", 20, 700m, animalTypeCode: "CAT", coatTypeCode: "SHORT_COAT"),
            new PriceSeed("CAT_HAIRCUT", 10, 700m, animalTypeCode: "CAT"),

            new PriceSeed("DELIVERY_ADDON", 10, 150m),
            new PriceSeed("NAIL_TRIM_ONLY", 10, 50m),
            new PriceSeed("EAR_CLEANING_ONLY", 10, 50m)
        };
    }

    private static IReadOnlyCollection<DurationSeed> BuildDurationSeeds()
    {
        return new[]
        {
            new DurationSeed("DOG_FULL_GROOMING", 10, 80, 10, 10, breedCode: "YORKSHIRE_TERRIER"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 80, 10, 10, breedCode: "BIEWER_TERRIER"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 95, 10, 15, breedCode: "MALTIPOO_F1"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 95, 10, 15, breedCode: "MALTIPOO_F2"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 60, 5, 10, breedCode: "CHIHUAHUA"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 85, 10, 10, breedCode: "POMERANIAN"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 90, 10, 10, breedCode: "GERMAN_SPITZ"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 95, 10, 10, breedCode: "JAPANESE_SPITZ"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 95, 10, 15, breedCode: "POODLE_TOY"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 95, 10, 15, breedCode: "BICHON_FRISE"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 85, 10, 10, breedCode: "PEKINGESE"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 80, 10, 10, breedCode: "MALTESE"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 85, 10, 10, breedCode: "SHIH_TZU"),
            new DurationSeed("DOG_FULL_GROOMING", 10, 110, 10, 15, breedCode: "COCKER_SPANIEL"),
            new DurationSeed("DOG_FULL_GROOMING", 20, 110, 10, 15, breedCode: "CAVALIER_KING_CHARLES_SPANIEL"),
            new DurationSeed("DOG_FULL_GROOMING", 100, 90, 10, 10, animalTypeCode: "DOG"),

            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 50, 5, 10, breedCode: "TOY_TERRIER"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 50, 5, 10, breedCode: "CHIHUAHUA"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 60, 5, 10, breedGroupCode: "DOG_DACHSHUND"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 60, 5, 10, breedCode: "FOX_TERRIER"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 60, 5, 10, breedCode: "JACK_RUSSELL_TERRIER"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 60, 5, 10, breedCode: "PUG"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 65, 5, 10, breedCode: "FRENCH_BULLDOG"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 75, 5, 10, breedCode: "BEAGLE"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 80, 10, 10, breedCode: "WELSH_CORGI_PEMBROKE"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 80, 10, 10, breedCode: "WELSH_CORGI_CARDIGAN"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 80, 10, 10, breedCode: "SHIBA_INU"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 85, 10, 10, breedCode: "ENGLISH_BULLDOG"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 110, 10, 15, breedGroupCode: "DOG_RETRIEVER"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 120, 10, 15, breedCode: "HUSKY"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 140, 10, 20, breedCode: "GERMAN_SHEPHERD"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 130, 10, 20, breedCode: "ALASKAN_MALAMUTE"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 150, 10, 20, breedCode: "AKITA"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 10, 145, 10, 20, breedCode: "SAMOYED"),
            new DurationSeed("DOG_EXPRESS_DESHEDDING", 100, 80, 10, 10, animalTypeCode: "DOG"),

            new DurationSeed("CAT_EXPRESS_DESHEDDING", 10, 120, 10, 20, animalTypeCode: "CAT", breedCode: "MAINE_COON"),
            new DurationSeed("CAT_EXPRESS_DESHEDDING", 20, 90, 10, 15, animalTypeCode: "CAT", coatTypeCode: "LONG_COAT"),
            new DurationSeed("CAT_EXPRESS_DESHEDDING", 20, 75, 10, 10, animalTypeCode: "CAT", coatTypeCode: "SHORT_COAT"),
            new DurationSeed("CAT_HAIRCUT", 10, 105, 10, 15, animalTypeCode: "CAT"),

            new DurationSeed("DELIVERY_ADDON", 10, 0, 0, 0),
            new DurationSeed("NAIL_TRIM_ONLY", 10, 10, 0, 5),
            new DurationSeed("EAR_CLEANING_ONLY", 10, 10, 0, 5)
        };
    }

    private sealed record TaxonomyLookup(
        IReadOnlyDictionary<string, AnimalType> AnimalTypes,
        IReadOnlyDictionary<string, BreedGroup> BreedGroups,
        IReadOnlyDictionary<string, Breed> Breeds,
        IReadOnlyDictionary<string, CoatType> CoatTypes,
        IReadOnlyDictionary<string, SizeCategory> SizeCategories)
    {
        public ResolvedCondition Resolve(string? animalTypeCode, string? breedCode, string? breedGroupCode, string? coatTypeCode, string? sizeCategoryCode)
        {
            var animalTypeId = ResolveOptional(AnimalTypes, animalTypeCode);
            var breedId = ResolveOptional(Breeds, breedCode);
            var breedGroupId = ResolveOptional(BreedGroups, breedGroupCode);
            var coatTypeId = ResolveOptional(CoatTypes, coatTypeCode);
            var sizeCategoryId = ResolveOptional(SizeCategories, sizeCategoryCode);
            var specificityScore = new Guid?[] { animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId }.Count(x => x.HasValue);
            return new ResolvedCondition(animalTypeId, breedId, breedGroupId, coatTypeId, sizeCategoryId, specificityScore);
        }

        private static Guid? ResolveOptional<T>(IReadOnlyDictionary<string, T> source, string? code) where T : class
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return source.TryGetValue(code, out var entity)
                ? entity switch
                {
                    AnimalType animalType => animalType.Id,
                    BreedGroup breedGroup => breedGroup.Id,
                    Breed breed => breed.Id,
                    CoatType coatType => coatType.Id,
                    SizeCategory sizeCategory => sizeCategory.Id,
                    _ => throw new InvalidOperationException($"Unsupported taxonomy entity type '{typeof(T).Name}'.")
                }
                : throw new InvalidOperationException($"Required taxonomy code '{code}' was not found for the development demo seed.");
        }
    }

    private sealed record ResolvedCondition(Guid? AnimalTypeId, Guid? BreedId, Guid? BreedGroupId, Guid? CoatTypeId, Guid? SizeCategoryId, int SpecificityScore);
    private sealed record ProcedureSeed(string Code, string Name);
    private sealed record OfferSeed(string Code, string OfferType, string DisplayName);
    private sealed record PriceSeed(string OfferCode, int Priority, decimal Amount, string? animalTypeCode = null, string? breedCode = null, string? breedGroupCode = null, string? coatTypeCode = null, string? sizeCategoryCode = null)
    {
        public string? AnimalTypeCode { get; } = animalTypeCode;
        public string? BreedCode { get; } = breedCode;
        public string? BreedGroupCode { get; } = breedGroupCode;
        public string? CoatTypeCode { get; } = coatTypeCode;
        public string? SizeCategoryCode { get; } = sizeCategoryCode;

        public ResolvedCondition Resolve(TaxonomyLookup taxonomy) => taxonomy.Resolve(AnimalTypeCode, BreedCode, BreedGroupCode, CoatTypeCode, SizeCategoryCode);
    }

    private sealed record DurationSeed(string OfferCode, int Priority, int BaseMinutes, int BufferBeforeMinutes, int BufferAfterMinutes, string? animalTypeCode = null, string? breedCode = null, string? breedGroupCode = null, string? coatTypeCode = null, string? sizeCategoryCode = null)
    {
        public string? AnimalTypeCode { get; } = animalTypeCode;
        public string? BreedCode { get; } = breedCode;
        public string? BreedGroupCode { get; } = breedGroupCode;
        public string? CoatTypeCode { get; } = coatTypeCode;
        public string? SizeCategoryCode { get; } = sizeCategoryCode;

        public ResolvedCondition Resolve(TaxonomyLookup taxonomy) => taxonomy.Resolve(AnimalTypeCode, BreedCode, BreedGroupCode, CoatTypeCode, SizeCategoryCode);
    }

    private sealed record RuleMatch<TRule, TCondition>(TRule Rule, TCondition Condition);
}

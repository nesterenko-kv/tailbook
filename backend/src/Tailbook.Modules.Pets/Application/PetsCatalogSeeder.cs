using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Pets.Domain;

namespace Tailbook.Modules.Pets.Application;

public sealed class PetsCatalogSeeder : IDataSeeder
{
    public int Order => 20;

    public async Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await EnsureAnimalTypesAsync(dbContext, cancellationToken);
        await EnsureBreedGroupsAsync(dbContext, cancellationToken);
        await EnsureBreedsAsync(dbContext, cancellationToken);
        await EnsureCoatTypesAsync(dbContext, cancellationToken);
        await EnsureBreedAllowedCoatTypesAsync(dbContext, cancellationToken);
        await EnsureSizeCategoriesAsync(dbContext, cancellationToken);
        await EnsureBreedAllowedSizeCategoriesAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureAnimalTypesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await EnsureEntityAsync(dbContext, x => x.Code, new AnimalType { Id = Guid.NewGuid(), Code = "DOG", Name = "Dog" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new AnimalType { Id = Guid.NewGuid(), Code = "CAT", Name = "Cat" }, cancellationToken);
    }

    private static async Task EnsureBreedGroupsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var dog = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "DOG", cancellationToken);
        var cat = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "CAT", cancellationToken);

        var breedGroups = new[]
        {
            new BreedGroupSeed(dog.Id, "DOG_MIXED", "Mixed / No Breed"),
            new BreedGroupSeed(dog.Id, "DOG_SPITZ", "Spitz"),
            new BreedGroupSeed(dog.Id, "DOG_POODLE_DOODLE", "Poodle / Doodle"),
            new BreedGroupSeed(dog.Id, "DOG_RETRIEVER", "Retriever"),
            new BreedGroupSeed(dog.Id, "DOG_SHEPHERD", "Shepherd"),
            new BreedGroupSeed(dog.Id, "DOG_TERRIER", "Terrier"),
            new BreedGroupSeed(dog.Id, "DOG_COMPANION_TOY", "Companion / Toy"),
            new BreedGroupSeed(dog.Id, "DOG_BICHON", "Bichon"),
            new BreedGroupSeed(dog.Id, "DOG_DACHSHUND", "Dachshund"),
            new BreedGroupSeed(dog.Id, "DOG_BULLDOG_MASTIFF", "Bulldog / Mastiff"),
            new BreedGroupSeed(dog.Id, "DOG_HOUND", "Hound"),
            new BreedGroupSeed(dog.Id, "DOG_WORKING", "Working"),

            new BreedGroupSeed(cat.Id, "CAT_MIXED", "Mixed / No Breed"),
            new BreedGroupSeed(cat.Id, "CAT_SHORT_HAIR", "Short Hair Cats"),
            new BreedGroupSeed(cat.Id, "CAT_LONG_HAIR", "Long Hair Cats"),
            new BreedGroupSeed(cat.Id, "CAT_HAIRLESS", "Hairless Cats")
        };

        foreach (var item in breedGroups)
        {
            await EnsureEntityAsync(
                dbContext,
                x => x.Code,
                new BreedGroup
                {
                    Id = Guid.NewGuid(),
                    AnimalTypeId = item.AnimalTypeId,
                    Code = item.Code,
                    Name = item.Name
                },
                cancellationToken);
        }
    }

    private static async Task EnsureBreedsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var dog = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "DOG", cancellationToken);
        var cat = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "CAT", cancellationToken);
        var groups = await dbContext.Set<BreedGroup>()
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var breeds = new[]
        {
            new BreedSeed(dog.Id, groups["DOG_MIXED"].Id, "DOG_MIXED", "Mixed Breed / No Breed"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "POODLE_TOY", "Toy Poodle"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "POODLE_MINIATURE", "Miniature Poodle"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "POODLE_STANDARD", "Standard Poodle"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "MALTIPOO_F1", "Maltipoo F1"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "MALTIPOO_F2", "Maltipoo F2"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "CAVAPOO_F1", "Cavapoo F1"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "CAVAPOO_F2", "Cavapoo F2"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "COCKAPOO", "Cockapoo"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "LABRADOODLE", "Labradoodle"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "GOLDENDOODLE", "Goldendoodle"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "YORKIPOO", "Yorkipoo"),
            new BreedSeed(dog.Id, groups["DOG_POODLE_DOODLE"].Id, "SHIH_POO", "Shih-Poo"),

            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "POMERANIAN", "Pomeranian"),
            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "JAPANESE_SPITZ", "Japanese Spitz"),
            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "SAMOYED", "Samoyed"),
            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "SHIBA_INU", "Shiba Inu"),
            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "AKITA", "Akita"),
            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "HUSKY", "Siberian Husky"),
            new BreedSeed(dog.Id, groups["DOG_SPITZ"].Id, "POMSKY", "Pomsky"),

            new BreedSeed(dog.Id, groups["DOG_RETRIEVER"].Id, "LABRADOR_RETRIEVER", "Labrador Retriever"),
            new BreedSeed(dog.Id, groups["DOG_RETRIEVER"].Id, "GOLDEN_RETRIEVER", "Golden Retriever"),
            new BreedSeed(dog.Id, groups["DOG_RETRIEVER"].Id, "NOVA_SCOTIA_DUCK_TOLLING_RETRIEVER", "Nova Scotia Duck Tolling Retriever"),

            new BreedSeed(dog.Id, groups["DOG_SHEPHERD"].Id, "GERMAN_SHEPHERD", "German Shepherd"),
            new BreedSeed(dog.Id, groups["DOG_SHEPHERD"].Id, "BELGIAN_SHEPHERD", "Belgian Shepherd"),
            new BreedSeed(dog.Id, groups["DOG_SHEPHERD"].Id, "AUSTRALIAN_SHEPHERD", "Australian Shepherd"),
            new BreedSeed(dog.Id, groups["DOG_SHEPHERD"].Id, "BORDER_COLLIE", "Border Collie"),
            new BreedSeed(dog.Id, groups["DOG_SHEPHERD"].Id, "WELSH_CORGI_PEMBROKE", "Welsh Corgi Pembroke"),
            new BreedSeed(dog.Id, groups["DOG_SHEPHERD"].Id, "WELSH_CORGI_CARDIGAN", "Welsh Corgi Cardigan"),

            new BreedSeed(dog.Id, groups["DOG_TERRIER"].Id, "YORKSHIRE_TERRIER", "Yorkshire Terrier"),
            new BreedSeed(dog.Id, groups["DOG_TERRIER"].Id, "WEST_HIGHLAND_WHITE_TERRIER", "West Highland White Terrier"),
            new BreedSeed(dog.Id, groups["DOG_TERRIER"].Id, "JACK_RUSSELL_TERRIER", "Jack Russell Terrier"),
            new BreedSeed(dog.Id, groups["DOG_TERRIER"].Id, "FOX_TERRIER", "Fox Terrier"),
            new BreedSeed(dog.Id, groups["DOG_TERRIER"].Id, "SCOTTISH_TERRIER", "Scottish Terrier"),

            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "CHIHUAHUA", "Chihuahua"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "PAPILLON", "Papillon"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "PUG", "Pug"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "PEKINGESE", "Pekingese"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "SHIH_TZU", "Shih Tzu"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "CAVALIER_KING_CHARLES_SPANIEL", "Cavalier King Charles Spaniel"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "CHINESE_CRESTED", "Chinese Crested"),
            new BreedSeed(dog.Id, groups["DOG_COMPANION_TOY"].Id, "MINIATURE_PINSCHER", "Miniature Pinscher"),

            new BreedSeed(dog.Id, groups["DOG_BICHON"].Id, "MALTESE", "Maltese"),
            new BreedSeed(dog.Id, groups["DOG_BICHON"].Id, "BICHON_FRISE", "Bichon Frise"),
            new BreedSeed(dog.Id, groups["DOG_BICHON"].Id, "HAVANESE", "Havanese"),
            new BreedSeed(dog.Id, groups["DOG_BICHON"].Id, "COTON_DE_TULEAR", "Coton de Tulear"),

            new BreedSeed(dog.Id, groups["DOG_DACHSHUND"].Id, "DACHSHUND_SMOOTH", "Dachshund Smooth"),
            new BreedSeed(dog.Id, groups["DOG_DACHSHUND"].Id, "DACHSHUND_LONG_HAIRED", "Dachshund Long-Haired"),
            new BreedSeed(dog.Id, groups["DOG_DACHSHUND"].Id, "DACHSHUND_WIRE_HAIRED", "Dachshund Wire-Haired"),

            new BreedSeed(dog.Id, groups["DOG_BULLDOG_MASTIFF"].Id, "FRENCH_BULLDOG", "French Bulldog"),
            new BreedSeed(dog.Id, groups["DOG_BULLDOG_MASTIFF"].Id, "ENGLISH_BULLDOG", "English Bulldog"),
            new BreedSeed(dog.Id, groups["DOG_BULLDOG_MASTIFF"].Id, "BOSTON_TERRIER", "Boston Terrier"),
            new BreedSeed(dog.Id, groups["DOG_BULLDOG_MASTIFF"].Id, "CANE_CORSO", "Cane Corso"),

            new BreedSeed(dog.Id, groups["DOG_HOUND"].Id, "BEAGLE", "Beagle"),
            new BreedSeed(dog.Id, groups["DOG_HOUND"].Id, "BASSET_HOUND", "Basset Hound"),
            new BreedSeed(dog.Id, groups["DOG_HOUND"].Id, "WHIPPET", "Whippet"),
            new BreedSeed(dog.Id, groups["DOG_HOUND"].Id, "GREYHOUND", "Greyhound"),

            new BreedSeed(dog.Id, groups["DOG_WORKING"].Id, "BERNESE_MOUNTAIN_DOG", "Bernese Mountain Dog"),
            new BreedSeed(dog.Id, groups["DOG_WORKING"].Id, "NEWFOUNDLAND", "Newfoundland"),
            new BreedSeed(dog.Id, groups["DOG_WORKING"].Id, "DOBERMAN", "Doberman"),
            new BreedSeed(dog.Id, groups["DOG_WORKING"].Id, "ROTTWEILER", "Rottweiler"),

            new BreedSeed(cat.Id, groups["CAT_MIXED"].Id, "CAT_MIXED", "Mixed Breed / No Breed"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "BRITISH_SHORTHAIR", "British Shorthair"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "SCOTTISH_STRAIGHT", "Scottish Straight"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "SCOTTISH_FOLD", "Scottish Fold"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "BENGAL", "Bengal"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "ABYSSINIAN", "Abyssinian"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "BURMESE", "Burmese"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "RUSSIAN_BLUE", "Russian Blue"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "EXOTIC_SHORTHAIR", "Exotic Shorthair"),
            new BreedSeed(cat.Id, groups["CAT_SHORT_HAIR"].Id, "AMERICAN_SHORTHAIR", "American Shorthair"),

            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "SIBERIAN_CAT", "Siberian Cat"),
            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "MAINE_COON", "Maine Coon"),
            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "PERSIAN", "Persian"),
            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "NORWEGIAN_FOREST_CAT", "Norwegian Forest Cat"),
            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "RAGDOLL", "Ragdoll"),
            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "NEVA_MASQUERADE", "Neva Masquerade"),
            new BreedSeed(cat.Id, groups["CAT_LONG_HAIR"].Id, "TURKISH_ANGORA", "Turkish Angora"),

            new BreedSeed(cat.Id, groups["CAT_HAIRLESS"].Id, "SPHYNX", "Sphynx"),
            new BreedSeed(cat.Id, groups["CAT_HAIRLESS"].Id, "DONSKOY", "Donskoy")
        };

        foreach (var item in breeds)
        {
            await EnsureEntityAsync(
                dbContext,
                x => x.Code,
                new Breed
                {
                    Id = Guid.NewGuid(),
                    AnimalTypeId = item.AnimalTypeId,
                    BreedGroupId = item.BreedGroupId,
                    Code = item.Code,
                    Name = item.Name
                },
                cancellationToken);
        }
    }

    private static async Task EnsureCoatTypesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var dog = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "DOG", cancellationToken);

        await EnsureCoatTypeAsync(dbContext, "DOUBLE_COAT", "Double Coat", dog.Id, cancellationToken);
        await EnsureCoatTypeAsync(dbContext, "CURLY_COAT", "Curly Coat", dog.Id, cancellationToken);
        await EnsureCoatTypeAsync(dbContext, "SHORT_COAT", "Short Coat", null, cancellationToken);
        await EnsureCoatTypeAsync(dbContext, "LONG_COAT", "Long Coat", null, cancellationToken);
    }

    private static async Task EnsureBreedAllowedCoatTypesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var breeds = await dbContext.Set<Breed>().ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var coatTypes = await dbContext.Set<CoatType>().ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingMappings = await dbContext.Set<BreedAllowedCoatType>().ToListAsync(cancellationToken);
        var existingPairs = existingMappings
            .Select(x => (x.BreedId, x.CoatTypeId))
            .ToHashSet();

        var mappings = new List<BreedAllowedCoatTypeSeed>();

        AddMappings(new[] { "DOG_MIXED" }, "SHORT_COAT", "LONG_COAT", "DOUBLE_COAT", "CURLY_COAT");
        AddMappings(new[] { "POODLE_TOY", "POODLE_MINIATURE", "POODLE_STANDARD", "MALTIPOO_F1", "MALTIPOO_F2", "CAVAPOO_F1", "CAVAPOO_F2", "COCKAPOO", "LABRADOODLE", "GOLDENDOODLE", "YORKIPOO", "SHIH_POO" }, "CURLY_COAT");
        AddMappings(new[] { "POMERANIAN", "JAPANESE_SPITZ", "SAMOYED", "SHIBA_INU", "AKITA", "HUSKY", "POMSKY" }, "DOUBLE_COAT");
        AddMappings(new[] { "LABRADOR_RETRIEVER" }, "SHORT_COAT");
        AddMappings(new[] { "GOLDEN_RETRIEVER" }, "LONG_COAT");
        AddMappings(new[] { "NOVA_SCOTIA_DUCK_TOLLING_RETRIEVER" }, "DOUBLE_COAT");
        AddMappings(new[] { "GERMAN_SHEPHERD", "BELGIAN_SHEPHERD", "AUSTRALIAN_SHEPHERD", "BORDER_COLLIE", "WELSH_CORGI_PEMBROKE", "WELSH_CORGI_CARDIGAN" }, "DOUBLE_COAT");
        AddMappings(new[] { "YORKSHIRE_TERRIER", "WEST_HIGHLAND_WHITE_TERRIER", "SCOTTISH_TERRIER" }, "LONG_COAT");
        AddMappings(new[] { "JACK_RUSSELL_TERRIER", "FOX_TERRIER" }, "SHORT_COAT");
        AddMappings(new[] { "CHIHUAHUA", "PUG", "CHINESE_CRESTED", "MINIATURE_PINSCHER" }, "SHORT_COAT");
        AddMappings(new[] { "PAPILLON", "PEKINGESE", "SHIH_TZU", "CAVALIER_KING_CHARLES_SPANIEL" }, "LONG_COAT");
        AddMappings(new[] { "MALTESE", "HAVANESE", "COTON_DE_TULEAR" }, "LONG_COAT");
        AddMappings(new[] { "BICHON_FRISE" }, "CURLY_COAT");
        AddMappings(new[] { "DACHSHUND_SMOOTH", "FRENCH_BULLDOG", "ENGLISH_BULLDOG", "BOSTON_TERRIER", "CANE_CORSO", "BEAGLE", "BASSET_HOUND", "WHIPPET", "GREYHOUND", "DOBERMAN", "ROTTWEILER" }, "SHORT_COAT");
        AddMappings(new[] { "DACHSHUND_LONG_HAIRED", "DACHSHUND_WIRE_HAIRED" }, "LONG_COAT");
        AddMappings(new[] { "BERNESE_MOUNTAIN_DOG", "NEWFOUNDLAND" }, "DOUBLE_COAT");

        AddMappings(new[] { "CAT_MIXED" }, "SHORT_COAT", "LONG_COAT");
        AddMappings(new[] { "BRITISH_SHORTHAIR", "SCOTTISH_STRAIGHT", "SCOTTISH_FOLD", "BENGAL", "ABYSSINIAN", "BURMESE", "RUSSIAN_BLUE", "EXOTIC_SHORTHAIR", "AMERICAN_SHORTHAIR" }, "SHORT_COAT");
        AddMappings(new[] { "SIBERIAN_CAT", "MAINE_COON", "PERSIAN", "NORWEGIAN_FOREST_CAT", "RAGDOLL", "NEVA_MASQUERADE", "TURKISH_ANGORA" }, "LONG_COAT");

        var pending = new List<BreedAllowedCoatType>();
        foreach (var mapping in mappings)
        {
            var breed = breeds.TryGetValue(mapping.BreedCode, out var breedValue)
                ? breedValue
                : throw new InvalidOperationException($"Seed breed '{mapping.BreedCode}' was not found.");
            var coatType = coatTypes.TryGetValue(mapping.CoatTypeCode, out var coatTypeValue)
                ? coatTypeValue
                : throw new InvalidOperationException($"Seed coat type '{mapping.CoatTypeCode}' was not found.");

            if (existingPairs.Add((breed.Id, coatType.Id)))
            {
                pending.Add(new BreedAllowedCoatType
                {
                    BreedId = breed.Id,
                    CoatTypeId = coatType.Id
                });
            }
        }

        if (pending.Count > 0)
        {
            dbContext.Set<BreedAllowedCoatType>().AddRange(pending);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        void AddMappings(IEnumerable<string> breedCodes, params string[] coatTypeCodes)
        {
            foreach (var breedCode in breedCodes)
            {
                foreach (var coatTypeCode in coatTypeCodes)
                {
                    mappings.Add(new BreedAllowedCoatTypeSeed(breedCode, coatTypeCode));
                }
            }
        }
    }

    private static async Task EnsureSizeCategoriesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        await EnsureSizeCategoryAsync(dbContext, "TEACUP", "Teacup", null, null, 1.8m, cancellationToken);
        await EnsureSizeCategoryAsync(dbContext, "MINIATURE", "Miniature", null, 1.3m, 5.5m, cancellationToken);
        await EnsureSizeCategoryAsync(dbContext, "TOY", "Toy", null, 2.2m, 5.5m, cancellationToken);
        await EnsureSizeCategoryAsync(dbContext, "SMALL", "Small Breeds", null, null, 10m, cancellationToken);
        await EnsureSizeCategoryAsync(dbContext, "MEDIUM", "Medium Breeds", null, 11m, 26m, cancellationToken);
        await EnsureSizeCategoryAsync(dbContext, "LARGE", "Large Breeds", null, 26m, 45m, cancellationToken);
        await EnsureSizeCategoryAsync(dbContext, "GIANT", "Giant Breeds", null, 45m, null, cancellationToken);
    }

    private static async Task EnsureBreedAllowedSizeCategoriesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var breeds = await dbContext.Set<Breed>().ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var sizeCategories = await dbContext.Set<SizeCategory>().ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var existingMappings = await dbContext.Set<BreedAllowedSizeCategory>().ToListAsync(cancellationToken);
        var existingPairs = existingMappings
            .Select(x => (x.BreedId, x.SizeCategoryId))
            .ToHashSet();

        var mappings = new List<BreedAllowedSizeCategorySeed>();

        AddMappings(new[] { "DOG_MIXED" }, "TEACUP", "MINIATURE", "TOY", "SMALL", "MEDIUM", "LARGE", "GIANT");
        AddMappings(new[] { "POODLE_TOY", "POODLE_MINIATURE", "MALTIPOO_F1", "MALTIPOO_F2", "YORKIPOO", "SHIH_POO" }, "MINIATURE", "TOY", "SMALL");
        AddMappings(new[] { "POODLE_STANDARD" }, "MEDIUM", "LARGE");
        AddMappings(new[] { "CAVAPOO_F1", "CAVAPOO_F2", "COCKAPOO" }, "SMALL", "MEDIUM");
        AddMappings(new[] { "LABRADOODLE", "GOLDENDOODLE" }, "MEDIUM", "LARGE");

        AddMappings(new[] { "POMERANIAN" }, "MINIATURE", "TOY", "SMALL");
        AddMappings(new[] { "JAPANESE_SPITZ" }, "SMALL", "MEDIUM");
        AddMappings(new[] { "SAMOYED" }, "LARGE");
        AddMappings(new[] { "SHIBA_INU" }, "MEDIUM");
        AddMappings(new[] { "AKITA" }, "LARGE");
        AddMappings(new[] { "HUSKY" }, "MEDIUM", "LARGE");
        AddMappings(new[] { "POMSKY" }, "SMALL", "MEDIUM");

        AddMappings(new[] { "LABRADOR_RETRIEVER", "GOLDEN_RETRIEVER" }, "LARGE");
        AddMappings(new[] { "NOVA_SCOTIA_DUCK_TOLLING_RETRIEVER" }, "MEDIUM");

        AddMappings(new[] { "GERMAN_SHEPHERD", "BELGIAN_SHEPHERD" }, "LARGE");
        AddMappings(new[] { "AUSTRALIAN_SHEPHERD", "BORDER_COLLIE" }, "MEDIUM");
        AddMappings(new[] { "WELSH_CORGI_PEMBROKE", "WELSH_CORGI_CARDIGAN" }, "MEDIUM");

        AddMappings(new[] { "YORKSHIRE_TERRIER" }, "MINIATURE", "TOY", "SMALL");
        AddMappings(new[] { "WEST_HIGHLAND_WHITE_TERRIER", "JACK_RUSSELL_TERRIER", "FOX_TERRIER", "SCOTTISH_TERRIER" }, "SMALL");

        AddMappings(new[] { "CHIHUAHUA" }, "TEACUP", "MINIATURE", "TOY", "SMALL");
        AddMappings(new[] { "PAPILLON", "CHINESE_CRESTED", "MINIATURE_PINSCHER" }, "MINIATURE", "TOY", "SMALL");
        AddMappings(new[] { "PUG", "PEKINGESE", "SHIH_TZU", "CAVALIER_KING_CHARLES_SPANIEL" }, "SMALL");

        AddMappings(new[] { "MALTESE", "BICHON_FRISE", "HAVANESE", "COTON_DE_TULEAR" }, "MINIATURE", "TOY", "SMALL");

        AddMappings(new[] { "DACHSHUND_SMOOTH", "DACHSHUND_LONG_HAIRED", "DACHSHUND_WIRE_HAIRED", "FRENCH_BULLDOG", "ENGLISH_BULLDOG", "BOSTON_TERRIER", "BEAGLE" }, "SMALL");
        AddMappings(new[] { "CANE_CORSO" }, "LARGE", "GIANT");

        AddMappings(new[] { "BASSET_HOUND", "WHIPPET" }, "MEDIUM");
        AddMappings(new[] { "GREYHOUND" }, "LARGE");

        AddMappings(new[] { "BERNESE_MOUNTAIN_DOG" }, "LARGE", "GIANT");
        AddMappings(new[] { "NEWFOUNDLAND" }, "GIANT");
        AddMappings(new[] { "DOBERMAN", "ROTTWEILER" }, "LARGE");

        AddMappings(new[] { "CAT_MIXED" }, "SMALL", "MEDIUM", "LARGE");
        AddMappings(new[] { "BRITISH_SHORTHAIR" }, "MEDIUM", "LARGE");
        AddMappings(new[] { "SCOTTISH_STRAIGHT", "SCOTTISH_FOLD", "BENGAL", "ABYSSINIAN", "BURMESE", "RUSSIAN_BLUE", "EXOTIC_SHORTHAIR", "AMERICAN_SHORTHAIR" }, "SMALL", "MEDIUM");
        AddMappings(new[] { "SIBERIAN_CAT" }, "MEDIUM", "LARGE");
        AddMappings(new[] { "MAINE_COON" }, "LARGE", "GIANT");
        AddMappings(new[] { "PERSIAN", "TURKISH_ANGORA", "SPHYNX", "DONSKOY" }, "SMALL", "MEDIUM");
        AddMappings(new[] { "NORWEGIAN_FOREST_CAT", "RAGDOLL", "NEVA_MASQUERADE" }, "LARGE");

        var pending = new List<BreedAllowedSizeCategory>();
        foreach (var mapping in mappings)
        {
            var breed = breeds.TryGetValue(mapping.BreedCode, out var breedValue)
                ? breedValue
                : throw new InvalidOperationException($"Seed breed '{mapping.BreedCode}' was not found.");
            var sizeCategory = sizeCategories.TryGetValue(mapping.SizeCategoryCode, out var sizeCategoryValue)
                ? sizeCategoryValue
                : throw new InvalidOperationException($"Seed size category '{mapping.SizeCategoryCode}' was not found.");

            if (existingPairs.Add((breed.Id, sizeCategory.Id)))
            {
                pending.Add(new BreedAllowedSizeCategory
                {
                    BreedId = breed.Id,
                    SizeCategoryId = sizeCategory.Id
                });
            }
        }

        if (pending.Count > 0)
        {
            dbContext.Set<BreedAllowedSizeCategory>().AddRange(pending);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        void AddMappings(IEnumerable<string> breedCodes, params string[] sizeCategoryCodes)
        {
            foreach (var breedCode in breedCodes)
            {
                foreach (var sizeCategoryCode in sizeCategoryCodes)
                {
                    mappings.Add(new BreedAllowedSizeCategorySeed(breedCode, sizeCategoryCode));
                }
            }
        }
    }

    private static async Task EnsureCoatTypeAsync(AppDbContext dbContext, string code, string name, Guid? animalTypeId, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<CoatType>().SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (existing is null)
        {
            dbContext.Set<CoatType>().Add(new CoatType
            {
                Id = Guid.NewGuid(),
                AnimalTypeId = animalTypeId,
                Code = code,
                Name = name
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (existing.AnimalTypeId == animalTypeId && string.Equals(existing.Name, name, StringComparison.Ordinal))
        {
            return;
        }

        existing.AnimalTypeId = animalTypeId;
        existing.Name = name;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSizeCategoryAsync(AppDbContext dbContext, string code, string name, Guid? animalTypeId, decimal? minWeightKg, decimal? maxWeightKg, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Set<SizeCategory>().SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (existing is null)
        {
            dbContext.Set<SizeCategory>().Add(new SizeCategory
            {
                Id = Guid.NewGuid(),
                AnimalTypeId = animalTypeId,
                Code = code,
                Name = name,
                MinWeightKg = minWeightKg,
                MaxWeightKg = maxWeightKg
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (existing.AnimalTypeId == animalTypeId
            && string.Equals(existing.Name, name, StringComparison.Ordinal)
            && existing.MinWeightKg == minWeightKg
            && existing.MaxWeightKg == maxWeightKg)
        {
            return;
        }

        existing.AnimalTypeId = animalTypeId;
        existing.Name = name;
        existing.MinWeightKg = minWeightKg;
        existing.MaxWeightKg = maxWeightKg;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureEntityAsync<TEntity>(AppDbContext dbContext, Func<TEntity, string> codeSelector, TEntity entity, CancellationToken cancellationToken)
        where TEntity : class
    {
        var code = codeSelector(entity);
        var existing = await dbContext.Set<TEntity>().ToListAsync(cancellationToken);
        if (existing.Any(x => string.Equals(codeSelector(x), code, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        dbContext.Set<TEntity>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record BreedGroupSeed(Guid AnimalTypeId, string Code, string Name);
    private sealed record BreedSeed(Guid AnimalTypeId, Guid? BreedGroupId, string Code, string Name);
    private sealed record BreedAllowedCoatTypeSeed(string BreedCode, string CoatTypeCode);
    private sealed record BreedAllowedSizeCategorySeed(string BreedCode, string SizeCategoryCode);
}

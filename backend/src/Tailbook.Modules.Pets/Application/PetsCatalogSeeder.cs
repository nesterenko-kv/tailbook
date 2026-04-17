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
        await EnsureSizeCategoriesAsync(dbContext, cancellationToken);
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
            // Dogs: mixed / designer / common salon breeds
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

            // Cats
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
        var cat = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "CAT", cancellationToken);

        await EnsureEntityAsync(dbContext, x => x.Code, new CoatType { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "DOUBLE_COAT", Name = "Double Coat" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new CoatType { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "CURLY_COAT", Name = "Curly Coat" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new CoatType { Id = Guid.NewGuid(), AnimalTypeId = cat.Id, Code = "SHORT_COAT", Name = "Short Coat" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new CoatType { Id = Guid.NewGuid(), AnimalTypeId = null, Code = "LONG_COAT", Name = "Long Coat" }, cancellationToken);
    }

    private static async Task EnsureSizeCategoriesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var dog = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "DOG", cancellationToken);
        var cat = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "CAT", cancellationToken);

        await EnsureEntityAsync(dbContext, x => x.Code, new SizeCategory { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "SMALL", Name = "Small", MinWeightKg = 0m, MaxWeightKg = 10m }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new SizeCategory { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "MEDIUM", Name = "Medium", MinWeightKg = 10.01m, MaxWeightKg = 25m }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new SizeCategory { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "LARGE", Name = "Large", MinWeightKg = 25.01m, MaxWeightKg = 100m }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new SizeCategory { Id = Guid.NewGuid(), AnimalTypeId = cat.Id, Code = "CAT_STANDARD", Name = "Cat Standard", MinWeightKg = 0m, MaxWeightKg = 20m }, cancellationToken);
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
}

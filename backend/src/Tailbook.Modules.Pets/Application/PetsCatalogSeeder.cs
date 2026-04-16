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

        await EnsureEntityAsync(dbContext, x => x.Code, new BreedGroup { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "DOG_SPITZ", Name = "Spitz" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new BreedGroup { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, Code = "DOG_POODLE", Name = "Poodle" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new BreedGroup { Id = Guid.NewGuid(), AnimalTypeId = cat.Id, Code = "CAT_SHORT_HAIR", Name = "Short Hair Cats" }, cancellationToken);
    }

    private static async Task EnsureBreedsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var dog = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "DOG", cancellationToken);
        var cat = await dbContext.Set<AnimalType>().SingleAsync(x => x.Code == "CAT", cancellationToken);
        var dogSpitz = await dbContext.Set<BreedGroup>().SingleAsync(x => x.Code == "DOG_SPITZ", cancellationToken);
        var dogPoodle = await dbContext.Set<BreedGroup>().SingleAsync(x => x.Code == "DOG_POODLE", cancellationToken);
        var catShortHair = await dbContext.Set<BreedGroup>().SingleAsync(x => x.Code == "CAT_SHORT_HAIR", cancellationToken);

        await EnsureEntityAsync(dbContext, x => x.Code, new Breed { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, BreedGroupId = dogSpitz.Id, Code = "SAMOYED", Name = "Samoyed" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new Breed { Id = Guid.NewGuid(), AnimalTypeId = dog.Id, BreedGroupId = dogPoodle.Id, Code = "POODLE_MINIATURE", Name = "Miniature Poodle" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new Breed { Id = Guid.NewGuid(), AnimalTypeId = cat.Id, BreedGroupId = catShortHair.Id, Code = "BRITISH_SHORTHAIR", Name = "British Shorthair" }, cancellationToken);
        await EnsureEntityAsync(dbContext, x => x.Code, new Breed { Id = Guid.NewGuid(), AnimalTypeId = cat.Id, BreedGroupId = null, Code = "SIBERIAN_CAT", Name = "Siberian Cat" }, cancellationToken);
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
}

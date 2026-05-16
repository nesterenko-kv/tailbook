using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Pets.Application.Pets.Queries;
using Tailbook.Modules.Pets.Domain.Entities;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class PetCatalogCacheTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Pet_catalog_uses_distributed_cache_after_first_load()
    {
        using var firstScope = factory.Services.CreateScope();
        var firstReadService = firstScope.ServiceProvider.GetRequiredService<IPetsReadService>();
        var firstCatalog = await firstReadService.GetCatalogAsync(CancellationToken.None);
        var cachedBreed = firstCatalog.Breeds.Single(x => x.Code == "SAMOYED");

        using (var mutationScope = factory.Services.CreateScope())
        {
            var dbContext = mutationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var breed = await dbContext.Set<Breed>().SingleAsync(x => x.Id == cachedBreed.Id);
            breed.Name = "Changed after cache fill";
            await dbContext.SaveChangesAsync();
        }

        using var secondScope = factory.Services.CreateScope();
        var secondReadService = secondScope.ServiceProvider.GetRequiredService<IPetsReadService>();
        var secondCatalog = await secondReadService.GetCatalogAsync(CancellationToken.None);

        Assert.Equal(cachedBreed.Name, secondCatalog.Breeds.Single(x => x.Id == cachedBreed.Id).Name);
    }
}

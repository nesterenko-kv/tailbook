using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public static class ApplicationInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (IsInMemoryProvider(dbContext))
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var seeders = scope.ServiceProvider.GetServices<IDataSeeder>()
            .OrderBy(x => x.Order)
            .ToArray();

        foreach (var seeder in seeders)
        {
            await seeder.SeedAsync(dbContext, scope.ServiceProvider, cancellationToken);
        }
    }

    private static bool IsInMemoryProvider(AppDbContext dbContext)
    {
        return dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
    }
}

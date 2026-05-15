namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public interface IDataSeeder
{
    int Order { get; }

    Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        ModelConfigurationRegistry.ApplyAll(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}

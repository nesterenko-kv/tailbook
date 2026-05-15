using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Jobs;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IReadOnlyList<Assembly> _modelConfigurationAssemblies;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ModelConfigurationAssemblies modelConfigurationAssemblies)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(modelConfigurationAssemblies);

        _modelConfigurationAssemblies = modelConfigurationAssemblies.Assemblies;
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<JobRecord> Jobs => Set<JobRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var assembly in _modelConfigurationAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        base.OnModelCreating(modelBuilder);
    }
}

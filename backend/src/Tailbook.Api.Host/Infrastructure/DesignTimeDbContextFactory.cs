using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        ModuleCatalog.ConfigureModulePersistence();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=tailbook;Username=tailbook;Password=tailbook;Include Error Detail=true");
        return new AppDbContext(optionsBuilder.Options);
    }
}

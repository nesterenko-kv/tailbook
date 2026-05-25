using Microsoft.EntityFrameworkCore;
using Tailbook.Api.Host.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Api.Tests;

public static class TestModelConfiguration
{
    public static AppDbContext CreateDbContext(DbContextOptions<AppDbContext> options) => new AppDbContext(options, ModuleCatalog.PersistenceModelAssemblies);
}

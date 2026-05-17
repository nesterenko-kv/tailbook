using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.Api.Tests.Factories;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class MigrationsValidationTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task All_migrations_apply_from_scratch()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Empty(pending);
    }

    [Fact]
    public void Model_has_no_pending_changes()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(dbContext.Database.HasPendingModelChanges());
    }
}

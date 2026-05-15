using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class IdempotencyTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Idempotency_key_on_get_is_ignored()
    {
        var targetUserId = await factory.SeedUserAsync("idem-get@test.local", "Idemp Get", "Manager123!", "manager");
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var idempotencyKey = Guid.NewGuid().ToString("N");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/iam/users/{targetUserId:D}");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task No_idempotency_key_processes_normally()
    {
        var token = await factory.LoginAsAsync("admin@test.local", "MyV3ryC00lAdminP@ss");

        using var client = factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/iam/permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task Concurrent_idempotency_key_returns_conflict()
    {
        var idempotencyKey = Guid.NewGuid().ToString("N");
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

        var acquire1 = await store.TryAcquireAsync(idempotencyKey, default);
        Assert.True(acquire1.Value.IsNew);

        var acquire2 = await store.TryAcquireAsync(idempotencyKey, default);
        Assert.False(acquire2.Value.IsNew);
        Assert.False(acquire2.Value.IsCompleted);
    }

    [Fact]
    public async Task Completed_idempotency_key_returns_cached_data()
    {
        var idempotencyKey = Guid.NewGuid().ToString("N");
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

        var acquire = await store.TryAcquireAsync(idempotencyKey, default);
        Assert.True(acquire.Value.IsNew);

        await store.CompleteAsync(idempotencyKey, 200, """{"status":"ok"}""", default);

        var reacquire = await store.TryAcquireAsync(idempotencyKey, default);
        Assert.False(reacquire.Value.IsNew);
        Assert.True(reacquire.Value.IsCompleted);
        Assert.Equal(200, reacquire.Value.ExistingStatusCode);
        Assert.Contains("ok", reacquire.Value.ExistingResponseBody);
    }

    [Fact]
    public async Task Expired_idempotency_entry_can_be_reacquired()
    {
        var idempotencyKey = Guid.NewGuid().ToString("N");
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BuildingBlocks.Infrastructure.Persistence.AppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        dbContext.Set<IdempotentRequest>().Add(new IdempotentRequest
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            Status = IdempotentRequestStatuses.Completed,
            ResponseStatusCode = 200,
            CreatedAt = timeProvider.GetUtcNow().AddDays(-2),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(-1)
        });
        await dbContext.SaveChangesAsync();

        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        await store.CleanupExpiredAsync(default);

        var expiredExists = await dbContext.Set<IdempotentRequest>()
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey);
        Assert.False(expiredExists);
    }
}

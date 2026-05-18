using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public sealed class PoisonOutboxMonitorBackgroundService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PoisonOutboxMonitorBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorPoisonedMessagesAsync(stoppingToken);
                await MonitorStuckMessagesAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task MonitorPoisonedMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var poisonedCount = await dbContext.Set<OutboxMessage>()
            .Where(x => x.IsPoisoned)
            .CountAsync(cancellationToken);

        if (poisonedCount > 0)
        {
            logger.PoisonOutboxMessagesFound(poisonedCount);
        }
    }

    private async Task MonitorStuckMessagesAsync(CancellationToken cancellationToken)
    {
        var stuck = await GetStuckMessagesCoreAsync(cancellationToken);

        if (stuck.Count > 0)
        {
            logger.IntegrationOutboxStuckMessagesFound(stuck.Count);
        }
    }

    public async Task<List<OutboxMessage>> GetStuckMessagesAsync(CancellationToken cancellationToken)
    {
        return await GetStuckMessagesCoreAsync(cancellationToken);
    }

    private async Task<List<OutboxMessage>> GetStuckMessagesCoreAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var utcNow = timeProvider.GetUtcNow();

        return await dbContext.Set<OutboxMessage>()
            .Where(x => x.ProcessedAt == null)
            .Where(x => !x.IsPoisoned)
            .Where(x => x.LastError != null)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= utcNow)
            .ToListAsync(cancellationToken);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Infrastructure.Telemetry;

namespace Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

internal sealed class AuditBatchWriterHostedService(
    IAuditWriteQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<AuditWriteOptions> optionsAccessor,
    TimeProvider timeProvider,
    ILogger<AuditBatchWriterHostedService> logger) : BackgroundService
{
    private readonly int _batchSize = optionsAccessor.Value.BatchSize;
    private readonly TimeSpan _flushInterval = optionsAccessor.Value.FlushInterval;
    private readonly int _flushIntervalMilliseconds = optionsAccessor.Value.FlushIntervalMilliseconds;
    private readonly int _maxWriteRetries = optionsAccessor.Value.MaxWriteRetries;
    private readonly TimeSpan _retryDelay = optionsAccessor.Value.RetryDelay;
    private CancellationToken _shutdownCancellationToken;

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCancellationToken = cancellationToken;
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditWriteItem>(_batchSize);
        logger.AuditBatchWriterStarted(_batchSize, _flushIntervalMilliseconds, _maxWriteRetries);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (batch.Count == 0)
                {
                    if (!await WaitForNextItemAsync(batch, stoppingToken))
                    {
                        break;
                    }
                }

                await FillBatchUntilFlushAsync(batch, stoppingToken);
                await PersistBatchWithRetryAsync(batch, stoppingToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            var drainToken = _shutdownCancellationToken.CanBeCanceled
                ? _shutdownCancellationToken
                : CancellationToken.None;

            logger.AuditBatchWriterDraining();
            await DrainQueueAsync(batch, drainToken);
        }
    }

    private async ValueTask<bool> WaitForNextItemAsync(List<AuditWriteItem> batch, CancellationToken cancellationToken)
    {
        while (await queue.Reader.WaitToReadAsync(cancellationToken))
        {
            if (queue.Reader.TryRead(out var item))
            {
                batch.Add(item);
                AuditTelemetry.RecordQueueDequeued(AuditWriteItemTypes.GetTelemetryItemType(item));
                return true;
            }
        }

        return false;
    }

    private async Task FillBatchUntilFlushAsync(List<AuditWriteItem> batch, CancellationToken cancellationToken)
    {
        using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var flushDelay = Task.Delay(_flushInterval, timeProvider, flushCts.Token);

        while (batch.Count < _batchSize)
        {
            while (batch.Count < _batchSize && queue.Reader.TryRead(out var item))
            {
                batch.Add(item);
                AuditTelemetry.RecordQueueDequeued(AuditWriteItemTypes.GetTelemetryItemType(item));
            }

            if (batch.Count >= _batchSize)
            {
                break;
            }

            var waitToRead = queue.Reader.WaitToReadAsync(flushCts.Token).AsTask();
            var completed = await Task.WhenAny(waitToRead, flushDelay);
            if (completed == flushDelay)
            {
                break;
            }

            if (!await waitToRead)
            {
                break;
            }
        }

        await flushCts.CancelAsync();
    }

    private async Task DrainQueueAsync(List<AuditWriteItem> batch, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (batch.Count < _batchSize && queue.Reader.TryRead(out var item))
                {
                    batch.Add(item);
                    AuditTelemetry.RecordQueueDequeued(AuditWriteItemTypes.GetTelemetryItemType(item));
                }

                if (batch.Count == 0)
                {
                    break;
                }

                await PersistBatchWithRetryAsync(batch, cancellationToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async ValueTask PersistBatchWithRetryAsync(List<AuditWriteItem> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        var maxAttempts = _maxWriteRetries + 1;
        var accessAuditCount = batch.OfType<AccessAuditWriteItem>().Count();
        var auditTrailCount = batch.OfType<AuditTrailWriteItem>().Count();
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var activity = AuditTelemetry.StartBatchWriteActivity(batch.Count, accessAuditCount, auditTrailCount);
            var stopwatch = ValueStopwatch.StartNew();
            try
            {
                await PersistBatchOnceAsync(batch, cancellationToken);
                var duration = stopwatch.GetElapsedTime();
                AuditTelemetry.RecordBatchWrite(activity, batch.Count, accessAuditCount, auditTrailCount, duration, AuditTelemetry.ResultSuccess);
                logger.AuditBatchPersisted(batch.Count, accessAuditCount, auditTrailCount, Math.Round(duration.TotalMilliseconds, 2));
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                AuditTelemetry.RecordBatchWrite(activity, batch.Count, accessAuditCount, auditTrailCount, stopwatch.GetElapsedTime(), AuditTelemetry.ResultCanceled);
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                AuditTelemetry.RecordBatchException(activity, ex);
                AuditTelemetry.RecordBatchWrite(activity, batch.Count, accessAuditCount, auditTrailCount, stopwatch.GetElapsedTime(), AuditTelemetry.ResultError);
                AuditTelemetry.RecordBatchRetry(attempt);
                logger.AuditBatchWriteAttemptFailed(ex, attempt, maxAttempts, batch.Count);
                await Task.Delay(_retryDelay, timeProvider, cancellationToken);
            }
            catch (Exception ex)
            {
                AuditTelemetry.RecordBatchException(activity, ex);
                AuditTelemetry.RecordBatchWrite(activity, batch.Count, accessAuditCount, auditTrailCount, stopwatch.GetElapsedTime(), AuditTelemetry.ResultDropped);
                logger.AuditBatchWriteDropped(ex, maxAttempts, batch.Count);
                return;
            }
        }
    }

    private async Task PersistBatchOnceAsync(IReadOnlyCollection<AuditWriteItem> batch, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var accessEntries = new List<AccessAuditEntry>();
        var auditEntries = new List<AuditEntry>();

        foreach (var item in batch)
        {
            switch (item)
            {
                case AccessAuditWriteItem access:
                    accessEntries.Add(new AccessAuditEntry
                    {
                        Id = access.Id,
                        ActorUserId = access.ActorUserId,
                        ResourceType = access.ResourceType,
                        ResourceId = access.ResourceId,
                        ActionCode = access.ActionCode,
                        HappenedAt = access.HappenedAt
                    });
                    break;
                case AuditTrailWriteItem audit:
                    auditEntries.Add(new AuditEntry
                    {
                        Id = audit.Id,
                        ActorUserId = audit.ActorUserId,
                        ModuleCode = audit.ModuleCode,
                        EntityType = audit.EntityType,
                        EntityId = audit.EntityId,
                        ActionCode = audit.ActionCode,
                        HappenedAt = audit.HappenedAt,
                        BeforeJson = audit.BeforeJson,
                        AfterJson = audit.AfterJson
                    });
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported audit write item type '{item.GetType().FullName}'.");
            }
        }

        if (accessEntries.Count > 0)
        {
            dbContext.Set<AccessAuditEntry>().AddRange(accessEntries);
        }

        if (auditEntries.Count > 0)
        {
            dbContext.Set<AuditEntry>().AddRange(auditEntries);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

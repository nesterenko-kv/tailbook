using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Tailbook.Api.Host.Infrastructure;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Infrastructure.Services;
using Tailbook.Modules.Audit.Infrastructure.Telemetry;
using Tailbook.Modules.Audit.Infrastructure.WriteBuffering;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AuditWriteBufferingTests
{
    [Fact]
    public async Task AccessAuditService_enqueues_without_db_context_write()
    {
        var queue = CreateQueue();
        var happenedAt = new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero);
        var actorUserId = Guid.NewGuid();
        var service = new AccessAuditService(queue, new FakeTimeProvider(happenedAt));

        await service.RecordAsync("iam_user", "user-1", "READ_DETAIL", actorUserId, CancellationToken.None);

        Assert.True(queue.Reader.TryRead(out var queued));
        var item = Assert.IsType<AccessAuditWriteItem>(queued);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(actorUserId, item.ActorUserId);
        Assert.Equal("iam_user", item.ResourceType);
        Assert.Equal("user-1", item.ResourceId);
        Assert.Equal("READ_DETAIL", item.ActionCode);
        Assert.Equal(happenedAt, item.HappenedAt);
    }

    [Fact]
    public async Task AuditTrailService_enqueues_without_db_context_write()
    {
        var queue = CreateQueue();
        var happenedAt = new DateTimeOffset(2026, 5, 7, 11, 0, 0, TimeSpan.Zero);
        var actorUserId = Guid.NewGuid();
        var service = new AuditTrailService(queue, new FakeTimeProvider(happenedAt));

        await service.RecordAsync("identity", "iam_user", "user-2", "ASSIGN_ROLES", actorUserId, """{"roles":[]}""", """{"roles":["manager"]}""", CancellationToken.None);

        Assert.True(queue.Reader.TryRead(out var queued));
        var item = Assert.IsType<AuditTrailWriteItem>(queued);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(actorUserId, item.ActorUserId);
        Assert.Equal("identity", item.ModuleCode);
        Assert.Equal("iam_user", item.EntityType);
        Assert.Equal("user-2", item.EntityId);
        Assert.Equal("ASSIGN_ROLES", item.ActionCode);
        Assert.Equal(happenedAt, item.HappenedAt);
        Assert.Equal("""{"roles":[]}""", item.BeforeJson);
        Assert.Equal("""{"roles":["manager"]}""", item.AfterJson);
    }

    [Fact]
    public async Task Background_service_writes_access_audit_entries_in_batches()
    {
        var saveCounter = new CountingSaveChangesInterceptor();
        await using var provider = CreateServiceProvider(CreateOptions(batchSize: 2, flushIntervalMilliseconds: 30_000), saveCounter);
        var queue = provider.GetRequiredService<IAuditWriteQueue>();
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            var first = CreateAccessItem("client-1");
            var second = CreateAccessItem("client-2");

            await queue.EnqueueAsync(first, CancellationToken.None);
            await queue.EnqueueAsync(second, CancellationToken.None);

            await saveCounter.WaitForSaveAsync();
            var entries = await ListAsync<AccessAuditEntry>(provider);

            Assert.Contains(entries, x => x.Id == first.Id && x.ResourceId == "client-1");
            Assert.Contains(entries, x => x.Id == second.Id && x.ResourceId == "client-2");
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Background_service_writes_audit_trail_entries_in_batches()
    {
        var saveCounter = new CountingSaveChangesInterceptor();
        await using var provider = CreateServiceProvider(CreateOptions(batchSize: 2, flushIntervalMilliseconds: 30_000), saveCounter);
        var queue = provider.GetRequiredService<IAuditWriteQueue>();
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            var first = CreateTrailItem("appointment-1", "CREATE");
            var second = CreateTrailItem("appointment-2", "CANCEL");

            await queue.EnqueueAsync(first, CancellationToken.None);
            await queue.EnqueueAsync(second, CancellationToken.None);

            await saveCounter.WaitForSaveAsync();
            var entries = await ListAsync<AuditEntry>(provider);

            Assert.Contains(entries, x => x.Id == first.Id && x.EntityId == "appointment-1" && x.ActionCode == "CREATE");
            Assert.Contains(entries, x => x.Id == second.Id && x.EntityId == "appointment-2" && x.ActionCode == "CANCEL");
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Mixed_audit_items_are_persisted_in_one_batch_save()
    {
        var saveCounter = new CountingSaveChangesInterceptor();
        await using var provider = CreateServiceProvider(CreateOptions(batchSize: 10, flushIntervalMilliseconds: 20), saveCounter);
        var queue = provider.GetRequiredService<IAuditWriteQueue>();
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(CreateAccessItem("visit-1"), CancellationToken.None);
            await queue.EnqueueAsync(CreateTrailItem("visit-1", "CLOSE"), CancellationToken.None);

            await saveCounter.WaitForSaveAsync();

            Assert.Equal(1, saveCounter.SaveChangesCount);
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Flush_happens_when_batch_size_is_reached()
    {
        var saveCounter = new CountingSaveChangesInterceptor();
        await using var provider = CreateServiceProvider(CreateOptions(batchSize: 2, flushIntervalMilliseconds: 30_000), saveCounter);
        var queue = provider.GetRequiredService<IAuditWriteQueue>();
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(CreateAccessItem("pet-1"), CancellationToken.None);
            await queue.EnqueueAsync(CreateAccessItem("pet-2"), CancellationToken.None);

            await saveCounter.WaitForSaveAsync();

            Assert.Equal(1, saveCounter.SaveChangesCount);
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Flush_happens_when_interval_expires()
    {
        const int flushIntervalMilliseconds = 20;
        var backgroundWaitTimeout = TimeSpan.FromSeconds(3);
        var flushInterval = TimeSpan.FromMilliseconds(flushIntervalMilliseconds);
        var timeProvider = new FakeTimeProvider();
        var options = CreateOptions(batchSize: 100, flushIntervalMilliseconds: flushIntervalMilliseconds);
        var queue = new ObservedAuditWriteQueue(options);
        var saveCounter = new CountingSaveChangesInterceptor();
        await using var provider = CreateServiceProvider(options, saveCounter, timeProvider: timeProvider, auditWriteQueue: queue);
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(CreateAccessItem("notification-job-1"), CancellationToken.None);
            await queue.WaitForFlushWaitAsync(backgroundWaitTimeout);
            timeProvider.Advance(flushInterval);

            await saveCounter.WaitForSaveAsync(backgroundWaitTimeout);

            Assert.Equal(1, saveCounter.SaveChangesCount);
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Cancellation_during_enqueue_is_observed()
    {
        var queue = CreateQueue(queueCapacity: 1);

        await queue.EnqueueAsync(CreateAccessItem("first"), CancellationToken.None);
        using var cts = new CancellationTokenSource();
        var blockedEnqueue = queue.EnqueueAsync(CreateAccessItem("second"), cts.Token).AsTask();

        await Task.Delay(20, CancellationToken.None);
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => blockedEnqueue);
        Assert.True(queue.Reader.TryRead(out var queued));
        var item = Assert.IsType<AccessAuditWriteItem>(queued);
        Assert.Equal("first", item.ResourceId);
        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Queue_records_enqueue_metrics()
    {
        var enqueuedCount = 0L;
        var enqueueDurationCount = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == AuditTelemetry.MeterName
                && (instrument.Name == "tailbook.audit.queue.enqueued"
                    || instrument.Name == "tailbook.audit.queue.enqueue.duration"))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "tailbook.audit.queue.enqueued"
                && HasTag(tags, "tailbook.audit.item_type", AuditTelemetry.ItemTypeAccessAudit)
                && HasTag(tags, "tailbook.audit.result", AuditTelemetry.ResultAccepted))
            {
                enqueuedCount += measurement;
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "tailbook.audit.queue.enqueue.duration"
                && HasTag(tags, "tailbook.audit.item_type", AuditTelemetry.ItemTypeAccessAudit)
                && HasTag(tags, "tailbook.audit.result", AuditTelemetry.ResultAccepted))
            {
                enqueueDurationCount++;
            }
        });
        listener.Start();
        var queue = CreateQueue();

        await queue.EnqueueAsync(CreateAccessItem("metrics-1"), CancellationToken.None);

        Assert.True(enqueuedCount >= 1);
        Assert.True(enqueueDurationCount >= 1);
    }

    [Fact]
    public async Task Background_service_records_batch_activity_metrics_and_success_log()
    {
        var loggerProvider = new RecordingLoggerProvider();
        var batchWriteCount = 0L;
        var batchItemCount = 0L;
        var batchDurationCount = 0;
        var dequeuedCount = 0L;
        var stoppedActivities = new ConcurrentQueue<Activity>();
        using var activityListener = new ActivityListener();
        activityListener.ShouldListenTo = source => source.Name == AuditTelemetry.ActivitySourceName;
        activityListener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        activityListener.ActivityStopped = stoppedActivities.Enqueue;
        ActivitySource.AddActivityListener(activityListener);
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AuditTelemetry.MeterName
                && (instrument.Name == "tailbook.audit.batch.writes"
                    || instrument.Name == "tailbook.audit.batch.items"
                    || instrument.Name == "tailbook.audit.batch.duration"
                    || instrument.Name == "tailbook.audit.queue.dequeued"))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "tailbook.audit.queue.dequeued"
                && (HasTag(tags, "tailbook.audit.item_type", AuditTelemetry.ItemTypeAccessAudit)
                    || HasTag(tags, "tailbook.audit.item_type", AuditTelemetry.ItemTypeAuditTrail)))
            {
                dequeuedCount += measurement;
            }

            if (!HasTag(tags, "tailbook.audit.item_type", AuditTelemetry.ItemTypeMixed)
                || !HasTag(tags, "tailbook.audit.result", AuditTelemetry.ResultSuccess))
            {
                return;
            }

            if (instrument.Name == "tailbook.audit.batch.writes")
            {
                batchWriteCount += measurement;
            }

            if (instrument.Name == "tailbook.audit.batch.items")
            {
                batchItemCount += measurement;
            }
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "tailbook.audit.batch.duration"
                && HasTag(tags, "tailbook.audit.item_type", AuditTelemetry.ItemTypeMixed)
                && HasTag(tags, "tailbook.audit.result", AuditTelemetry.ResultSuccess))
            {
                batchDurationCount++;
            }
        });
        meterListener.Start();
        var saveCounter = new CountingSaveChangesInterceptor();
        await using var provider = CreateServiceProvider(CreateOptions(batchSize: 2, flushIntervalMilliseconds: 30_000), saveCounter, loggerProvider);
        var queue = provider.GetRequiredService<IAuditWriteQueue>();
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(CreateAccessItem("telemetry-1"), CancellationToken.None);
            await queue.EnqueueAsync(CreateTrailItem("telemetry-1", "UPDATE"), CancellationToken.None);

            await saveCounter.WaitForSaveAsync();
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }

        Assert.True(batchWriteCount >= 1);
        Assert.True(batchItemCount >= 2);
        Assert.True(batchDurationCount >= 1);
        Assert.True(dequeuedCount >= 2);
        Assert.Contains(stoppedActivities, activity =>
            activity.OperationName == AuditTelemetry.BatchWriteActivityName
            && Equals(activity.GetTagItem("tailbook.audit.result"), AuditTelemetry.ResultSuccess)
            && Equals(activity.GetTagItem("tailbook.audit.item_type"), AuditTelemetry.ItemTypeMixed)
            && Equals(activity.GetTagItem("tailbook.audit.batch.item_count"), 2));
        Assert.Contains(loggerProvider.Entries, x => x.Level == LogLevel.Information && x.Message.Contains("Audit batch writer started", StringComparison.Ordinal));
        Assert.Contains(loggerProvider.Entries, x => x.Level == LogLevel.Debug && x.Message.Contains("Audit batch persisted 2 item", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Failed_batch_write_is_logged_and_retried_within_limit()
    {
        var failingInterceptor = new FailingSaveChangesInterceptor(failuresBeforeSuccess: 1);
        var loggerProvider = new RecordingLoggerProvider();
        await using var provider = CreateServiceProvider(
            CreateOptions(batchSize: 1, flushIntervalMilliseconds: 30_000, maxWriteRetries: 2, retryDelayMilliseconds: 1),
            failingInterceptor,
            loggerProvider);
        var queue = provider.GetRequiredService<IAuditWriteQueue>();
        var writer = provider.GetRequiredService<AuditBatchWriterHostedService>();

        await writer.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(CreateTrailItem("retry-1", "UPDATE"), CancellationToken.None);

            await failingInterceptor.WaitForSuccessfulSaveAsync();

            Assert.True(failingInterceptor.SaveAttempts >= 2);
            Assert.Contains(loggerProvider.Entries, x => x.Level == LogLevel.Warning && x.Message.Contains("Audit batch write attempt", StringComparison.Ordinal));
            Assert.DoesNotContain(loggerProvider.Entries, x => x.Level == LogLevel.Error);
        }
        finally
        {
            await writer.StopAsync(CancellationToken.None);
        }
    }

    private static AuditWriteQueue CreateQueue(int queueCapacity = 100)
    {
        return new AuditWriteQueue(Options.Create(CreateOptions(queueCapacity: queueCapacity)));
    }

    private static AuditWriteOptions CreateOptions(
        int queueCapacity = 100,
        int batchSize = 10,
        int flushIntervalMilliseconds = 50,
        int maxWriteRetries = 1,
        int retryDelayMilliseconds = 1)
    {
        return new AuditWriteOptions
        {
            QueueCapacity = queueCapacity,
            BatchSize = batchSize,
            FlushIntervalMilliseconds = flushIntervalMilliseconds,
            MaxWriteRetries = maxWriteRetries,
            RetryDelayMilliseconds = retryDelayMilliseconds
        };
    }

    private sealed class ObservedAuditWriteQueue : IAuditWriteQueue
    {
        private readonly AuditWriteQueue _inner;
        private readonly ObservedChannelReader _reader;

        public ObservedAuditWriteQueue(AuditWriteOptions options)
        {
            _inner = new AuditWriteQueue(Options.Create(options));
            _reader = new ObservedChannelReader(_inner.Reader);
        }

        public ChannelReader<AuditWriteItem> Reader => _reader;

        public ValueTask EnqueueAsync(AuditWriteItem item, CancellationToken cancellationToken)
        {
            return _inner.EnqueueAsync(item, cancellationToken);
        }

        public Task WaitForFlushWaitAsync(TimeSpan timeout)
        {
            return _reader.WaitForFlushWaitAsync(timeout);
        }
    }

    private sealed class ObservedChannelReader(ChannelReader<AuditWriteItem> inner) : ChannelReader<AuditWriteItem>
    {
        private int _readCount;
        private readonly TaskCompletionSource _flushWaitObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override bool TryRead([MaybeNullWhen(false)] out AuditWriteItem item)
        {
            var result = inner.TryRead(out item);
            if (result)
            {
                Interlocked.Increment(ref _readCount);
            }

            return result;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            var waitToRead = inner.WaitToReadAsync(cancellationToken);
            if (Volatile.Read(ref _readCount) > 0)
            {
                _flushWaitObserved.TrySetResult();
            }

            return waitToRead;
        }

        public Task WaitForFlushWaitAsync(TimeSpan timeout)
        {
            return _flushWaitObserved.Task.WaitAsync(timeout);
        }
    }

    private static ServiceProvider CreateServiceProvider(
        AuditWriteOptions options,
        SaveChangesInterceptor? saveChangesInterceptor = null,
        ILoggerProvider? loggerProvider = null,
        TimeProvider? timeProvider = null,
        IAuditWriteQueue? auditWriteQueue = null)
    {
        var services = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"audit-buffering-{Guid.NewGuid():N}";

        services.AddSingleton(Options.Create(options));
        services.AddSingleton(timeProvider ?? TimeProvider.System);
        if (auditWriteQueue is null)
        {
            services.AddSingleton<IAuditWriteQueue, AuditWriteQueue>();
        }
        else
        {
            services.AddSingleton(auditWriteQueue);
        }

        services.AddSingleton<AuditBatchWriterHostedService>();
        services.AddSingleton(ModuleCatalog.PersistenceModelAssemblies);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            if (loggerProvider is not null)
            {
                builder.AddProvider(loggerProvider);
            }
        });
        services.AddDbContext<AppDbContext>(dbOptions =>
        {
            dbOptions.UseInMemoryDatabase(databaseName, databaseRoot);
            if (saveChangesInterceptor is not null)
            {
                dbOptions.AddInterceptors(saveChangesInterceptor);
            }
        });

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static AccessAuditWriteItem CreateAccessItem(string resourceId)
    {
        return new AccessAuditWriteItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test_resource",
            resourceId,
            "READ_DETAIL",
            new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero));
    }

    private static AuditTrailWriteItem CreateTrailItem(string entityId, string actionCode)
    {
        return new AuditTrailWriteItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "test_module",
            "test_entity",
            entityId,
            actionCode,
            new DateTimeOffset(2026, 5, 7, 12, 5, 0, TimeSpan.Zero),
            """{"before":true}""",
            """{"after":true}""");
    }

    private static async Task<List<TEntity>> ListAsync<TEntity>(IServiceProvider provider)
        where TEntity : class
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Set<TEntity>().ToListAsync();
    }

    private sealed class CountingSaveChangesInterceptor : SaveChangesInterceptor
    {
        private int _saveChangesCount;
        private readonly TaskCompletionSource _saveObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SaveChangesCount => _saveChangesCount;

        public Task WaitForSaveAsync(TimeSpan? timeout = null)
        {
            return _saveObserved.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(3));
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _saveChangesCount);
            _saveObserved.TrySetResult();
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    private sealed class FailingSaveChangesInterceptor(int failuresBeforeSuccess) : SaveChangesInterceptor
    {
        private int _remainingFailures = failuresBeforeSuccess;
        private int _saveAttempts;
        private readonly TaskCompletionSource _successfulSaveObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SaveAttempts => _saveAttempts;

        public Task WaitForSuccessfulSaveAsync()
        {
            return _successfulSaveObserved.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _saveAttempts);
            if (Interlocked.Decrement(ref _remainingFailures) >= 0)
            {
                throw new InvalidOperationException("Transient audit write failure.");
            }

            _successfulSaveObserved.TrySetResult();
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<RecordedLogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new RecordingLogger(Entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed record RecordedLogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class RecordingLogger(ConcurrentQueue<RecordedLogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(new RecordedLogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private static bool HasTag<T>(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key, T value)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key && Equals(tag.Value, value))
            {
                return true;
            }
        }

        return false;
    }
}

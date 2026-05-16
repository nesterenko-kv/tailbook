using System.Text.Json;
using FastEndpoints;
using StackExchange.Redis;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Jobs;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class RedisJobProvider(ConnectionMultiplexer redis, TimeProvider timeProvider) : IJobStorageProvider<JobRecord>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IDatabase Db => redis.GetDatabase();

    public async Task StoreJobAsync(JobRecord job, CancellationToken ct)
    {
        const string operation = "store";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultSuccess;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation, job.QueueID);

        try
        {
            var dataKey = $"jobs:data:{job.TrackingID:N}";
            var queueKey = $"jobs:queue:{job.QueueID}";

            var entries = new HashEntry[]
            {
                new("Id", job.Id.ToString("N")),
                new("QueueID", job.QueueID),
                new("TrackingID", job.TrackingID.ToString("N")),
                new("ExecuteAfter", job.ExecuteAfter.Ticks.ToString()),
                new("ExpireOn", job.ExpireOn.Ticks.ToString()),
                new("DequeueAfter", job.DequeueAfter.Ticks.ToString()),
                new("IsComplete", job.IsComplete ? "1" : "0"),
                new("CommandJson", job.CommandJson ?? ""),
                new("ResultJson", job.ResultJson ?? "")
            };

            var db = Db;
            await db.HashSetAsync(dataKey, entries);
            await db.KeyExpireAsync(dataKey, job.ExpireOn - DateTime.UtcNow + TimeSpan.FromDays(1));

            var score = new DateTimeOffset(job.ExecuteAfter.Ticks, TimeSpan.Zero).ToUnixTimeSeconds();
            await db.SortedSetAddAsync(queueKey, job.TrackingID.ToString("N"), score);
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, job.QueueID, 1, stopwatch.GetElapsedTime(), result);
        }
    }

    public async Task<ICollection<JobRecord>> GetNextBatchAsync(PendingJobSearchParams<JobRecord> p)
    {
        const string operation = "dequeue";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultSuccess;
        var itemCount = 0;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation, p.QueueID);

        try
        {
            var db = Db;
            var queueKey = $"jobs:queue:{p.QueueID}";
            var now = timeProvider.GetUtcNow();
            var nowUnix = now.ToUnixTimeSeconds();
            var leaseTime = p.ExecutionTimeLimit == Timeout.InfiniteTimeSpan
                ? TimeSpan.FromMinutes(30)
                : p.ExecutionTimeLimit + TimeSpan.FromMinutes(1);

            var script = @"
                local queueKey = KEYS[1]
                local now = tonumber(ARGV[1])
                local limit = tonumber(ARGV[2])
                local leaseTicks = tonumber(ARGV[3])

                local members = redis.call('ZRANGEBYSCORE', queueKey, '-inf', now, 'LIMIT', 0, limit)
                if #members == 0 then
                    return {}
                end

                redis.call('ZREM', queueKey, unpack(members))
                return members
            ";

            var evalResult = await db.ScriptEvaluateAsync(script, [queueKey], [nowUnix, p.Limit, leaseTime.Ticks]);
            var rawResults = (RedisResult[]?)evalResult;
            var trackingIds = rawResults?
                .Select(x => (string?)x)
                .Where(x => x is not null)
                .Select(x => x!)
                .ToArray() ?? [];

            itemCount = trackingIds.Length;
            if (itemCount == 0)
            {
                return [];
            }

            var leaseUntil = now.UtcDateTime + leaseTime;
            var jobs = new List<JobRecord>(itemCount);

            foreach (var trackingId in trackingIds)
            {
                var dataKey = $"jobs:data:{trackingId}";
                var hash = await db.HashGetAllAsync(dataKey);
                if (hash.Length == 0)
                {
                    continue;
                }

                var job = DeserializeJob(hash);
                if (job is null)
                {
                    continue;
                }

                job.DequeueAfter = leaseUntil;
                await UpdateJobFieldAsync(db, dataKey, "DequeueAfter", leaseUntil.Ticks.ToString());
                jobs.Add(job);
            }

            return jobs;
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, p.QueueID, itemCount, stopwatch.GetElapsedTime(), result);
        }
    }

    public async Task MarkJobAsCompleteAsync(JobRecord job, CancellationToken ct)
    {
        const string operation = "complete";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultSuccess;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation, job.QueueID);

        try
        {
            var dataKey = $"jobs:data:{job.TrackingID:N}";
            var db = Db;
            job.IsComplete = true;
            job.DequeueAfter = DateTime.MaxValue;
            await UpdateJobFieldAsync(db, dataKey, "IsComplete", "1");
            await UpdateJobFieldAsync(db, dataKey, "DequeueAfter", DateTime.MaxValue.Ticks.ToString());
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, job.QueueID, 1, stopwatch.GetElapsedTime(), result);
        }
    }

    public async Task CancelJobAsync(Guid trackingId, CancellationToken ct)
    {
        const string operation = "cancel";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultMiss;
        var itemCount = 0;
        string? queueId = null;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation);

        try
        {
            var db = Db;
            var trackingKey = trackingId.ToString("N");
            var dataKey = $"jobs:data:{trackingKey}";

            var queueIdRaw = await db.HashGetAsync(dataKey, "QueueID");
            if (queueIdRaw.IsNull)
            {
                return;
            }

            queueId = (string?)queueIdRaw;
            var queueKey = $"jobs:queue:{queueId}";

            await db.SortedSetRemoveAsync(queueKey, trackingKey);
            await UpdateJobFieldAsync(db, dataKey, "IsComplete", "1");
            itemCount = 1;
            result = JobQueueTelemetry.ResultSuccess;
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, queueId, itemCount, stopwatch.GetElapsedTime(), result);
        }
    }

    public async Task OnHandlerExecutionFailureAsync(JobRecord job, Exception e, CancellationToken ct)
    {
        const string operation = "reschedule_failed";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultSuccess;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation, job.QueueID);

        try
        {
            var db = Db;
            var dataKey = $"jobs:data:{job.TrackingID:N}";
            var queueKey = $"jobs:queue:{job.QueueID}";

            var retryAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(1);
            job.ExecuteAfter = retryAt;
            job.DequeueAfter = retryAt;

            await UpdateJobFieldAsync(db, dataKey, "ExecuteAfter", retryAt.Ticks.ToString());
            await UpdateJobFieldAsync(db, dataKey, "DequeueAfter", retryAt.Ticks.ToString());

            var score = new DateTimeOffset(retryAt.Ticks, TimeSpan.Zero).ToUnixTimeSeconds();
            await db.SortedSetAddAsync(queueKey, job.TrackingID.ToString("N"), score);
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, job.QueueID, 1, stopwatch.GetElapsedTime(), result);
        }
    }

    public async Task PurgeStaleJobsAsync(StaleJobSearchParams<JobRecord> p)
    {
        const string operation = "purge_stale";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultSuccess;
        var itemCount = 0;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation);

        try
        {
            var db = Db;
            var matchFunc = p.Match.Compile();
            var server = redis.GetServer(redis.GetEndPoints().First());

            var pageSize = 200;

            await foreach (var key in server.KeysAsync(pattern: "jobs:queue:*", pageSize: pageSize))
            {
                var members = await db.SortedSetRangeByScoreAsync(key);
                foreach (var member in members)
                {
                    var trackingId = (string)member!;
                    var dataKey = $"jobs:data:{trackingId}";
                    var hash = await db.HashGetAllAsync(dataKey);

                    var job = DeserializeJob(hash);
                    if (job is not null && matchFunc(job))
                    {
                        await db.KeyDeleteAsync(dataKey);
                        await db.SortedSetRemoveAsync(key, trackingId);
                        itemCount++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, null, itemCount, stopwatch.GetElapsedTime(), result);
        }
    }

    public bool DistributedJobProcessingEnabled => true;

    public async Task StoreJobResultAsync<TResult>(Guid trackingId, TResult result, CancellationToken ct)
    {
        const string operation = "store_result";
        var stopwatch = ValueStopwatch.StartNew();
        var telemetryResult = JobQueueTelemetry.ResultSuccess;
        var itemCount = 0;
        string? queueId = null;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation);

        try
        {
            var db = Db;
            var dataKey = $"jobs:data:{trackingId:N}";

            var queueIdRaw = await db.HashGetAsync(dataKey, "QueueID");
            if (!queueIdRaw.IsNull)
            {
                queueId = (string?)queueIdRaw;
            }

            var resultJson = JsonSerializer.Serialize(result, JsonOptions);
            await UpdateJobFieldAsync(db, dataKey, "ResultJson", resultJson);
            itemCount = 1;
        }
        catch (Exception ex)
        {
            telemetryResult = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, queueId, itemCount, stopwatch.GetElapsedTime(), telemetryResult);
        }
    }

    public async Task<TResult?> GetJobResultAsync<TResult>(Guid trackingId, CancellationToken ct)
    {
        const string operation = "get_result";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultMiss;
        var itemCount = 0;
        string? queueId = null;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation);

        try
        {
            var db = Db;
            var dataKey = $"jobs:data:{trackingId:N}";

            var queueIdRaw = await db.HashGetAsync(dataKey, "QueueID");
            if (!queueIdRaw.IsNull)
            {
                queueId = (string?)queueIdRaw;
            }

            var resultJson = await db.HashGetAsync(dataKey, "ResultJson");
            if (resultJson.IsNull)
            {
                return default;
            }

            itemCount = 1;
            result = JobQueueTelemetry.ResultSuccess;
            var json = (string?)resultJson;
            return json is not null ? JsonSerializer.Deserialize<TResult>(json, JsonOptions) : default;
        }
        catch (Exception ex)
        {
            result = JobQueueTelemetry.ResultError;
            JobQueueTelemetry.RecordStorageError(activity, ex);
            throw;
        }
        finally
        {
            JobQueueTelemetry.RecordStorageOperation(activity, operation, queueId, itemCount, stopwatch.GetElapsedTime(), result);
        }
    }

    private static JobRecord? DeserializeJob(HashEntry[] hash)
    {
        if (hash.Length == 0)
        {
            return null;
        }

        var dict = hash.ToDictionary(x => x.Name.ToString(), x => (string?)x.Value);
        return new JobRecord
        {
            Id = TryParseGuid(dict.GetValueOrDefault("Id")),
            QueueID = dict.GetValueOrDefault("QueueID") ?? string.Empty,
            TrackingID = TryParseGuid(dict.GetValueOrDefault("TrackingID")),
            ExecuteAfter = TryParseDateTime(dict.GetValueOrDefault("ExecuteAfter")),
            ExpireOn = TryParseDateTime(dict.GetValueOrDefault("ExpireOn")),
            DequeueAfter = TryParseDateTime(dict.GetValueOrDefault("DequeueAfter")),
            IsComplete = dict.GetValueOrDefault("IsComplete") == "1",
            CommandJson = dict.GetValueOrDefault("CommandJson") ?? string.Empty,
            ResultJson = dict.GetValueOrDefault("ResultJson")
        };
    }

    private static Guid TryParseGuid(string? value)
        => value is not null && Guid.TryParseExact(value, "N", out var id) ? id : Guid.Empty;

    private static DateTime TryParseDateTime(string? value)
        => value is not null && long.TryParse(value, out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.UnixEpoch;

    private static async Task UpdateJobFieldAsync(IDatabase db, string dataKey, string field, string value)
    {
        await db.HashSetAsync(dataKey, field, value);
    }
}

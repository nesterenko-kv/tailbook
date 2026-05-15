using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Jobs;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class JobProvider(IDbContextFactory<AppDbContext> dbContextFactory, TimeProvider timeProvider) : IJobStorageProvider<JobRecord>
{
    public async Task StoreJobAsync(JobRecord job, CancellationToken ct)
    {
        const string operation = "store";
        var stopwatch = ValueStopwatch.StartNew();
        var result = JobQueueTelemetry.ResultSuccess;
        using var activity = JobQueueTelemetry.StartStorageOperation(operation, job.QueueID);

        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            await db.AddAsync(job, ct);
            await db.SaveChangesAsync(ct);
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
            await using var db = await dbContextFactory.CreateDbContextAsync(p.CancellationToken);

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var leaseTime = p.ExecutionTimeLimit == Timeout.InfiniteTimeSpan
                ? TimeSpan.FromMinutes(30)
                : p.ExecutionTimeLimit + TimeSpan.FromMinutes(1);

            var jobs = await db.Set<JobRecord>()
                .FromSqlInterpolated(
                    $"""
                     UPDATE "Jobs"
                     SET "DequeueAfter" = {now + leaseTime}
                     WHERE "Id" IN (
                         SELECT "Id" FROM "Jobs"
                         WHERE "QueueID" = {p.QueueID}
                           AND "IsComplete" = false
                           AND "ExecuteAfter" <= {now}
                           AND "ExpireOn" >= {now}
                           AND "DequeueAfter" <= {now}
                         ORDER BY "ExecuteAfter"
                         FOR UPDATE SKIP LOCKED
                         LIMIT {p.Limit}
                     )
                     RETURNING *
                     """)
                .AsNoTracking()
                .ToListAsync(p.CancellationToken);

            itemCount = jobs.Count;

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
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            db.Update(job);
            await db.SaveChangesAsync(ct);
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
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var job = await db.Set<JobRecord>().FirstOrDefaultAsync(j => j.TrackingID == trackingId, ct);

            if (job is not null)
            {
                queueId = job.QueueID;
                itemCount = 1;
                result = JobQueueTelemetry.ResultSuccess;
                job.IsComplete = true;
                db.Update(job);
                await db.SaveChangesAsync(ct);
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
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            job.ExecuteAfter = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(1);
            job.DequeueAfter = job.ExecuteAfter;
            db.Update(job);
            await db.SaveChangesAsync(ct);
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
            await using var db = await dbContextFactory.CreateDbContextAsync(p.CancellationToken);
            var staleJobs = db.Set<JobRecord>().Where(p.Match);
            db.RemoveRange(staleJobs);
            itemCount = await db.SaveChangesAsync(p.CancellationToken);
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

    public bool DistributedJobProcessingEnabled => false;

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
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var job = await db.Set<JobRecord>().SingleAsync(j => j.TrackingID == trackingId, ct);

            queueId = job.QueueID;
            itemCount = 1;
            ((IJobResultStorage)job).SetResult(result);
            db.Update(job);
            await db.SaveChangesAsync(ct);
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
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var job = await db.Set<JobRecord>().FirstOrDefaultAsync(j => j.TrackingID == trackingId, ct);

            if (job is null)
            {
                return default;
            }

            queueId = job.QueueID;
            itemCount = 1;
            result = JobQueueTelemetry.ResultSuccess;

            return ((IJobResultStorage)job).GetResult<TResult>();
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
}

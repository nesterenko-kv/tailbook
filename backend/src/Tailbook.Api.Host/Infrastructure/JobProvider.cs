using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class JobProvider(IDbContextFactory<AppDbContext> dbContextFactory) : IJobStorageProvider<JobRecord>
{
    public async Task StoreJobAsync(JobRecord job, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        await db.AddAsync(job, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ICollection<JobRecord>> GetNextBatchAsync(PendingJobSearchParams<JobRecord> p)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(p.CancellationToken);

        var now = DateTime.UtcNow;
        var leaseTime = p.ExecutionTimeLimit == Timeout.InfiniteTimeSpan
            ? TimeSpan.FromMinutes(30)
            : p.ExecutionTimeLimit + TimeSpan.FromMinutes(1);

        return await db.Set<JobRecord>()
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
    }

    public async Task MarkJobAsCompleteAsync(JobRecord job, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        db.Update(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelJobAsync(Guid trackingId, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var job = await db.Set<JobRecord>().FirstOrDefaultAsync(j => j.TrackingID == trackingId, ct);

        if (job is not null)
        {
            job.IsComplete = true;
            db.Update(job);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task OnHandlerExecutionFailureAsync(JobRecord job, Exception e, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        job.ExecuteAfter = DateTime.UtcNow.AddMinutes(1);
        db.Update(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task PurgeStaleJobsAsync(StaleJobSearchParams<JobRecord> p)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(p.CancellationToken);
        var staleJobs = db.Set<JobRecord>().Where(p.Match);
        db.RemoveRange(staleJobs);
        await db.SaveChangesAsync(p.CancellationToken);
    }

    public bool DistributedJobProcessingEnabled => false;

    public async Task StoreJobResultAsync<TResult>(Guid trackingId, TResult result, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var job = await db.Set<JobRecord>().SingleAsync(j => j.TrackingID == trackingId, ct);

        ((IJobResultStorage)job).SetResult(result);
        db.Update(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task<TResult?> GetJobResultAsync<TResult>(Guid trackingId, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        var job = await db.Set<JobRecord>().FirstOrDefaultAsync(j => j.TrackingID == trackingId, ct);

        return job is not null
            ? ((IJobResultStorage)job).GetResult<TResult>()
            : default;
    }
}

using Microsoft.EntityFrameworkCore;
using TrustDraft.Core.Abstractions;
using TrustDraft.Core.Domain;
using TrustDraft.Infrastructure.Persistence;

namespace TrustDraft.Worker;

public class JobTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
}

public interface IJobHandler
{
    JobType Type { get; }
    Task HandleAsync(Job job, CancellationToken ct);
}

/// <summary>
/// Simple Postgres-backed job queue: claims one job at a time with FOR UPDATE SKIP LOCKED,
/// so you can run several workers safely. Swap for Hangfire later if you need scheduling/dashboards.
/// </summary>
public class JobRunner(IServiceScopeFactory scopes, ILogger<JobRunner> log) : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("Job runner started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await TryRunOneAsync(stoppingToken))
                    await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Job loop error");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }

    private async Task<bool> TryRunOneAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Job? job;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            // ToListAsync (not FirstOrDefaultAsync) so EF doesn't wrap the FOR UPDATE query in a subquery.
            job = (await db.Jobs
                .FromSql($"""
                    SELECT * FROM "Jobs"
                    WHERE "Status" = 'Queued'
                    ORDER BY "CreatedAt"
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct)).FirstOrDefault();

            if (job is null) return false;

            job.Status = JobStatus.Running;
            job.StartedAt = DateTimeOffset.UtcNow;
            job.Attempts++;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        scope.ServiceProvider.GetRequiredService<JobTenantContext>().TenantId = job.TenantId;
        var handler = scope.ServiceProvider.GetServices<IJobHandler>().FirstOrDefault(h => h.Type == job.Type);

        try
        {
            if (handler is null) throw new InvalidOperationException($"No handler for {job.Type}");
            log.LogInformation("Running {Type} for {TargetId}", job.Type, job.TargetId);
            await handler.HandleAsync(job, ct);
            job.Status = JobStatus.Succeeded;
            job.Error = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "Job {JobId} failed", job.Id);
            job.Error = ex.Message;
            job.Status = job.Attempts >= MaxAttempts ? JobStatus.Failed : JobStatus.Queued;
        }

        job.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}

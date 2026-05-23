using Closeoutflow.Modules.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Closeoutflow.Api.Persistence;

public sealed class EfJobRepository : IJobRepository
{
    private readonly AppDbContext _dbContext;

    public EfJobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.Jobs
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyCollection<Job>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.Jobs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return rows.Select(ToDomain).ToArray();
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Jobs
            .AnyAsync(x => x.Id == job.Id, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A job with id '{job.Id}' already exists.");
        }

        _dbContext.Jobs.Add(ToRow(job));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.Jobs
            .SingleOrDefaultAsync(x => x.Id == job.Id, cancellationToken);

        if (row is null)
        {
            throw new InvalidOperationException($"A job with id '{job.Id}' does not exist.");
        }

        row.Title = job.Title;
        row.Status = (int)job.Status;
        row.CreatedAtUtc = job.CreatedAtUtc;
        row.StartedAtUtc = job.StartedAtUtc;
        row.PendingCloseoutAtUtc = job.PendingCloseoutAtUtc;
        row.ClosedAtUtc = job.ClosedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static JobRow ToRow(Job job)
    {
        return new JobRow
        {
            Id = job.Id,
            Title = job.Title,
            Status = (int)job.Status,
            CreatedAtUtc = job.CreatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            PendingCloseoutAtUtc = job.PendingCloseoutAtUtc,
            ClosedAtUtc = job.ClosedAtUtc
        };
    }

    private static Job ToDomain(JobRow row)
    {
        return Job.Rehydrate(
            row.Id,
            row.Title,
            (JobStatus)row.Status,
            row.CreatedAtUtc,
            row.StartedAtUtc,
            row.PendingCloseoutAtUtc,
            row.ClosedAtUtc);
    }
}

using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Api.Repositories;

public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly List<Job> _jobs = new();

    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_jobs.SingleOrDefault(x => x.Id == jobId));
    }

    public Task<IReadOnlyCollection<Job>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Job> jobs = _jobs
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(jobs);
    }

    public Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (_jobs.Any(x => x.Id == job.Id))
        {
            throw new InvalidOperationException($"A job with id '{job.Id}' already exists.");
        }

        _jobs.Add(job);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        var existingJob = _jobs.SingleOrDefault(x => x.Id == job.Id);

        if (existingJob is null)
        {
            throw new InvalidOperationException($"A job with id '{job.Id}' does not exist.");
        }

        return Task.CompletedTask;
    }
}

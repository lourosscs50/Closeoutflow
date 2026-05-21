using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Api.Repositories;

public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly List<Job> _jobs = new();

    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_jobs.SingleOrDefault(x => x.Id == jobId));
    }

    public Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        _jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
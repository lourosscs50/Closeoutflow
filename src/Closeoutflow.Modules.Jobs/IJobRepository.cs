namespace Closeoutflow.Modules.Jobs;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Job>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    Task UpdateAsync(Job job, CancellationToken cancellationToken = default);
}

namespace Closeoutflow.Modules.Closeouts;

public interface ICloseoutRecordRepository
{
    Task AddAsync(CloseoutRecord closeoutRecord, CancellationToken cancellationToken = default);
    Task<CloseoutRecord?> GetByIdAsync(Guid closeoutRecordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CloseoutRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CloseoutRecord>> ListByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}

namespace Closeoutflow.Modules.Closeouts;

public interface ICloseoutRecordRepository
{
    Task AddAsync(CloseoutRecord closeoutRecord, CancellationToken cancellationToken = default);
    Task<CloseoutRecord?> GetByIdAsync(Guid closeoutRecordId, CancellationToken cancellationToken = default);
}
using Closeoutflow.Modules.Closeouts;

namespace Closeoutflow.Api.Repositories;

public sealed class InMemoryCloseoutRecordRepository : ICloseoutRecordRepository
{
    private readonly List<CloseoutRecord> _items = new();

    public Task AddAsync(CloseoutRecord closeoutRecord, CancellationToken cancellationToken = default)
    {
        _items.Add(closeoutRecord);
        return Task.CompletedTask;
    }

    public Task<CloseoutRecord?> GetByIdAsync(Guid closeoutRecordId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.SingleOrDefault(x => x.Id == closeoutRecordId));
    }
}
using Closeoutflow.Modules.Closeouts;

namespace Closeoutflow.Api.Repositories;

public sealed class InMemoryCloseoutRecordRepository : ICloseoutRecordRepository
{
    private readonly List<CloseoutRecord> _items = new();

    public Task AddAsync(CloseoutRecord closeoutRecord, CancellationToken cancellationToken = default)
    {
        if (_items.Any(x => x.Id == closeoutRecord.Id))
        {
            throw new InvalidOperationException($"A closeout record with id '{closeoutRecord.Id}' already exists.");
        }

        _items.Add(closeoutRecord);

        return Task.CompletedTask;
    }

    public Task<CloseoutRecord?> GetByIdAsync(Guid closeoutRecordId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.SingleOrDefault(x => x.Id == closeoutRecordId));
    }
}

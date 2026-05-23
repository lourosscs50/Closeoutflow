using Closeoutflow.Modules.Closeouts;
using Microsoft.EntityFrameworkCore;

namespace Closeoutflow.Api.Persistence;

public sealed class EfCloseoutRecordRepository : ICloseoutRecordRepository
{
    private readonly AppDbContext _dbContext;

    public EfCloseoutRecordRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CloseoutRecord closeoutRecord, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.CloseoutRecords
            .AnyAsync(x => x.Id == closeoutRecord.Id, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"A closeout record with id '{closeoutRecord.Id}' already exists.");
        }

        _dbContext.CloseoutRecords.Add(ToRow(closeoutRecord));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CloseoutRecord?> GetByIdAsync(
        Guid closeoutRecordId,
        CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.CloseoutRecords
            .AsNoTracking()
            .Include(x => x.ProofItems)
            .SingleOrDefaultAsync(x => x.Id == closeoutRecordId, cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyCollection<CloseoutRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.CloseoutRecords
            .AsNoTracking()
            .Include(x => x.ProofItems)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return rows.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<CloseoutRecord>> ListByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.CloseoutRecords
            .AsNoTracking()
            .Include(x => x.ProofItems)
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return rows.Select(ToDomain).ToArray();
    }

    private static CloseoutRecordRow ToRow(CloseoutRecord closeoutRecord)
    {
        return new CloseoutRecordRow
        {
            Id = closeoutRecord.Id,
            JobId = closeoutRecord.JobId,
            Summary = closeoutRecord.Summary,
            CreatedAtUtc = closeoutRecord.CreatedAtUtc,
            ProofItems = closeoutRecord.ProofItems
                .Select(x => new ProofItemRow
                {
                    Id = x.Id,
                    CloseoutRecordId = closeoutRecord.Id,
                    Type = (int)x.Type,
                    Value = x.Value,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList()
        };
    }

    private static CloseoutRecord ToDomain(CloseoutRecordRow row)
    {
        var proofItems = row.ProofItems
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => ProofItem.Rehydrate(
                x.Id,
                (ProofItemType)x.Type,
                x.Value,
                x.CreatedAtUtc))
            .ToArray();

        return CloseoutRecord.Rehydrate(
            row.Id,
            row.JobId,
            row.Summary,
            row.CreatedAtUtc,
            proofItems);
    }
}

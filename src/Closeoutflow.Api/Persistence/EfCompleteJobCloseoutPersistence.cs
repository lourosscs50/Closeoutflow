using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Closeoutflow.Api.Persistence;

public sealed class EfCompleteJobCloseoutPersistence
    : ICompleteJobCloseoutPersistence
{
    private readonly AppDbContext _dbContext;

    public EfCompleteJobCloseoutPersistence(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(
        Job job,
        CloseoutRecord closeoutRecord,
        CancellationToken cancellationToken = default)
    {
        if (job.Id != closeoutRecord.JobId)
        {
            throw new InvalidOperationException(
                "The closeout record must belong to the job being closed.");
        }

        var jobRow = await _dbContext.Jobs
            .SingleOrDefaultAsync(
                x => x.Id == job.Id,
                cancellationToken);

        if (jobRow is null)
        {
            throw new InvalidOperationException(
                $"A job with id '{job.Id}' does not exist.");
        }

        var closeoutExists = await _dbContext.CloseoutRecords
            .AnyAsync(
                x => x.Id == closeoutRecord.Id,
                cancellationToken);

        if (closeoutExists)
        {
            throw new InvalidOperationException(
                $"A closeout record with id '{closeoutRecord.Id}' already exists.");
        }

        jobRow.Title = job.Title;
        jobRow.Status = (int)job.Status;
        jobRow.CreatedAtUtc = job.CreatedAtUtc;
        jobRow.StartedAtUtc = job.StartedAtUtc;
        jobRow.PendingCloseoutAtUtc = job.PendingCloseoutAtUtc;
        jobRow.ClosedAtUtc = job.ClosedAtUtc;

        _dbContext.CloseoutRecords.Add(
            new CloseoutRecordRow
            {
                Id = closeoutRecord.Id,
                JobId = closeoutRecord.JobId,
                Summary = closeoutRecord.Summary,
                CreatedAtUtc = closeoutRecord.CreatedAtUtc,
                ProofItems = closeoutRecord.ProofItems
                    .Select(
                        proofItem => new ProofItemRow
                        {
                            Id = proofItem.Id,
                            CloseoutRecordId = closeoutRecord.Id,
                            Type = (int)proofItem.Type,
                            Value = proofItem.Value,
                            CreatedAtUtc = proofItem.CreatedAtUtc
                        })
                    .ToList()
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

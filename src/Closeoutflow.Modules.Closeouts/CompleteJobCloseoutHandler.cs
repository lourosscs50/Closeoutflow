using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts.Application;

public sealed class CompleteJobCloseoutHandler
{
    private readonly IJobRepository _jobRepository;
    private readonly ICompleteJobCloseoutPersistence _persistence;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompleteJobCloseoutHandler(
        IJobRepository jobRepository,
        ICompleteJobCloseoutPersistence persistence,
        IDateTimeProvider dateTimeProvider)
    {
        _jobRepository = jobRepository;
        _persistence = persistence;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CompleteJobCloseoutResult>> HandleAsync(
        CompleteJobCloseoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(
            command.JobId,
            cancellationToken);

        if (job is null)
        {
            return Result<CompleteJobCloseoutResult>.Failure(
                JobApplicationErrors.NotFound);
        }

        if (job.Status != JobStatus.PendingCloseout)
        {
            return Result<CompleteJobCloseoutResult>.Failure(
                JobApplicationErrors.JobMustBePendingCloseout);
        }

        var utcNow = _dateTimeProvider.UtcNow;

        var closeoutResult = CloseoutRecord.Create(
            command.JobId,
            command.Summary,
            command.ProofItems,
            utcNow);

        if (closeoutResult.IsFailure)
        {
            return Result<CompleteJobCloseoutResult>.Failure(
                closeoutResult.Error);
        }

        var jobCloseResult = job.Close(utcNow);

        if (jobCloseResult.IsFailure)
        {
            return Result<CompleteJobCloseoutResult>.Failure(
                jobCloseResult.Error);
        }

        await _persistence.SaveAsync(
            job,
            closeoutResult.Value,
            cancellationToken);

        return Result<CompleteJobCloseoutResult>.Success(
            new CompleteJobCloseoutResult(
                closeoutResult.Value.Id,
                job.Id,
                job.Status));
    }
}

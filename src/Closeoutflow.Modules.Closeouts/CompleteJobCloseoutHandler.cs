using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts.Application;

public sealed class CompleteJobCloseoutHandler
{
    private readonly IJobRepository _jobRepository;
    private readonly ICloseoutRecordRepository _closeoutRecordRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompleteJobCloseoutHandler(
        IJobRepository jobRepository,
        ICloseoutRecordRepository closeoutRecordRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _jobRepository = jobRepository;
        _closeoutRecordRepository = closeoutRecordRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CompleteJobCloseoutResult>> HandleAsync(
        CompleteJobCloseoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdAsync(command.JobId, cancellationToken);

        if (job is null)
        {
            return Result<CompleteJobCloseoutResult>.Failure(JobApplicationErrors.NotFound);
        }

        if (job.Status != JobStatus.PendingCloseout)
        {
            return Result<CompleteJobCloseoutResult>.Failure(JobApplicationErrors.JobMustBePendingCloseout);
        }

        var closeoutResult = CloseoutRecord.Create(
            command.JobId,
            command.Summary,
            command.ProofItems,
            _dateTimeProvider.UtcNow);

        if (closeoutResult.IsFailure)
        {
            return Result<CompleteJobCloseoutResult>.Failure(closeoutResult.Error);
        }

        var jobCloseResult = job.Close(_dateTimeProvider.UtcNow);

        if (jobCloseResult.IsFailure)
        {
            return Result<CompleteJobCloseoutResult>.Failure(jobCloseResult.Error);
        }

        await _closeoutRecordRepository.AddAsync(closeoutResult.Value, cancellationToken);
        await _jobRepository.UpdateAsync(job, cancellationToken);

        return Result<CompleteJobCloseoutResult>.Success(
            new CompleteJobCloseoutResult(
                closeoutResult.Value.Id,
                job.Id,
                job.Status));
    }
}
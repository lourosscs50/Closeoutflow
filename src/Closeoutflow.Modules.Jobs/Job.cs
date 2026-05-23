using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Jobs;

public sealed class Job
{
    private Job(
        Guid id,
        string title,
        JobStatus status,
        DateTime createdAtUtc,
        DateTime? startedAtUtc,
        DateTime? pendingCloseoutAtUtc,
        DateTime? closedAtUtc)
    {
        Id = id;
        Title = title;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        StartedAtUtc = startedAtUtc;
        PendingCloseoutAtUtc = pendingCloseoutAtUtc;
        ClosedAtUtc = closedAtUtc;
    }

    public Guid Id { get; }
    public string Title { get; }
    public JobStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? PendingCloseoutAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public static Result<Job> Create(string title, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<Job>.Failure(new Error("Jobs.TitleRequired", "Job title is required."));
        }

        return Result<Job>.Success(
            new Job(
                Guid.NewGuid(),
                title.Trim(),
                JobStatus.New,
                createdAtUtc,
                startedAtUtc: null,
                pendingCloseoutAtUtc: null,
                closedAtUtc: null));
    }

    public static Job Rehydrate(
        Guid id,
        string title,
        JobStatus status,
        DateTime createdAtUtc,
        DateTime? startedAtUtc,
        DateTime? pendingCloseoutAtUtc,
        DateTime? closedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Job id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Job title is required.", nameof(title));
        }

        return new Job(
            id,
            title.Trim(),
            status,
            createdAtUtc,
            startedAtUtc,
            pendingCloseoutAtUtc,
            closedAtUtc);
    }

    public Result Start(DateTime utcNow)
    {
        if (Status != JobStatus.New)
        {
            return Result.Failure(JobErrors.AlreadyStarted);
        }

        Status = JobStatus.InProgress;
        StartedAtUtc = utcNow;

        return Result.Success();
    }

    public Result MarkPendingCloseout(DateTime utcNow)
    {
        if (Status != JobStatus.InProgress)
        {
            return Result.Failure(JobErrors.CannotMarkPendingFromCurrentStatus);
        }

        Status = JobStatus.PendingCloseout;
        PendingCloseoutAtUtc = utcNow;

        return Result.Success();
    }

    public Result Close(DateTime utcNow)
    {
        if (Status != JobStatus.PendingCloseout)
        {
            return Result.Failure(JobErrors.CannotCloseFromCurrentStatus);
        }

        Status = JobStatus.Closed;
        ClosedAtUtc = utcNow;

        return Result.Success();
    }
}

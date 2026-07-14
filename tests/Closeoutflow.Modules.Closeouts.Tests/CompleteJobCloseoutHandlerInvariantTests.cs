using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts.Tests;

public sealed class CompleteJobCloseoutHandlerInvariantTests
{
    [Fact]
    public async Task Missing_Job_Should_Not_Call_Persistence()
    {
        var jobs = new RecordingJobRepository();
        var persistence =
            new RecordingCompleteJobCloseoutPersistence();

        var handler = CreateHandler(jobs, persistence);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                Guid.NewGuid(),
                "Completed",
                new[] { (ProofItemType.Note, "Verified") }));

        Assert.True(result.IsFailure);
        Assert.Equal(JobApplicationErrors.NotFound, result.Error);
        Assert.Equal(0, persistence.SaveCalls);
    }

    [Fact]
    public async Task Invalid_Job_Status_Should_Not_Call_Persistence()
    {
        var job = Job.Create(
            "Repair gate motor",
            new DateTime(
                2026,
                6,
                13,
                8,
                0,
                0,
                DateTimeKind.Utc)).Value;

        var jobs = new RecordingJobRepository(job);
        var persistence =
            new RecordingCompleteJobCloseoutPersistence();

        var handler = CreateHandler(jobs, persistence);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "Completed",
                new[] { (ProofItemType.Note, "Verified") }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            JobApplicationErrors.JobMustBePendingCloseout,
            result.Error);

        Assert.Equal(JobStatus.New, job.Status);
        Assert.Null(job.ClosedAtUtc);
        Assert.Equal(0, persistence.SaveCalls);
    }

    [Fact]
    public async Task Invalid_Closeout_Should_Not_Close_Job_Or_Call_Persistence()
    {
        var now = new DateTime(
            2026,
            6,
            14,
            8,
            0,
            0,
            DateTimeKind.Utc);

        var job = CreatePendingJob(now);
        var jobs = new RecordingJobRepository(job);
        var persistence =
            new RecordingCompleteJobCloseoutPersistence();

        var handler = CreateHandler(
            jobs,
            persistence,
            now);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "Completed",
                new[] { (ProofItemType.Photo, "   ") }));

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofValueRequired, result.Error);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Null(job.ClosedAtUtc);
        Assert.Equal(0, persistence.SaveCalls);
    }

    [Fact]
    public async Task Successful_Closeout_Should_Call_Persistence_Exactly_Once()
    {
        var now = new DateTime(
            2026,
            6,
            15,
            8,
            0,
            0,
            DateTimeKind.Utc);

        var job = CreatePendingJob(now);
        var jobs = new RecordingJobRepository(job);
        var persistence =
            new RecordingCompleteJobCloseoutPersistence();

        var handler = CreateHandler(
            jobs,
            persistence,
            now);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "  Completed and inspected  ",
                new[]
                {
                    (
                        ProofItemType.Note,
                        "  Technician verified work  "),
                    (
                        ProofItemType.Photo,
                        "photo://completed-work")
                }));

        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(now, job.ClosedAtUtc);

        Assert.Equal(1, persistence.SaveCalls);
        Assert.Same(job, persistence.LastSavedJob);

        Assert.NotNull(persistence.LastSavedCloseout);
        Assert.Equal(
            result.Value.CloseoutRecordId,
            persistence.LastSavedCloseout!.Id);

        Assert.Equal(
            job.Id,
            persistence.LastSavedCloseout.JobId);

        Assert.Equal(
            "Completed and inspected",
            persistence.LastSavedCloseout.Summary);

        Assert.Equal(
            now,
            persistence.LastSavedCloseout.CreatedAtUtc);

        Assert.All(
            persistence.LastSavedCloseout.ProofItems,
            proof => Assert.Equal(now, proof.CreatedAtUtc));
    }


    [Fact]
    public async Task Persistence_Failure_Should_Be_Returned_By_Handler()
    {
        var now = new DateTime(
            2026,
            6,
            16,
            8,
            0,
            0,
            DateTimeKind.Utc);

        var job = CreatePendingJob(now);
        var jobs = new RecordingJobRepository(job);

        var handler = CreateHandler(
            jobs,
            new RejectingCompleteJobCloseoutPersistence(),
            now);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "Completed",
                new[] { (ProofItemType.Note, "Verified") }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            CloseoutErrors.AlreadyExistsForJob,
            result.Error);
    }

    private static Job CreatePendingJob(DateTime now)
    {
        var job = Job.Create(
            "Complete service work",
            now.AddHours(-3)).Value;

        Assert.True(
            job.Start(now.AddHours(-2)).IsSuccess);

        Assert.True(
            job.MarkPendingCloseout(now.AddHours(-1)).IsSuccess);

        return job;
    }

    private static CompleteJobCloseoutHandler CreateHandler(
        RecordingJobRepository jobs,
        ICompleteJobCloseoutPersistence persistence,
        DateTime? now = null)
    {
        return new CompleteJobCloseoutHandler(
            jobs,
            persistence,
            new FixedDateTimeProvider(
                now
                ?? new DateTime(
                    2026,
                    6,
                    15,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)));
    }

    private sealed class RecordingJobRepository : IJobRepository
    {
        private readonly Job? _job;

        internal RecordingJobRepository(Job? job = null)
        {
            _job = job;
        }

        public Task<Job?> GetByIdAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _job is not null && _job.Id == jobId
                    ? _job
                    : null);
        }

        public Task<IReadOnlyCollection<Job>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<Job> jobs =
                _job is null
                    ? Array.Empty<Job>()
                    : new[] { _job };

            return Task.FromResult(jobs);
        }

        public Task AddAsync(
            Job job,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            Job job,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingCompleteJobCloseoutPersistence
        : ICompleteJobCloseoutPersistence
    {
        internal int SaveCalls { get; private set; }
        internal Job? LastSavedJob { get; private set; }
        internal CloseoutRecord? LastSavedCloseout { get; private set; }

        public Task<Result> SaveAsync(
            Job job,
            CloseoutRecord closeoutRecord,
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            LastSavedJob = job;
            LastSavedCloseout = closeoutRecord;

            return Task.FromResult(Result.Success());
        }
    }


    private sealed class RejectingCompleteJobCloseoutPersistence
        : ICompleteJobCloseoutPersistence
    {
        public Task<Result> SaveAsync(
            Job job,
            CloseoutRecord closeoutRecord,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result.Failure(
                    CloseoutErrors.AlreadyExistsForJob));
        }
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        internal FixedDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}

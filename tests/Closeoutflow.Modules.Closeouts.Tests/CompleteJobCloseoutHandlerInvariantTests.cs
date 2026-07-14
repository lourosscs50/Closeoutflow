using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts.Tests;

public sealed class CompleteJobCloseoutHandlerInvariantTests
{
    [Fact]
    public async Task Missing_Job_Should_Not_Call_Persistence_Writes()
    {
        var jobs = new RecordingJobRepository();
        var closeouts = new RecordingCloseoutRepository();
        var handler = CreateHandler(jobs, closeouts);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                Guid.NewGuid(),
                "Completed",
                new[] { (ProofItemType.Note, "Verified") }));

        Assert.True(result.IsFailure);
        Assert.Equal(JobApplicationErrors.NotFound, result.Error);
        Assert.Equal(0, jobs.UpdateCalls);
        Assert.Equal(0, closeouts.AddCalls);
    }

    [Fact]
    public async Task Invalid_Job_Status_Should_Not_Call_Persistence_Writes()
    {
        var job = Job.Create(
            "Repair gate motor",
            new DateTime(2026, 6, 13, 8, 0, 0, DateTimeKind.Utc)).Value;

        var jobs = new RecordingJobRepository(job);
        var closeouts = new RecordingCloseoutRepository();
        var handler = CreateHandler(jobs, closeouts);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "Completed",
                new[] { (ProofItemType.Note, "Verified") }));

        Assert.True(result.IsFailure);
        Assert.Equal(JobApplicationErrors.JobMustBePendingCloseout, result.Error);
        Assert.Equal(JobStatus.New, job.Status);
        Assert.Null(job.ClosedAtUtc);
        Assert.Equal(0, jobs.UpdateCalls);
        Assert.Equal(0, closeouts.AddCalls);
    }

    [Fact]
    public async Task Invalid_Closeout_Should_Not_Close_Job_Or_Call_Writes()
    {
        var now = new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc);
        var job = CreatePendingJob(now);

        var jobs = new RecordingJobRepository(job);
        var closeouts = new RecordingCloseoutRepository();
        var handler = CreateHandler(jobs, closeouts, now);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "Completed",
                new[] { (ProofItemType.Photo, "   ") }));

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofValueRequired, result.Error);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Null(job.ClosedAtUtc);
        Assert.Equal(0, jobs.UpdateCalls);
        Assert.Equal(0, closeouts.AddCalls);
    }

    [Fact]
    public async Task Successful_Closeout_Should_Write_Each_Aggregate_Exactly_Once()
    {
        var now = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);
        var job = CreatePendingJob(now);

        var jobs = new RecordingJobRepository(job);
        var closeouts = new RecordingCloseoutRepository();
        var handler = CreateHandler(jobs, closeouts, now);

        var result = await handler.HandleAsync(
            new CompleteJobCloseoutCommand(
                job.Id,
                "  Completed and inspected  ",
                new[]
                {
                    (ProofItemType.Note, "  Technician verified work  "),
                    (ProofItemType.Photo, "photo://completed-work")
                }));

        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(now, job.ClosedAtUtc);

        Assert.Equal(1, jobs.UpdateCalls);
        Assert.Same(job, jobs.LastUpdatedJob);

        Assert.Equal(1, closeouts.AddCalls);
        Assert.NotNull(closeouts.LastAddedCloseout);
        Assert.Equal(result.Value.CloseoutRecordId, closeouts.LastAddedCloseout!.Id);
        Assert.Equal(job.Id, closeouts.LastAddedCloseout.JobId);
        Assert.Equal("Completed and inspected", closeouts.LastAddedCloseout.Summary);
        Assert.Equal(now, closeouts.LastAddedCloseout.CreatedAtUtc);
        Assert.All(
            closeouts.LastAddedCloseout.ProofItems,
            proof => Assert.Equal(now, proof.CreatedAtUtc));
    }

    private static Job CreatePendingJob(DateTime now)
    {
        var job = Job.Create("Complete service work", now.AddHours(-3)).Value;

        Assert.True(job.Start(now.AddHours(-2)).IsSuccess);
        Assert.True(job.MarkPendingCloseout(now.AddHours(-1)).IsSuccess);

        return job;
    }

    private static CompleteJobCloseoutHandler CreateHandler(
        RecordingJobRepository jobs,
        RecordingCloseoutRepository closeouts,
        DateTime? now = null)
    {
        return new CompleteJobCloseoutHandler(
            jobs,
            closeouts,
            new FixedDateTimeProvider(
                now ?? new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc)));
    }

    private sealed class RecordingJobRepository : IJobRepository
    {
        private readonly Job? _job;

        internal RecordingJobRepository(Job? job = null)
        {
            _job = job;
        }

        internal int UpdateCalls { get; private set; }
        internal Job? LastUpdatedJob { get; private set; }

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
                _job is null ? Array.Empty<Job>() : new[] { _job };

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
            UpdateCalls++;
            LastUpdatedJob = job;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingCloseoutRepository : ICloseoutRecordRepository
    {
        internal int AddCalls { get; private set; }
        internal CloseoutRecord? LastAddedCloseout { get; private set; }

        public Task AddAsync(
            CloseoutRecord closeoutRecord,
            CancellationToken cancellationToken = default)
        {
            AddCalls++;
            LastAddedCloseout = closeoutRecord;
            return Task.CompletedTask;
        }

        public Task<CloseoutRecord?> GetByIdAsync(
            Guid closeoutRecordId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CloseoutRecord?>(null);
        }

        public Task<IReadOnlyCollection<CloseoutRecord>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<CloseoutRecord>>(
                Array.Empty<CloseoutRecord>());
        }

        public Task<IReadOnlyCollection<CloseoutRecord>> ListByJobIdAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<CloseoutRecord>>(
                Array.Empty<CloseoutRecord>());
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

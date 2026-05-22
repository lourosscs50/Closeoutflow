using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts.Tests;

public class CompleteJobCloseoutHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Job_Does_Not_Exist()
    {
        var handler = new CompleteJobCloseoutHandler(
            new FakeJobRepository(),
            new FakeCloseoutRecordRepository(),
            new FakeDateTimeProvider(new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)));

        var command = new CompleteJobCloseoutCommand(
            Guid.NewGuid(),
            "Completed work",
            new[] { (ProofItemType.Note, "All tasks finished") });

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(JobApplicationErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Job_Is_Not_PendingCloseout()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;

        var jobRepository = new FakeJobRepository();
        await jobRepository.AddAsync(job);

        var handler = new CompleteJobCloseoutHandler(
            jobRepository,
            new FakeCloseoutRecordRepository(),
            new FakeDateTimeProvider(new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)));

        var command = new CompleteJobCloseoutCommand(
            job.Id,
            "Completed work",
            new[] { (ProofItemType.Note, "All tasks finished") });

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(JobApplicationErrors.JobMustBePendingCloseout, result.Error);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Closeout_Proof_Is_Invalid()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);
        job.MarkPendingCloseout(DateTime.UtcNow);

        var jobRepository = new FakeJobRepository();
        await jobRepository.AddAsync(job);

        var closeoutRepository = new FakeCloseoutRecordRepository();

        var handler = new CompleteJobCloseoutHandler(
            jobRepository,
            closeoutRepository,
            new FakeDateTimeProvider(new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)));

        var command = new CompleteJobCloseoutCommand(
            job.Id,
            "Completed work",
            new[] { (ProofItemType.Note, "   ") });

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofValueRequired, result.Error);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Null(job.ClosedAtUtc);
        Assert.Empty(closeoutRepository.Items);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Closeout_And_Close_Job_When_Input_Is_Valid()
    {
        var now = new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc);

        var job = Job.Create("Replace rooftop unit", now.AddHours(-2)).Value;
        job.Start(now.AddHours(-1));
        job.MarkPendingCloseout(now.AddMinutes(-30));

        var jobRepository = new FakeJobRepository();
        await jobRepository.AddAsync(job);

        var closeoutRepository = new FakeCloseoutRecordRepository();

        var handler = new CompleteJobCloseoutHandler(
            jobRepository,
            closeoutRepository,
            new FakeDateTimeProvider(now));

        var command = new CompleteJobCloseoutCommand(
            job.Id,
            "Completed work successfully",
            new[]
            {
                (ProofItemType.Note, "All tasks finished"),
                (ProofItemType.Photo, "https://example.com/proof.jpg")
            });

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(job.Id, result.Value.JobId);
        Assert.Equal(JobStatus.Closed, result.Value.JobStatus);

        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(now, job.ClosedAtUtc);

        Assert.Single(closeoutRepository.Items);
        Assert.Equal(job.Id, closeoutRepository.Items.Single().JobId);
    }
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Proof_Items_Are_Null()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);
        job.MarkPendingCloseout(DateTime.UtcNow);

        var jobRepository = new FakeJobRepository();
        await jobRepository.AddAsync(job);

        var closeoutRepository = new FakeCloseoutRecordRepository();

        var handler = new CompleteJobCloseoutHandler(
            jobRepository,
            closeoutRepository,
            new FakeDateTimeProvider(new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)));

        var command = new CompleteJobCloseoutCommand(
            job.Id,
            "Completed work",
            null!);

        var result = await handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofRequired, result.Error);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Null(job.ClosedAtUtc);
        Assert.Empty(closeoutRepository.Items);
    }

}

internal sealed class FakeJobRepository : IJobRepository
{
    private readonly List<Job> _jobs = new();

    public Task<Job?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_jobs.SingleOrDefault(x => x.Id == jobId));
    }

    public Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        _jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakeCloseoutRecordRepository : ICloseoutRecordRepository
{
    public List<CloseoutRecord> Items { get; } = new();

    public Task AddAsync(CloseoutRecord closeoutRecord, CancellationToken cancellationToken = default)
    {
        Items.Add(closeoutRecord);
        return Task.CompletedTask;
    }

    public Task<CloseoutRecord?> GetByIdAsync(Guid closeoutRecordId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(x => x.Id == closeoutRecordId));
    }

    public Task<IReadOnlyCollection<CloseoutRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CloseoutRecord> records = Items
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(records);
    }

    public Task<IReadOnlyCollection<CloseoutRecord>> ListByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CloseoutRecord> records = Items
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(records);
    }
}

internal sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}
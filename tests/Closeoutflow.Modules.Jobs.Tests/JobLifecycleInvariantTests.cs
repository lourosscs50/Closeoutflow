using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Modules.Jobs.Tests;

public sealed class JobLifecycleInvariantTests
{
    [Fact]
    public void Failed_MarkPendingCloseout_Should_Not_Mutate_New_Job()
    {
        var createdAt = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var attemptedAt = createdAt.AddHours(1);
        var job = Job.Create("Repair dock plate", createdAt).Value;

        var result = job.MarkPendingCloseout(attemptedAt);

        Assert.True(result.IsFailure);
        Assert.Equal(JobErrors.CannotMarkPendingFromCurrentStatus, result.Error);
        Assert.Equal(JobStatus.New, job.Status);
        Assert.Null(job.StartedAtUtc);
        Assert.Null(job.PendingCloseoutAtUtc);
        Assert.Null(job.ClosedAtUtc);
    }

    [Fact]
    public void Failed_Close_Should_Not_Mutate_InProgress_Job()
    {
        var createdAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc);
        var startedAt = createdAt.AddMinutes(30);
        var attemptedCloseAt = createdAt.AddHours(1);

        var job = Job.Create("Replace door closer", createdAt).Value;
        Assert.True(job.Start(startedAt).IsSuccess);

        var result = job.Close(attemptedCloseAt);

        Assert.True(result.IsFailure);
        Assert.Equal(JobErrors.CannotCloseFromCurrentStatus, result.Error);
        Assert.Equal(JobStatus.InProgress, job.Status);
        Assert.Equal(startedAt, job.StartedAtUtc);
        Assert.Null(job.PendingCloseoutAtUtc);
        Assert.Null(job.ClosedAtUtc);
    }

    [Fact]
    public void Repeated_Close_Should_Not_Overwrite_Original_ClosedAtUtc()
    {
        var createdAt = new DateTime(2026, 6, 3, 8, 0, 0, DateTimeKind.Utc);
        var startedAt = createdAt.AddMinutes(30);
        var pendingAt = createdAt.AddHours(1);
        var closedAt = createdAt.AddHours(2);
        var repeatedCloseAt = createdAt.AddHours(3);

        var job = Job.Create("Repair loading ramp", createdAt).Value;
        Assert.True(job.Start(startedAt).IsSuccess);
        Assert.True(job.MarkPendingCloseout(pendingAt).IsSuccess);
        Assert.True(job.Close(closedAt).IsSuccess);

        var result = job.Close(repeatedCloseAt);

        Assert.True(result.IsFailure);
        Assert.Equal(JobErrors.CannotCloseFromCurrentStatus, result.Error);
        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(closedAt, job.ClosedAtUtc);
    }

    [Fact]
    public void Successful_Lifecycle_Should_Keep_Status_And_Timestamps_Synchronized()
    {
        var createdAt = new DateTime(2026, 6, 4, 8, 0, 0, DateTimeKind.Utc);
        var startedAt = createdAt.AddMinutes(20);
        var pendingAt = createdAt.AddHours(1);
        var closedAt = createdAt.AddHours(2);

        var job = Job.Create("Replace safety rail", createdAt).Value;

        Assert.Equal(JobStatus.New, job.Status);
        Assert.Null(job.StartedAtUtc);
        Assert.Null(job.PendingCloseoutAtUtc);
        Assert.Null(job.ClosedAtUtc);

        Assert.True(job.Start(startedAt).IsSuccess);
        Assert.Equal(JobStatus.InProgress, job.Status);
        Assert.Equal(startedAt, job.StartedAtUtc);
        Assert.Null(job.PendingCloseoutAtUtc);
        Assert.Null(job.ClosedAtUtc);

        Assert.True(job.MarkPendingCloseout(pendingAt).IsSuccess);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Equal(startedAt, job.StartedAtUtc);
        Assert.Equal(pendingAt, job.PendingCloseoutAtUtc);
        Assert.Null(job.ClosedAtUtc);

        Assert.True(job.Close(closedAt).IsSuccess);
        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(startedAt, job.StartedAtUtc);
        Assert.Equal(pendingAt, job.PendingCloseoutAtUtc);
        Assert.Equal(closedAt, job.ClosedAtUtc);
    }

    [Fact]
    public void Rehydrate_Should_Trim_Title_And_Preserve_State()
    {
        var jobId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 6, 5, 8, 0, 0, DateTimeKind.Utc);
        var startedAt = createdAt.AddMinutes(30);
        var pendingAt = createdAt.AddHours(1);

        var job = Job.Rehydrate(
            jobId,
            "  Inspect fire suppression system  ",
            JobStatus.PendingCloseout,
            createdAt,
            startedAt,
            pendingAt,
            closedAtUtc: null);

        Assert.Equal(jobId, job.Id);
        Assert.Equal("Inspect fire suppression system", job.Title);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Equal(createdAt, job.CreatedAtUtc);
        Assert.Equal(startedAt, job.StartedAtUtc);
        Assert.Equal(pendingAt, job.PendingCloseoutAtUtc);
        Assert.Null(job.ClosedAtUtc);
    }

    [Fact]
    public void Rehydrate_Should_Reject_Invalid_Identity_And_Title()
    {
        var now = new DateTime(2026, 6, 6, 8, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() =>
            Job.Rehydrate(
                Guid.Empty,
                "Valid title",
                JobStatus.New,
                now,
                null,
                null,
                null));

        Assert.Throws<ArgumentException>(() =>
            Job.Rehydrate(
                Guid.NewGuid(),
                "   ",
                JobStatus.New,
                now,
                null,
                null,
                null));
    }
}

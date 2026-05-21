using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Modules.Jobs.Tests;

public class JobTests
{
    [Fact]
    public void Create_Should_Set_Status_To_New_When_Title_Is_Valid()
    {
        var createdAtUtc = new DateTime(2026, 4, 17, 12, 0, 0, DateTimeKind.Utc);

        var result = Job.Create("Replace rooftop unit", createdAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal("Replace rooftop unit", result.Value.Title);
        Assert.Equal(JobStatus.New, result.Value.Status);
        Assert.Equal(createdAtUtc, result.Value.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Title_Is_Blank(string title)
    {
        var createdAtUtc = DateTime.UtcNow;

        var result = Job.Create(title, createdAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal("Jobs.TitleRequired", result.Error.Code);
    }

    [Fact]
    public void Start_Should_Move_Job_From_New_To_InProgress()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        var startedAtUtc = new DateTime(2026, 4, 17, 12, 30, 0, DateTimeKind.Utc);

        var result = job.Start(startedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.InProgress, job.Status);
        Assert.Equal(startedAtUtc, job.StartedAtUtc);
    }

    [Fact]
    public void Start_Should_Fail_When_Job_Has_Already_Been_Started()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);

        var result = job.Start(DateTime.UtcNow.AddMinutes(5));

        Assert.True(result.IsFailure);
        Assert.Equal(JobErrors.AlreadyStarted, result.Error);
    }

    [Fact]
    public void MarkPendingCloseout_Should_Move_Job_From_InProgress_To_PendingCloseout()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);
        var pendingAtUtc = new DateTime(2026, 4, 17, 13, 0, 0, DateTimeKind.Utc);

        var result = job.MarkPendingCloseout(pendingAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.PendingCloseout, job.Status);
        Assert.Equal(pendingAtUtc, job.PendingCloseoutAtUtc);
    }

    [Fact]
    public void MarkPendingCloseout_Should_Fail_When_Job_Is_Not_InProgress()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;

        var result = job.MarkPendingCloseout(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(JobErrors.CannotMarkPendingFromCurrentStatus, result.Error);
    }

    [Fact]
    public void Close_Should_Move_Job_From_PendingCloseout_To_Closed()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);
        job.MarkPendingCloseout(DateTime.UtcNow);
        var closedAtUtc = new DateTime(2026, 4, 17, 13, 30, 0, DateTimeKind.Utc);

        var result = job.Close(closedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(JobStatus.Closed, job.Status);
        Assert.Equal(closedAtUtc, job.ClosedAtUtc);
    }

    [Fact]
    public void Close_Should_Fail_When_Job_Is_Not_PendingCloseout()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);

        var result = job.Close(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(JobErrors.CannotCloseFromCurrentStatus, result.Error);
    }

    [Fact]
    public void Closed_Job_Should_Not_Move_Backward()
    {
        var job = Job.Create("Replace rooftop unit", DateTime.UtcNow).Value;
        job.Start(DateTime.UtcNow);
        job.MarkPendingCloseout(DateTime.UtcNow);
        job.Close(DateTime.UtcNow);

        var startResult = job.Start(DateTime.UtcNow.AddMinutes(1));
        var pendingResult = job.MarkPendingCloseout(DateTime.UtcNow.AddMinutes(2));

        Assert.True(startResult.IsFailure);
        Assert.True(pendingResult.IsFailure);
        Assert.Equal(JobStatus.Closed, job.Status);
    }
}
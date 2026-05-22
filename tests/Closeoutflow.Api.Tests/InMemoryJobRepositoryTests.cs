using Closeoutflow.Api.Repositories;
using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Api.Tests;

public sealed class InMemoryJobRepositoryTests
{
    [Fact]
    public async Task AddAsync_Should_Make_Job_Retrievable_By_Id()
    {
        var repository = new InMemoryJobRepository();
        var job = CreateJob();

        await repository.AddAsync(job);

        var savedJob = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(savedJob);
        Assert.Same(job, savedJob);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Job_Does_Not_Exist()
    {
        var repository = new InMemoryJobRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_Should_Throw_When_Job_With_Same_Id_Already_Exists()
    {
        var repository = new InMemoryJobRepository();
        var job = CreateJob();

        await repository.AddAsync(job);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(job));

        Assert.Contains(job.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Job_Does_Not_Exist()
    {
        var repository = new InMemoryJobRepository();
        var job = CreateJob();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(job));

        Assert.Contains(job.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_Should_Succeed_When_Job_Exists()
    {
        var repository = new InMemoryJobRepository();
        var job = CreateJob();

        await repository.AddAsync(job);
        var result = job.Start(new DateTime(2026, 4, 17, 12, 30, 0, DateTimeKind.Utc));

        await repository.UpdateAsync(job);
        var savedJob = await repository.GetByIdAsync(job.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedJob);
        Assert.Equal(JobStatus.InProgress, savedJob.Status);
    }


    [Fact]
    public async Task ListAsync_Should_Return_Jobs_Ordered_Newest_First()
    {
        var repository = new InMemoryJobRepository();

        var olderJob = CreateJob(
            "Older job",
            new DateTime(2026, 4, 17, 12, 0, 0, DateTimeKind.Utc));

        var newerJob = CreateJob(
            "Newer job",
            new DateTime(2026, 4, 17, 13, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(olderJob);
        await repository.AddAsync(newerJob);

        var jobs = await repository.ListAsync();

        Assert.Collection(
            jobs,
            first => Assert.Same(newerJob, first),
            second => Assert.Same(olderJob, second));
    }

    [Fact]
    public async Task ListAsync_Should_Return_Empty_Collection_When_No_Jobs_Exist()
    {
        var repository = new InMemoryJobRepository();

        var jobs = await repository.ListAsync();

        Assert.Empty(jobs);
    }

    private static Job CreateJob()
    {
        return CreateJob(
            "Replace rooftop unit",
            new DateTime(2026, 4, 17, 12, 0, 0, DateTimeKind.Utc));
    }

    private static Job CreateJob(string title, DateTime createdAtUtc)
    {
        return Job.Create(title, createdAtUtc).Value;
    }
}

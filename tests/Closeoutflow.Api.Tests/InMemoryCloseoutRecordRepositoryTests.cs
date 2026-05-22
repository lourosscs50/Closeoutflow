using Closeoutflow.Api.Repositories;
using Closeoutflow.Modules.Closeouts;

namespace Closeoutflow.Api.Tests;

public sealed class InMemoryCloseoutRecordRepositoryTests
{
    [Fact]
    public async Task AddAsync_Should_Make_CloseoutRecord_Retrievable_By_Id()
    {
        var repository = new InMemoryCloseoutRecordRepository();
        var closeoutRecord = CreateCloseoutRecord();

        await repository.AddAsync(closeoutRecord);

        var savedRecord = await repository.GetByIdAsync(closeoutRecord.Id);

        Assert.NotNull(savedRecord);
        Assert.Same(closeoutRecord, savedRecord);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_CloseoutRecord_Does_Not_Exist()
    {
        var repository = new InMemoryCloseoutRecordRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_Should_Throw_When_CloseoutRecord_With_Same_Id_Already_Exists()
    {
        var repository = new InMemoryCloseoutRecordRepository();
        var closeoutRecord = CreateCloseoutRecord();

        await repository.AddAsync(closeoutRecord);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(closeoutRecord));

        Assert.Contains(closeoutRecord.Id.ToString(), exception.Message);
    }


    [Fact]
    public async Task ListAsync_Should_Return_CloseoutRecords_Ordered_Newest_First()
    {
        var repository = new InMemoryCloseoutRecordRepository();

        var olderRecord = CreateCloseoutRecord(
            Guid.NewGuid(),
            new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc));

        var newerRecord = CreateCloseoutRecord(
            Guid.NewGuid(),
            new DateTime(2026, 4, 18, 16, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(olderRecord);
        await repository.AddAsync(newerRecord);

        var records = await repository.ListAsync();

        Assert.Collection(
            records,
            first => Assert.Same(newerRecord, first),
            second => Assert.Same(olderRecord, second));
    }

    [Fact]
    public async Task ListByJobIdAsync_Should_Return_Only_CloseoutRecords_For_Requested_Job()
    {
        var repository = new InMemoryCloseoutRecordRepository();

        var requestedJobId = Guid.NewGuid();
        var otherJobId = Guid.NewGuid();

        var requestedJobRecord = CreateCloseoutRecord(
            requestedJobId,
            new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc));

        var otherJobRecord = CreateCloseoutRecord(
            otherJobId,
            new DateTime(2026, 4, 18, 16, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(requestedJobRecord);
        await repository.AddAsync(otherJobRecord);

        var records = await repository.ListByJobIdAsync(requestedJobId);

        var record = Assert.Single(records);
        Assert.Same(requestedJobRecord, record);
    }

    private static CloseoutRecord CreateCloseoutRecord()
    {
        return CreateCloseoutRecord(
            Guid.NewGuid(),
            new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc));
    }

    private static CloseoutRecord CreateCloseoutRecord(Guid jobId, DateTime createdAtUtc)
    {
        return CloseoutRecord.Create(
            jobId,
            "Work completed",
            new[]
            {
                (ProofItemType.Note, "Completed work")
            },
            createdAtUtc).Value;
    }
}

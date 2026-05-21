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

    private static CloseoutRecord CreateCloseoutRecord()
    {
        return CloseoutRecord.Create(
            Guid.NewGuid(),
            "Work completed",
            new[]
            {
                (ProofItemType.Note, "Completed work")
            },
            new DateTime(2026, 4, 18, 15, 0, 0, DateTimeKind.Utc)).Value;
    }
}

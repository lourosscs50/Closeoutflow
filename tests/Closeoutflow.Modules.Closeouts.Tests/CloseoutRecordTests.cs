using Closeoutflow.Modules.Closeouts;

namespace Closeoutflow.Modules.Closeouts.Tests;

public class CloseoutRecordTests
{
    [Fact]
    public void Create_Should_Succeed_When_Valid_Input_Is_Provided()
    {
        var jobId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 4, 17, 14, 0, 0, DateTimeKind.Utc);

        var result = CloseoutRecord.Create(
            jobId,
            " Unit replaced successfully ",
            new[]
            {
                (ProofItemType.Note, "Completed replacement"),
                (ProofItemType.Photo, "https://example.com/photo-1.jpg")
            },
            createdAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(jobId, result.Value.JobId);
        Assert.Equal("Unit replaced successfully", result.Value.Summary);
        Assert.Equal(2, result.Value.ProofItems.Count);
        Assert.Equal(createdAtUtc, result.Value.CreatedAtUtc);
    }

    [Fact]
    public void Create_Should_Fail_When_JobId_Is_Empty()
    {
        var result = CloseoutRecord.Create(
            Guid.Empty,
            "Done",
            new[]
            {
                (ProofItemType.Note, "Completed work")
            },
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.JobIdRequired, result.Error);
    }

    [Fact]
    public void Create_Should_Fail_When_No_Proof_Items_Are_Provided()
    {
        var result = CloseoutRecord.Create(
            Guid.NewGuid(),
            "Done",
            Array.Empty<(ProofItemType Type, string Value)>(),
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofRequired, result.Error);
    }

    [Fact]
    public void Create_Should_Fail_When_Any_Proof_Value_Is_Blank()
    {
        var result = CloseoutRecord.Create(
            Guid.NewGuid(),
            "Done",
            new[]
            {
                (ProofItemType.Note, "   ")
            },
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofValueRequired, result.Error);
    }

    [Fact]
    public void Create_Should_Trim_Summary()
    {
        var result = CloseoutRecord.Create(
            Guid.NewGuid(),
            "  Work completed successfully  ",
            new[]
            {
                (ProofItemType.Note, "Completed work")
            },
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("Work completed successfully", result.Value.Summary);
    }

    [Fact]
    public void Create_Should_Preserve_Proof_Item_Details()
    {
        var result = CloseoutRecord.Create(
            Guid.NewGuid(),
            "Done",
            new[]
            {
                (ProofItemType.Signature, "Signed by customer")
            },
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);

        var proofItem = result.Value.ProofItems.Single();
        Assert.Equal(ProofItemType.Signature, proofItem.Type);
        Assert.Equal("Signed by customer", proofItem.Value);
    }
}
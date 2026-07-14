using Closeoutflow.Modules.Closeouts;

namespace Closeoutflow.Modules.Closeouts.Tests;

public sealed class CloseoutInvariantTests
{
    [Fact]
    public void Create_Should_Preserve_Proof_Order_And_Use_Aggregate_Timestamp()
    {
        var createdAt = new DateTime(2026, 6, 7, 8, 0, 0, DateTimeKind.Utc);

        var result = CloseoutRecord.Create(
            Guid.NewGuid(),
            "Work verified",
            new[]
            {
                (ProofItemType.Note, "Technician notes"),
                (ProofItemType.Photo, "photo://completed-work"),
                (ProofItemType.Signature, "Customer signature")
            },
            createdAt);

        Assert.True(result.IsSuccess);

        Assert.Collection(
            result.Value.ProofItems,
            first =>
            {
                Assert.Equal(ProofItemType.Note, first.Type);
                Assert.Equal("Technician notes", first.Value);
                Assert.Equal(createdAt, first.CreatedAtUtc);
            },
            second =>
            {
                Assert.Equal(ProofItemType.Photo, second.Type);
                Assert.Equal("photo://completed-work", second.Value);
                Assert.Equal(createdAt, second.CreatedAtUtc);
            },
            third =>
            {
                Assert.Equal(ProofItemType.Signature, third.Type);
                Assert.Equal("Customer signature", third.Value);
                Assert.Equal(createdAt, third.CreatedAtUtc);
            });
    }

    [Fact]
    public void Create_Failure_Should_Not_Produce_Partial_Aggregate()
    {
        var result = CloseoutRecord.Create(
            Guid.NewGuid(),
            "Work attempted",
            new[]
            {
                (ProofItemType.Note, "Valid note"),
                (ProofItemType.Photo, "   ")
            },
            new DateTime(2026, 6, 8, 8, 0, 0, DateTimeKind.Utc));

        Assert.True(result.IsFailure);
        Assert.Equal(CloseoutErrors.ProofValueRequired, result.Error);
    }

    [Fact]
    public void Rehydrate_Should_Preserve_Proof_Order_And_Trim_Summary()
    {
        var createdAt = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc);

        var first = ProofItem.Rehydrate(
            Guid.NewGuid(),
            ProofItemType.Signature,
            "Customer signature",
            createdAt.AddMinutes(-2));

        var second = ProofItem.Rehydrate(
            Guid.NewGuid(),
            ProofItemType.Photo,
            "photo://final-condition",
            createdAt.AddMinutes(-1));

        var closeout = CloseoutRecord.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Completed and inspected  ",
            createdAt,
            new[] { first, second });

        Assert.Equal("Completed and inspected", closeout.Summary);

        Assert.Collection(
            closeout.ProofItems,
            item => Assert.Same(first, item),
            item => Assert.Same(second, item));
    }

    [Fact]
    public void Rehydrate_Should_Reject_Invalid_Identity_Job_And_Proofs()
    {
        var now = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var proof = ProofItem.Create(ProofItemType.Note, "Valid note", now);

        Assert.Throws<ArgumentException>(() =>
            CloseoutRecord.Rehydrate(
                Guid.Empty,
                Guid.NewGuid(),
                "Done",
                now,
                new[] { proof }));

        Assert.Throws<ArgumentException>(() =>
            CloseoutRecord.Rehydrate(
                Guid.NewGuid(),
                Guid.Empty,
                "Done",
                now,
                new[] { proof }));

        Assert.Throws<ArgumentException>(() =>
            CloseoutRecord.Rehydrate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Done",
                now,
                Array.Empty<ProofItem>()));
    }

    [Fact]
    public void ProofItem_Rehydrate_Should_Trim_Value_And_Preserve_State()
    {
        var proofId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Utc);

        var proof = ProofItem.Rehydrate(
            proofId,
            ProofItemType.Photo,
            "  photo://equipment  ",
            createdAt);

        Assert.Equal(proofId, proof.Id);
        Assert.Equal(ProofItemType.Photo, proof.Type);
        Assert.Equal("photo://equipment", proof.Value);
        Assert.Equal(createdAt, proof.CreatedAtUtc);
    }

    [Fact]
    public void ProofItem_Rehydrate_Should_Reject_Invalid_Id_And_Value()
    {
        var now = new DateTime(2026, 6, 12, 8, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() =>
            ProofItem.Rehydrate(
                Guid.Empty,
                ProofItemType.Note,
                "Valid note",
                now));

        Assert.Throws<ArgumentException>(() =>
            ProofItem.Rehydrate(
                Guid.NewGuid(),
                ProofItemType.Note,
                "   ",
                now));
    }
}

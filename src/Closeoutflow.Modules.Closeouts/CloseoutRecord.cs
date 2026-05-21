using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts;

public sealed class CloseoutRecord
{
    private readonly List<ProofItem> _proofItems = new();

    private CloseoutRecord(
        Guid id,
        Guid jobId,
        string summary,
        DateTime createdAtUtc,
        IEnumerable<ProofItem> proofItems)
    {
        Id = id;
        JobId = jobId;
        Summary = summary;
        CreatedAtUtc = createdAtUtc;
        _proofItems.AddRange(proofItems);
    }

    public Guid Id { get; }
    public Guid JobId { get; }
    public string Summary { get; }
    public DateTime CreatedAtUtc { get; }
    public IReadOnlyCollection<ProofItem> ProofItems => _proofItems;

    public static Result<CloseoutRecord> Create(
        Guid jobId,
        string summary,
        IEnumerable<(ProofItemType Type, string Value)> proofItems,
        DateTime createdAtUtc)
    {
        if (jobId == Guid.Empty)
        {
            return Result<CloseoutRecord>.Failure(CloseoutErrors.JobIdRequired);
        }

        var items = proofItems?.ToList() ?? new List<(ProofItemType Type, string Value)>();

        if (items.Count == 0)
        {
            return Result<CloseoutRecord>.Failure(CloseoutErrors.ProofRequired);
        }

        if (items.Any(x => string.IsNullOrWhiteSpace(x.Value)))
        {
            return Result<CloseoutRecord>.Failure(CloseoutErrors.ProofValueRequired);
        }

        var createdProofItems = items
            .Select(x => ProofItem.Create(x.Type, x.Value, createdAtUtc))
            .ToList();

        return Result<CloseoutRecord>.Success(
            new CloseoutRecord(
                Guid.NewGuid(),
                jobId,
                summary?.Trim() ?? string.Empty,
                createdAtUtc,
                createdProofItems));
    }
}

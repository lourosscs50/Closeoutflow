namespace Closeoutflow.Modules.Closeouts;

public sealed class ProofItem
{
    private ProofItem(Guid id, ProofItemType type, string value, DateTime createdAtUtc)
    {
        Id = id;
        Type = type;
        Value = value;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public ProofItemType Type { get; }
    public string Value { get; }
    public DateTime CreatedAtUtc { get; }

    public static ProofItem Create(ProofItemType type, string value, DateTime createdAtUtc)
    {
        return new ProofItem(Guid.NewGuid(), type, value.Trim(), createdAtUtc);
    }
}
    
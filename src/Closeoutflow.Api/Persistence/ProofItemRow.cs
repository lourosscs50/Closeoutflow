namespace Closeoutflow.Api.Persistence;

public sealed class ProofItemRow
{
    public Guid Id { get; set; }
    public Guid CloseoutRecordId { get; set; }
    public int Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public CloseoutRecordRow? CloseoutRecord { get; set; }
}

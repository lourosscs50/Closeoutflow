namespace Closeoutflow.Api.Persistence;

public sealed class CloseoutRecordRow
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<ProofItemRow> ProofItems { get; set; } = new();
}

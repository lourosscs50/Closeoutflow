namespace Closeoutflow.Api.Persistence;

public sealed class JobRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? PendingCloseoutAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}

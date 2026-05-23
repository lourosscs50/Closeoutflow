namespace Closeoutflow.Api.Contracts;

public sealed record JobResponse(
    Guid JobId,
    string Title,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? PendingCloseoutAtUtc,
    DateTime? ClosedAtUtc);

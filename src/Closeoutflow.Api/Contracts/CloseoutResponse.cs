namespace Closeoutflow.Api.Contracts;

public sealed record CloseoutResponse(
    Guid CloseoutRecordId,
    Guid JobId,
    string Summary,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<ProofItemResponse> ProofItems);

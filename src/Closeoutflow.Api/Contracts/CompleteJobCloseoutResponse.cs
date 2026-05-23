namespace Closeoutflow.Api.Contracts;

public sealed record CompleteJobCloseoutResponse(
    Guid CloseoutRecordId,
    Guid JobId,
    string JobStatus);

namespace Closeoutflow.Api.Contracts;

public sealed record JobStatusResponse(
    Guid JobId,
    string Status);

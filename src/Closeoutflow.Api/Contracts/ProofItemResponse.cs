namespace Closeoutflow.Api.Contracts;

public sealed record ProofItemResponse(
    Guid ProofItemId,
    string Type,
    string Value,
    DateTime CreatedAtUtc);

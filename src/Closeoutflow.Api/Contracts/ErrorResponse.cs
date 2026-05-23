namespace Closeoutflow.Api.Contracts;

public sealed record ErrorResponse(
    string Error,
    string Message);

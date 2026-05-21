using Closeoutflow.Modules.Closeouts;

namespace Closeoutflow.Api.Contracts;

public sealed class CompleteJobCloseoutRequest
{
    public string Summary { get; init; } = string.Empty;
    public List<ProofItemRequest>? ProofItems { get; init; } = new();
}

public sealed class ProofItemRequest
{
    public ProofItemType Type { get; init; }
    public string Value { get; init; } = string.Empty;
}

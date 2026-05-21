namespace Closeoutflow.Modules.Closeouts.Application;

public sealed record CompleteJobCloseoutCommand(
    Guid JobId,
    string Summary,
    IReadOnlyCollection<(ProofItemType Type, string Value)> ProofItems);